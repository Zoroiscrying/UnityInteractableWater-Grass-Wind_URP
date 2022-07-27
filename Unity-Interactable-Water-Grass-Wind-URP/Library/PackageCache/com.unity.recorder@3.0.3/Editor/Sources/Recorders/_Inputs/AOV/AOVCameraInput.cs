#if HDRP_AVAILABLE
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using UnityEngine.Rendering;
using System.Linq;
using UnityEditor.Recorder.AOV;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Recorder.Input
{
    class AOVCameraAOVRequestAPIInput : CameraInput
    {
        RenderTexture m_TempRT;
        private RTHandle m_ColorRT;

        internal class AOVInfo
        {
            public AOVBuffers m_AovBuffers;
            public AOVRequest m_AovRequest;
        }

        // The dictionary of supported AOV types
        static internal Dictionary<AOVType, AOVInfo> m_Aovs = new Dictionary<AOVType, AOVInfo>
        {
            {
                AOVType.Beauty,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()),
                    m_AovBuffers = AOVBuffers.Output
                }
            },
            {
                AOVType.Albedo,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Albedo),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Normal,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Normal),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Smoothness,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Smoothness),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.AmbientOcclusion,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.AmbientOcclusion),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Metal,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Metal),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Specular,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Specular),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Alpha,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(MaterialSharedProperty.Alpha),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.DiffuseLighting,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DiffuseOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.SpecularLighting,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.SpecularOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
#if HDRP_LIGHTDECO_API
            {
                AOVType.DirectDiffuse,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DirectDiffuseOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.DirectSpecular,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.DirectSpecularOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.IndirectDiffuse,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.IndirectDiffuseOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Reflection,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.ReflectionOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Refraction,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.RefractionOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
            {
                AOVType.Emissive,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(LightingProperty.EmissiveOnly),
                    m_AovBuffers = AOVBuffers.Color
                }
            },
#endif
            {
                AOVType.MotionVectors,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(DebugFullScreen.MotionVectors),
                    m_AovBuffers = AOVBuffers.MotionVectors
                }
            },
            {
                AOVType.Depth,
                new AOVInfo()
                {
                    m_AovRequest = new AOVRequest(AOVRequest.NewDefault()).SetFullscreenOutput(DebugFullScreen.Depth),
                    m_AovBuffers = AOVBuffers.DepthStencil
                }
            }
        };


        protected RenderTexture CreateFrameBuffer(RenderTextureFormat format, int width, int height, int depth = 0, bool sRGB = false)
        {
            return new RenderTexture(width, height, depth, format, sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        }

        void EnableAOVCapture(RecordingSession session, Camera cam)
        {
            var aovRecorderSettings = session.settings as AOVRecorderSettings;

            if (aovRecorderSettings != null)
            {
                var hdAdditionalCameraData = cam.GetComponent<HDAdditionalCameraData>();
                if (hdAdditionalCameraData != null)
                {
                    if (m_TempRT == null)
                    {
                        if (aovRecorderSettings.CaptureHDR)
                        {
                            m_TempRT = CreateFrameBuffer(RenderTextureFormat.ARGBFloat, OutputWidth, OutputHeight, 0,
                                false);
                        }
                        else
                        {
                            m_TempRT = CreateFrameBuffer(RenderTextureFormat.BGRA32, OutputWidth, OutputHeight, 0,
                                true);
                        }
                    }

                    var aovRequest = new AOVRequest(AOVRequest.NewDefault());
                    var aovBuffer = AOVBuffers.Color;
                    var aovInfo = new AOVInfo();

                    if (m_Aovs.TryGetValue(aovRecorderSettings.m_AOVSelection, out aovInfo))
                    {
                        aovBuffer = aovInfo.m_AovBuffers;
                        aovRequest = aovInfo.m_AovRequest;
                    }
                    else
                    {
                        Debug.LogError($"Unrecognized AOV '{aovRecorderSettings.m_AOVSelection}'");
                    }

                    var bufAlloc = m_ColorRT ?? (m_ColorRT = RTHandles.Alloc(OutputWidth, OutputHeight,
                        colorFormat: GraphicsFormat.R32G32B32A32_SFloat, name: "m_ColorRT"));
                    var aovRequestBuilder = new AOVRequestBuilder();
                    aovRequestBuilder.Add(aovRequest,
                        bufferId => bufAlloc,
                        null,
                        new[] {aovBuffer},
                        (cmd, textures, properties) =>
                        {
                            if (m_TempRT != null)
                            {
                                cmd.Blit(textures[0], m_TempRT);
                            }
                        });
                    var aovRequestDataCollection = aovRequestBuilder.Build();
                    var previousRequests = hdAdditionalCameraData.aovRequests;
                    if (previousRequests != null && previousRequests.Any())
                    {
                        var listOfRequests = previousRequests.ToList();
                        foreach (var p in aovRequestDataCollection)
                        {
                            listOfRequests.Add(p);
                        }
                        var allRequests = new AOVRequestDataCollection(listOfRequests);
                        hdAdditionalCameraData.SetAOVRequests(allRequests);
                    }
                    else
                    {
                        hdAdditionalCameraData.SetAOVRequests(aovRequestDataCollection);
                    }
                }
            }
        }

        void ReadbackAOVCapture(RecordingSession session)
        {
            var aovRecorderSettings = session.settings as AOVRecorderSettings;

            if (aovRecorderSettings != null)
            {
                if (ReadbackTexture == null)
                {
                    ReadbackTexture = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBAFloat, false, aovRecorderSettings.CaptureHDR);
                }
                RenderTexture.active = m_TempRT;
                ReadbackTexture.ReadPixels(new Rect(0, 0, OutputWidth, OutputHeight), 0, 0, false);
                ReadbackTexture.Apply();
                RenderTexture.active = null;
            }
        }

        void DisableAOVCapture(RecordingSession session)
        {
            var aovRecorderSettings = session.settings as AOVRecorderSettings;

            if (aovRecorderSettings != null)
            {
                var add = TargetCamera.GetComponent<HDAdditionalCameraData>();
                if (add != null)
                {
                    add.SetAOVRequests(null);
                }
            }
        }

        protected internal override void NewFrameStarting(RecordingSession session)
        {
            base.NewFrameStarting(session);
            EnableAOVCapture(session, TargetCamera);
        }

        protected internal override void NewFrameReady(RecordingSession session)
        {
            ReadbackAOVCapture(session);
            base.NewFrameReady(session);
        }

        protected internal override void FrameDone(RecordingSession session)
        {
            base.FrameDone(session);
            DisableAOVCapture(session);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);
            if (m_ColorRT != null)
                UnityHelpers.Destroy(m_ColorRT);
            if (m_TempRT != null)
                UnityHelpers.Destroy(m_TempRT);
        }
    }
}
#else // HDRP_AVAILABLE
namespace UnityEditor.Recorder.Input
{
    class AOVCameraDebugFrameworkInput : CameraInput
    {
        // nop No HDRP available
    }
}
#endif
