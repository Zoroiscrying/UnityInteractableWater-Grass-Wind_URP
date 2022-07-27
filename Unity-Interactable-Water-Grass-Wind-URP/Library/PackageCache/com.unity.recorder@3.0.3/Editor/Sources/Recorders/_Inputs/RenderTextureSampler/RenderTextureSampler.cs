using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    class RenderTextureSampler : BaseRenderTextureInput
    {
        Shader superShader;
        Shader accumulateShader;
        Shader normalizeShader;

        RenderTexture m_renderRT;
        RenderTexture[] m_accumulateRTs = new RenderTexture[2];
        int m_renderWidth, m_renderHeight;

        // Whether or not the incoming RenderTexture must be converted from linear to sRGB color space
        private bool m_needToConvertLinearToSRGB = false;

        // Whether or not the incoming RenderTexture must be converted from sRGB to linear color space
        private bool m_needToConvertSRGBToLinear = false;

        Material m_superMaterial;
        Material m_accumulateMaterial;
        Material m_normalizeMaterial;

        class HookedCamera
        {
            public Camera camera;
            public RenderTexture textureBackup;
        }

        List<HookedCamera> m_hookedCameras;

        Vector2[] m_samples;

        RenderTextureSamplerSettings rtsSettings
        {
            get { return (RenderTextureSamplerSettings)settings; }
        }

        void GenerateSamplesMSAA(Vector2[] samples, SuperSamplingCount sc)
        {
            switch (sc)
            {
                case SuperSamplingCount.X1:
                    samples[0] = new Vector2(0.0f, 0.0f);
                    break;
                case SuperSamplingCount.X2:
                    samples[0] = new Vector2(4.0f, 4.0f);
                    samples[1] = new Vector2(-4.0f, -4.0f);
                    break;
                case SuperSamplingCount.X4:
                    samples[0] = new Vector2(-2.0f, -6.0f);
                    samples[1] = new Vector2(6.0f, -2.0f);
                    samples[2] = new Vector2(-6.0f, 2.0f);
                    samples[3] = new Vector2(2.0f, 6.0f);
                    break;
                case SuperSamplingCount.X8:
                    samples[0] = new Vector2(1.0f, -3.0f);
                    samples[1] = new Vector2(-1.0f, 3.0f);
                    samples[2] = new Vector2(5.0f, 1.0f);
                    samples[3] = new Vector2(-3.0f, -5.0f);

                    samples[4] = new Vector2(-5.0f, 5.0f);
                    samples[5] = new Vector2(-7.0f, -1.0f);
                    samples[6] = new Vector2(3.0f, 7.0f);
                    samples[7] = new Vector2(7.0f, -7.0f);
                    break;
                case SuperSamplingCount.X16:
                    samples[0] = new Vector2(1.0f, 1.0f);
                    samples[1] = new Vector2(-1.0f, -3.0f);
                    samples[2] = new Vector2(-3.0f, 2.0f);
                    samples[3] = new Vector2(4.0f, -1.0f);

                    samples[4] = new Vector2(-5.0f, -2.0f);
                    samples[5] = new Vector2(2.0f, 5.0f);
                    samples[6] = new Vector2(5.0f, 3.0f);
                    samples[7] = new Vector2(3.0f, -5.0f);

                    samples[8] = new Vector2(-2.0f, 6.0f);
                    samples[9] = new Vector2(0.0f, -7.0f);
                    samples[10] = new Vector2(-4.0f, -6.0f);
                    samples[11] = new Vector2(-6.0f, 4.0f);

                    samples[12] = new Vector2(-8.0f, 0.0f);
                    samples[13] = new Vector2(7.0f, -4.0f);
                    samples[14] = new Vector2(6.0f, 7.0f);
                    samples[15] = new Vector2(-7.0f, -8.0f);
                    break;
                default:
                    Debug.LogError("Not expected sample count: " + sc);
                    return;
            }
            const float oneOverSixteen = 1.0f / 16.0f;
            Vector2 halfHalf = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = samples[i] * oneOverSixteen + halfHalf;
            }
        }

        protected internal override void BeginRecording(RecordingSession session)
        {
            superShader = Shader.Find("Hidden/Volund/BS4SuperShader");
            accumulateShader = Shader.Find("Hidden/BeautyShot/Accumulate");
            normalizeShader = Shader.Find("Hidden/BeautyShot/Normalize");

            var movieRecorderSettings = session.settings as MovieRecorderSettings;
            bool encoderAlreadyFlips = false;
            if (movieRecorderSettings != null)
            {
                encoderAlreadyFlips = movieRecorderSettings.encodersRegistered[movieRecorderSettings.encoderSelected].PerformsVerticalFlip;
            }
            NeedToFlipVertically = UnityHelpers.NeedToActuallyFlip(rtsSettings.FlipFinalOutput, this, encoderAlreadyFlips);

            var requiredColorSpace = ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;
            if (session.settings is ImageRecorderSettings)
            {
                requiredColorSpace = ((ImageRecorderSettings)session.settings).OutputColorSpaceComputed;
            }
            else if (session.settings is MovieRecorderSettings)
            {
                requiredColorSpace = ImageRecorderSettings.ColorSpaceType.sRGB_sRGB; // always sRGB
            }

            var projectColorSpace = PlayerSettings.colorSpace;

            // Log warnings in unsupported contexts
            if (projectColorSpace == ColorSpace.Gamma)
            {
                if (requiredColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB)
                    Debug.LogWarning(
                        $"Gamma color space does not support linear output format. This operation is not supported.");
            }

            // We convert from linear to sRGB if the project is linear + the source RT is linear + the output color space is sRGB
            m_needToConvertLinearToSRGB = projectColorSpace == ColorSpace.Linear && requiredColorSpace == ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;

            // We convert from sRGB to linear if the RT is sRGB (gamma) and the output color space is linear (e.g., linear EXR)
            m_needToConvertSRGBToLinear = projectColorSpace == ColorSpace.Gamma && requiredColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;

            var h = rtsSettings.OutputHeight;

            // Below here is considered 'void Start()', but we run it for directly "various reasons".
            if (h > rtsSettings.RenderHeight)
                throw new UnityException("Upscaling is not supported! Output dimension must be smaller or equal to render dimension.");

            // Calculate aspect and render/output sizes
            // Clamp size to 16K, which is the min always supported size in d3d11
            // Force output to divisible by two as x264 doesn't approve of odd image dimensions.
            //var aspect = rtsSettings.m_OutputAspect.GetAspect();
            m_renderHeight = Mathf.Min(16 * 1024, Mathf.RoundToInt(rtsSettings.RenderHeight)); //rtsSettings.renderHeight; //m_RenderSize;
            m_renderWidth = Mathf.Min(16 * 1024, Mathf.RoundToInt(rtsSettings.RenderWidth));

            OutputHeight = h;
            OutputWidth = rtsSettings.OutputWidth;

            m_superMaterial = new Material(superShader) { hideFlags = HideFlags.DontSave };

            m_accumulateMaterial = new Material(accumulateShader) { hideFlags = HideFlags.DontSave };

            m_normalizeMaterial = new Material(normalizeShader) { hideFlags = HideFlags.DontSave };

            m_renderRT = new RenderTexture(m_renderWidth, m_renderHeight, 24, RenderTextureFormat.DefaultHDR,
                RenderTextureReadWrite.Linear) { wrapMode = TextureWrapMode.Clamp };

            for (int i = 0; i < 2; ++i)
            {
                m_accumulateRTs[i] = new RenderTexture(m_renderWidth, m_renderHeight, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                m_accumulateRTs[i].Create();
            }

            var rt = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            rt.Create();
            OutputRenderTexture = rt;
            m_samples = new Vector2[(int)rtsSettings.SuperSampling];
            GenerateSamplesMSAA(m_samples, rtsSettings.SuperSampling);

            m_hookedCameras = new List<HookedCamera>();
        }

        protected internal override void NewFrameStarting(RecordingSession session)
        {
            switch (rtsSettings.source)
            {
                case ImageSource.ActiveCamera:
                {
                    bool sort = false;

                    // Find all cameras targetting Display
                    foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>())
                    {
                        var hookedCam = m_hookedCameras.Find((x) => cam == x.camera);
                        if (hookedCam != null)
                        {
                            // Should we keep it?
                            if (cam.targetDisplay != 0 || !cam.enabled)
                            {
                                UnityHelpers.Destroy(cam.targetTexture);
                                cam.targetTexture = hookedCam.textureBackup;
                                m_hookedCameras.Remove(hookedCam);
                            }
                            continue;
                        }

                        if (!cam.enabled || !cam.gameObject.activeInHierarchy || cam.targetDisplay != 0)
                            continue;

                        hookedCam = new HookedCamera() { camera = cam, textureBackup = cam.targetTexture };
                        var camRT = new RenderTexture((int)(m_renderWidth * cam.rect.width), (int)(m_renderHeight * cam.rect.height), 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
                        cam.targetTexture = camRT;
                        m_hookedCameras.Add(hookedCam);
                        sort = true;
                    }

                    if (sort)
                    {
                        m_hookedCameras.Sort((x, y) => x.camera.depth<y.camera.depth ? -1 : x.camera.depth> y.camera.depth ? 1 : 0);
                    }
                    break;
                }
                case ImageSource.MainCamera:
                {
                    var cam = Camera.main;
                    if (m_hookedCameras.Count > 0)
                    {
                        if (m_hookedCameras[0].camera != cam)
                        {
                            m_hookedCameras[0].camera.targetTexture = m_hookedCameras[0].textureBackup;
                            m_hookedCameras.Clear();
                        }
                        else
                            break;
                    }
                    if (!cam.enabled)
                        break;

                    var hookedCam = new HookedCamera() { camera = cam, textureBackup = cam.targetTexture };
                    cam.targetTexture = m_renderRT;
                    m_hookedCameras.Add(hookedCam);
                    break;
                }
                case ImageSource.TaggedCamera:
                {
                    GameObject[] taggedObjs;
                    var tag = (settings as RenderTextureSamplerSettings).CameraTag;
                    try
                    {
                        taggedObjs = GameObject.FindGameObjectsWithTag(tag);
                    }
                    catch (UnityException)
                    {
                        Debug.LogWarning("No camera has the requested target tag:" + tag);
                        taggedObjs = new GameObject[0];
                    }

                    // Remove un-tagged cameras form list
                    for (int i = m_hookedCameras.Count - 1; i >= 0; i--)
                    {
                        if (!m_hookedCameras[i].camera.gameObject.CompareTag(tag))
                        {
                            // un-hook it
                            m_hookedCameras[i].camera.targetTexture = m_hookedCameras[i].textureBackup;
                            m_hookedCameras.RemoveAt(i);
                        }
                    }

                    // Add newly tagged cameras
                    foreach (var t in taggedObjs)
                    {
                        var found = false;
                        var cam = t.transform.GetComponent<Camera>();
                        if (cam != null && cam.enabled)
                        {
                            if (m_hookedCameras.Any(t1 => t1.camera == t.transform.GetComponent<Camera>()))
                            {
                                found = true;
                            }

                            if (!found)
                            {
                                var hookedCam = new HookedCamera { camera = cam, textureBackup = cam.targetTexture };
                                cam.targetTexture = m_renderRT;
                                m_hookedCameras.Add(hookedCam);
                            }
                        }
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_hookedCameras != null)
                {
                    foreach (var c in m_hookedCameras)
                    {
                        if (c != null)
                        {
                            if (c.camera.rect.width == 1f && c.camera.rect.height == 1f)
                                UnityHelpers.Destroy(c.camera.targetTexture);
                            c.camera.targetTexture = c.textureBackup;
                        }
                    }
                    m_hookedCameras.Clear();
                }

                UnityHelpers.Destroy(m_renderRT);
                foreach (var rt in m_accumulateRTs)
                    UnityHelpers.Destroy(rt);
                UnityHelpers.Destroy(m_superMaterial);
                UnityHelpers.Destroy(m_accumulateMaterial);
                UnityHelpers.Destroy(m_normalizeMaterial);
            }

            base.Dispose(disposing);
        }

        protected internal override void NewFrameReady(RecordingSession session)
        {
            PerformSubSampling();

            if (rtsSettings.RenderHeight == rtsSettings.OutputHeight)
            {
                // Blit with normalization if sizes match.
                m_normalizeMaterial.SetFloat("_NormalizationFactor", 1.0f / (float)rtsSettings.SuperSampling);
                if (NeedToFlipVertically.Value)
                    m_normalizeMaterial.EnableKeyword("VERTICAL_FLIP");
                if (m_needToConvertLinearToSRGB)
                    m_normalizeMaterial.EnableKeyword("SRGB_CONVERSION");
                else if (m_needToConvertSRGBToLinear)
                    m_normalizeMaterial.EnableKeyword("LINEAR_CONVERSION");
                Graphics.Blit(m_renderRT, OutputRenderTexture, m_normalizeMaterial);
            }
            else
            {
                // Ideally we would use a separable filter here, but we're massively bound by readback and disk anyway for hi-res.
                m_superMaterial.SetVector("_Target_TexelSize", new Vector4(1f / OutputWidth, 1f / OutputHeight, OutputWidth, OutputHeight));
                m_superMaterial.SetFloat("_KernelCosPower", rtsSettings.superKernelPower);
                m_superMaterial.SetFloat("_KernelScale", rtsSettings.superKernelScale);
                m_superMaterial.SetFloat("_NormalizationFactor", 1.0f / (float)rtsSettings.SuperSampling);
                if (NeedToFlipVertically.Value)
                    m_superMaterial.EnableKeyword("VERTICAL_FLIP");
                if (m_needToConvertLinearToSRGB)
                    m_superMaterial.EnableKeyword("SRGB_CONVERSION");
                else if (m_needToConvertSRGBToLinear)
                    m_superMaterial.EnableKeyword("LINEAR_CONVERSION");
                Graphics.Blit(m_renderRT, OutputRenderTexture, m_superMaterial);
            }
        }

        void ShiftProjectionMatrix(Camera camera, Vector2 sample)
        {
            Matrix4x4 projectionMatrix = camera.projectionMatrix;
            float dx = sample.x / m_renderWidth;
            float dy = sample.y / m_renderHeight;
            projectionMatrix.m02 += dx;
            projectionMatrix.m12 += dy;
            camera.projectionMatrix = projectionMatrix;
        }

        bool CameraUsingPartialViewport(Camera cam)
        {
            return cam.rect.width != 1 || cam.rect.height != 1 || cam.rect.x != 0 || cam.rect.y != 0;
        }

        void PerformSubSampling()
        {
            RenderTexture accumulateInto = null;
            m_renderRT.wrapMode = TextureWrapMode.Clamp;
            m_renderRT.filterMode = FilterMode.Point;

            int x = 0;
            Graphics.SetRenderTarget(m_accumulateRTs[0]);
            GL.Clear(false, true, Color.black);

            foreach (var hookedCam in m_hookedCameras)
            {
                var cam = hookedCam.camera;
                // We need to remember if the camera uses physical properties
                bool usePhysicalProperties = cam.usePhysicalProperties;

                for (int i = 0, n = (int)rtsSettings.SuperSampling; i < n; i++)
                {
                    var oldProjectionMatrix = cam.projectionMatrix;
                    var oldRect = cam.rect;
                    cam.rect  = new Rect(0f, 0f, 1f, 1f);
                    ShiftProjectionMatrix(cam, m_samples[i] - new Vector2(0.5f, 0.5f));
                    cam.Render();
                    cam.projectionMatrix = oldProjectionMatrix;
                    cam.rect = oldRect;

                    accumulateInto = m_accumulateRTs[(x + 1) % 2];
                    var accumulatedWith = m_accumulateRTs[x % 2];
                    m_accumulateMaterial.SetTexture("_PreviousTexture", accumulatedWith);

                    if (CameraUsingPartialViewport(cam))
                    {
                        m_accumulateMaterial.SetFloat("_OfsX", cam.rect.x);
                        m_accumulateMaterial.SetFloat("_OfsY", cam.rect.y);
                        m_accumulateMaterial.SetFloat("_Width", cam.rect.width);
                        m_accumulateMaterial.SetFloat("_Height", cam.rect.height);
                        m_accumulateMaterial.SetFloat("_Scale", cam.targetTexture.width / (float)m_renderRT.width);
                    }
                    else
                    {
                        m_accumulateMaterial.SetFloat("_OfsX", 0);
                        m_accumulateMaterial.SetFloat("_OfsY", 0);
                        m_accumulateMaterial.SetFloat("_Width", 1);
                        m_accumulateMaterial.SetFloat("_Height", 1);
                        m_accumulateMaterial.SetFloat("_Scale", 1);
                    }
                    m_accumulateMaterial.SetInt("_Pass", i);
                    Graphics.Blit(cam.targetTexture, accumulateInto, m_accumulateMaterial);
                    x++;
                }
                // It is really important to call ResetProjectionMatrix to insure the
                // camera will be recomputed and reflect the normal camera's parameters.
                // Call this to end the effect of setting projectionMatrix. FTV-824
                cam.ResetProjectionMatrix();
                // If the camera was using physical properties we need reset the camera
                // to keep using physical properties.
                cam.usePhysicalProperties = usePhysicalProperties;
            }

            Graphics.Blit(accumulateInto, m_renderRT);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);
            NeedToFlipVertically = null; // This variable is not valid anymore
        }
    }
}
