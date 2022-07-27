using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    class RenderTextureInput : BaseRenderTextureInput
    {
        // Whether or not the incoming RenderTexture must be converted from linear to sRGB color space
        private bool m_needToConvertLinearToSRGB = false;

        // Whether or not the incoming RenderTexture must be converted from sRGB to linear color space
        private bool m_needToConvertSRGBToLinear = false;

        RenderTextureInputSettings cbSettings => (RenderTextureInputSettings)settings;

        // An internal RenderTexture copied from the input RenderTexture when vertical flip and/or sRGB conversion need to be performed
        internal RenderTexture workTexture = null;

        // A material to perform vertical flips and/or sRGB conversion of a RenderTexture
        private Material MaterialRenderTextureCopy
        {
            get
            {
                if (_matSRGBConversion == null)
                    _matSRGBConversion = new Material(Shader.Find("Hidden/RenderTextureCopy"));

                if (NeedToFlipVertically != null && NeedToFlipVertically.Value)
                    _matSRGBConversion.EnableKeyword("VERTICAL_FLIP");
                if (m_needToConvertLinearToSRGB)
                    _matSRGBConversion.EnableKeyword("SRGB_CONVERSION");
                else if (m_needToConvertSRGBToLinear)
                    _matSRGBConversion.EnableKeyword("LINEAR_CONVERSION");
                return _matSRGBConversion;
            }
        }
        private Material _matSRGBConversion = null; // a shader for doing linear to sRGB conversion

        protected internal override void BeginRecording(RecordingSession session)
        {
            if (cbSettings.renderTexture == null)
                return; // error will have been triggered in RenderTextureInputSettings.CheckForErrors()

            OutputHeight = cbSettings.OutputHeight;
            OutputWidth = cbSettings.OutputWidth;
            OutputRenderTexture = cbSettings.renderTexture;

            var encoderAlreadyFlips = session.settings.EncoderAlreadyFlips();
            NeedToFlipVertically = UnityHelpers.NeedToActuallyFlip(cbSettings.FlipFinalOutput, this, encoderAlreadyFlips);

            var requiredColorSpace = ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;
            if (session.settings is ImageRecorderSettings)
            {
                requiredColorSpace = ((ImageRecorderSettings)session.settings).OutputColorSpaceComputed;
            }
            else if (session.settings is MovieRecorderSettings)
            {
                requiredColorSpace = ImageRecorderSettings.ColorSpaceType.sRGB_sRGB; // always sRGB
            }
            var renderTextureColorSpace = UnityHelpers.GetColorSpaceType(cbSettings.renderTexture.graphicsFormat); // the color space of the RenderTexture
            var projectColorSpace = PlayerSettings.colorSpace;

            // Log warnings in unsupported contexts
            if (projectColorSpace == ColorSpace.Gamma)
            {
                if (requiredColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB)
                    Debug.LogWarning($"Gamma color space does not support linear output format. This operation is not supported.");

                if (renderTextureColorSpace != ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB)
                    Debug.LogWarning($"Gamma color space does not support non-linear textures. This operation is not supported.");
            }

            // We convert from linear to sRGB if the project is linear + the source RT is linear + the output color space is sRGB
            m_needToConvertLinearToSRGB = (projectColorSpace == ColorSpace.Linear && renderTextureColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB) && requiredColorSpace == ImageRecorderSettings.ColorSpaceType.sRGB_sRGB;

            // We convert from sRGB to linear if the RT is sRGB (gamma) and the output color space is linear (e.g., linear EXR)
            m_needToConvertSRGBToLinear = renderTextureColorSpace == ImageRecorderSettings.ColorSpaceType.sRGB_sRGB && requiredColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;

            if (NeedToFlipVertically.Value || m_needToConvertLinearToSRGB || m_needToConvertSRGBToLinear)
            {
                workTexture = new RenderTexture(OutputRenderTexture);
                workTexture.name = "RenderTextureInput_intermediate";
            }
        }

        protected internal override void NewFrameReady(RecordingSession session)
        {
            if (cbSettings.renderTexture == null)
                return; // error will have been triggered in BeginRecording()

            if (!cbSettings.renderTexture.IsCreated())
                Debug.LogError($"The render texture '{cbSettings.renderTexture.name}' was not created before being sampled. Its content is invalid.");

            if ((NeedToFlipVertically != null && NeedToFlipVertically.Value) || m_needToConvertLinearToSRGB || m_needToConvertSRGBToLinear)
            {
                // Perform the actual conversion
                var rememberActive = RenderTexture.active;
                Graphics.Blit(cbSettings.renderTexture, workTexture, MaterialRenderTextureCopy);
                RenderTexture.active = rememberActive; // restore active RT

                // Make the final texture use the modified texture
                OutputRenderTexture = workTexture;
            }
            else
            {
                // Just use the input without any changes
                OutputRenderTexture = cbSettings.renderTexture;
            }

            base.NewFrameReady(session);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);

            if (workTexture != null)
            {
                UnityHelpers.Destroy(workTexture);
                workTexture = null;
            }

            NeedToFlipVertically = null; // This variable is not valid anymore
        }
    }
}
