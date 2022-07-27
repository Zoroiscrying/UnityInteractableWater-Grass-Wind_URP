using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class that represents the settings of an Image Recorder.
    /// </summary>
    [RecorderSettings(typeof(ImageRecorder), "Image Sequence", "imagesequence_16")]
    public class ImageRecorderSettings : RecorderSettings, IAccumulation
    {
        /// <summary>
        /// Available options for the output image format used by Image Sequence Recorder.
        /// </summary>
        public enum ImageRecorderOutputFormat
        {
            /// <summary>
            /// Output the recording in PNG format.
            /// </summary>
            PNG,
            /// <summary>
            /// Output the recording in JPEG format.
            /// </summary>
            JPEG,
            /// <summary>
            /// Output the recording in EXR format.
            /// </summary>
            EXR
        }

        /// <summary>
        /// Compression type for EXR files.
        /// </summary>
        public enum EXRCompressionType
        {
            /// <summary>
            /// No compression.
            /// </summary>
            None,
            /// <summary>
            /// Run-length encoding compression.
            /// </summary>
            RLE,
            /// <summary>
            /// Zip compression.
            /// </summary>
            Zip
        }

        static internal Texture2D.EXRFlags ToNativeType(EXRCompressionType type)
        {
            Texture2D.EXRFlags nativeType = Texture2D.EXRFlags.None;
            switch (type)
            {
                case ImageRecorderSettings.EXRCompressionType.RLE:
                    nativeType = Texture2D.EXRFlags.CompressRLE;
                    break;
                case ImageRecorderSettings.EXRCompressionType.Zip:
                    nativeType = Texture2D.EXRFlags.CompressZIP;
                    break;
                case ImageRecorderSettings.EXRCompressionType.None:
                    nativeType = Texture2D.EXRFlags.None;
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected compression type '{type}'.");
            }

            return nativeType;
        }

        /// <summary>
        /// Color Space (gamma curve, gamut) to use in the output images.
        /// </summary>
        public enum ColorSpaceType
        {
            /// <summary>
            /// The sRGB color space.
            /// </summary>
            sRGB_sRGB,
            /// <summary>
            /// The linear sRGB color space.
            /// </summary>
            Unclamped_linear_sRGB
        }

        /// <summary>
        /// Stores the output image format currently used for this Recorder.
        /// </summary>
        public ImageRecorderOutputFormat OutputFormat
        {
            get { return outputFormat; }
            set { outputFormat = value; }
        }

        [SerializeField] ImageRecorderOutputFormat outputFormat = ImageRecorderOutputFormat.JPEG;

        /// <summary>
        /// Use this property to capture the alpha channel (True) or not (False) in the output.
        /// </summary>
        /// <remarks>
        /// Alpha channel is captured only if the output image format supports it.
        /// </remarks>
        public bool CaptureAlpha
        {
            get { return captureAlpha; }
            set { captureAlpha = value; }
        }

        [SerializeField] private bool captureAlpha;


        /// <summary>
        /// Use this property to capture the frames in HDR (if the setup supports it).
        /// </summary>
        public bool CaptureHDR
        {
            get { return CanCaptureHDRFrames() && m_ColorSpace == ColorSpaceType.Unclamped_linear_sRGB; }
        }


        [SerializeField] ImageInputSelector m_ImageInputSelector = new ImageInputSelector();
        [SerializeField] internal ImageRecorderSettings.EXRCompressionType m_EXRCompression = ImageRecorderSettings.EXRCompressionType.Zip;
        [SerializeField] internal ColorSpaceType m_ColorSpace = ColorSpaceType.Unclamped_linear_sRGB;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ImageRecorderSettings()
        {
            fileNameGenerator.FileName = "image_" + DefaultWildcard.Take + "_" + DefaultWildcard.Frame;
        }

        /// <inheritdoc/>
        protected internal override string Extension
        {
            get
            {
                switch (OutputFormat)
                {
                    case ImageRecorderOutputFormat.PNG:
                        return "png";
                    case ImageRecorderOutputFormat.JPEG:
                        return "jpg";
                    case ImageRecorderOutputFormat.EXR:
                        return "exr";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Stores the data compression method to use to encode image files in the EXR format.
        /// </summary>
        public EXRCompressionType EXRCompression
        {
            get => m_EXRCompression;
            set => m_EXRCompression = value;
        }

        /// <summary>
        /// Stores the color space to use to encode the output image files.
        /// </summary>
        public ColorSpaceType OutputColorSpace
        {
            get => m_ColorSpace;
            set => m_ColorSpace = value;
        }

        // This is necessary because the value in OutputColorSpace might hold invalid information (e.g. for PNG and JPEG it
        // could say Linear) because the value doesn't change when the output format is changed.
        // See the handling of the color space dropdown in ImageRecorderEditor.FileTypeAndFormatGUI.
        internal ColorSpaceType OutputColorSpaceComputed
        {
            get
            {
                switch (OutputFormat)
                {
                    case ImageRecorderOutputFormat.PNG:
                    case ImageRecorderOutputFormat.JPEG:
                        return ColorSpaceType.sRGB_sRGB; // these formats must always be sRGB
                    case ImageRecorderOutputFormat.EXR:
                        if (CanCaptureHDRFrames())
                            return OutputColorSpace;
                        else
                            return ColorSpaceType.sRGB_sRGB; // must be sRGB
                    default:
                        throw new InvalidEnumArgumentException($"Unexpected output format {OutputFormat}");
                }
            }
        }

        /// <summary>
        /// The settings of the input image.
        /// </summary>
        public ImageInputSettings imageInputSettings
        {
            get { return m_ImageInputSelector.ImageInputSettings; }
            set { m_ImageInputSelector.ImageInputSettings = value; }
        }

        /// <summary>
        /// The list of settings of the Recorder Inputs.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_ImageInputSelector.Selected; }
        }

        internal bool CanCaptureHDRFrames()
        {
            bool isGameViewInput = imageInputSettings.InputType == typeof(GameViewInput);
            bool isFormatExr = OutputFormat == ImageRecorderOutputFormat.EXR;
            return !isGameViewInput && isFormatExr && UnityHelpers.UsingHDRP();
        }

        internal bool CanCaptureAlpha()
        {
            bool formatSupportAlpha = OutputFormat == ImageRecorderOutputFormat.PNG ||
                OutputFormat == ImageRecorderOutputFormat.EXR;
            bool inputSupportAlpha = imageInputSettings.SupportsTransparent;
            return (formatSupportAlpha && inputSupportAlpha && !UnityHelpers.UsingURP());
        }

        internal override void SelfAdjustSettings()
        {
            var input = m_ImageInputSelector.Selected;

            if (input == null)
                return;
            var renderTextureSamplerSettings = input as RenderTextureSamplerSettings;
            if (renderTextureSamplerSettings != null)
            {
                var colorSpace = OutputFormat == ImageRecorderOutputFormat.EXR ? UnityEngine.ColorSpace.Linear : UnityEngine.ColorSpace.Gamma;
                renderTextureSamplerSettings.ColorSpace = colorSpace;
            }

            var cbis = input as CameraInputSettings;
            if (cbis != null)
            {
                cbis.RecordTransparency = CanCaptureAlpha() && CaptureAlpha;
            }
        }

        [SerializeReference] AccumulationSettings _accumulationSettings = new AccumulationSettings();

        /// <summary>
        /// Stores the AccumulationSettings properties
        /// </summary>
        public AccumulationSettings AccumulationSettings
        {
            get { return _accumulationSettings; }
            set { _accumulationSettings = value; }
        }

        /// <summary>
        /// Use this method to get all the AccumulationSettings properties.
        /// </summary>
        /// <returns>AccumulationSettings</returns>
        public AccumulationSettings GetAccumulationSettings()
        {
            return AccumulationSettings;
        }

        /// <inheritdoc/>
        public override bool IsAccumulationSupported()
        {
            if (GetAccumulationSettings() != null)
            {
                var cis = m_ImageInputSelector.Selected as CameraInputSettings;
                var gis = m_ImageInputSelector.Selected as GameViewInputSettings;
                if (cis != null || gis != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
