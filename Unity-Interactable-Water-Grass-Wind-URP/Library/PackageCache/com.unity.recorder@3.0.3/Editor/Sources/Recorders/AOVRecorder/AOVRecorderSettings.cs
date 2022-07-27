using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder.AOV
{
    /// <summary>
    /// Available options AOV Types.
    /// </summary>
    public enum AOVType
    {
        /// <summary>
        /// Select the Beauty AOV.
        /// </summary>
        Beauty = 0,
        /// <summary>
        /// Select the Albedo AOV.
        /// </summary>
        Albedo = 1,
        /// <summary>
        /// Select the Normal AOV.
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Select the Smootness AOV.
        /// </summary>
        Smoothness = 3,
        /// <summary>
        /// Select the Ambient Occlusion AOV.
        /// </summary>
        AmbientOcclusion = 4,
        /// <summary>
        /// Select the Metal AOV.
        /// </summary>
        Metal = 5,
        /// <summary>
        /// Select the Specular AOV.
        /// </summary>
        Specular = 6,
        /// <summary>
        /// Select the Alpha AOV.
        /// </summary>
        Alpha = 7,
        /// <summary>
        /// Select the Diffuse Lighting AOV.
        /// </summary>
        DiffuseLighting = 8,
        /// <summary>
        /// Select the Specular Lighting AOV.
        /// </summary>
        SpecularLighting = 9,
        /// <summary>
        /// Select the Direct Diffuse Lighting Only AOV.
        /// </summary>
        DirectDiffuse = 10,
        /// <summary>
        /// Select the Direct Specular Lighting Only AOV.
        /// </summary>
        DirectSpecular = 11,
        /// <summary>
        /// Select the Indirect Diffuse Lighting Only AOV.
        /// </summary>
        IndirectDiffuse = 12,
        /// <summary>
        /// Select the Reflection Lighting Only AOV.
        /// </summary>
        Reflection = 13,
        /// <summary>
        /// Select the Refraction Lighting Only AOV.
        /// </summary>
        Refraction = 14,
        /// <summary>
        /// Select the Emissive Only AOV.
        /// </summary>
        Emissive = 15,
        /// <summary>
        /// Select the Motion Vector AOV.
        /// </summary>
        MotionVectors = 16,
        /// <summary>
        /// Select the Depth AOV.
        /// </summary>
        Depth = 17
    }

    /// <summary>
    /// A class that represents the settings of an AOV Sequence Recorder.
    /// </summary>
    [RecorderSettings(typeof(AOVRecorder), "AOV Image Sequence", "aovimagesequence_16")]
    public class AOVRecorderSettings : RecorderSettings
    {
        [SerializeField] internal ImageRecorderSettings.ImageRecorderOutputFormat m_OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
        [SerializeField] internal AOVType m_AOVSelection = AOVType.Beauty;
        [SerializeField] internal ImageRecorderSettings.EXRCompressionType m_EXRCompression = ImageRecorderSettings.EXRCompressionType.Zip;
        [SerializeField] internal AOVImageInputSelector m_AOVImageInputSelector = new AOVImageInputSelector();
        [SerializeField] internal ImageRecorderSettings.ColorSpaceType m_ColorSpace = ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AOVRecorderSettings()
        {
            FileNameGenerator.FileName = "aov_image_" + DefaultWildcard.Frame;
        }

        protected internal override string Extension
        {
            get
            {
                switch (m_OutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        return "png";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                        return "jpg";
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                        return "exr";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Indicates the selected AOV to render.
        /// </summary>
        public AOVType AOVSelection
        {
            get => m_AOVSelection;
            set => m_AOVSelection = value;
        }

        /// <summary>
        /// Stores the color space to use to encode the output image files.
        /// </summary>
        public ImageRecorderSettings.ColorSpaceType OutputColorSpace
        {
            get => m_ColorSpace;
            set => m_ColorSpace = value;
        }

        /// <summary>
        /// Stores the data compression method to use to encode image files in the EXR format.
        /// </summary>
        public ImageRecorderSettings.EXRCompressionType EXRCompression
        {
            get => m_EXRCompression;
            set => m_EXRCompression = value;
        }

        internal bool CanCaptureHDRFrames()
        {
            bool isFormatExr = m_OutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
            return isFormatExr;
        }

        /// <summary>
        /// Stores the output image format currently used for this Recorder.
        /// </summary>
        public ImageRecorderSettings.ImageRecorderOutputFormat OutputFormat
        {
            get { return m_OutputFormat; }
            set { m_OutputFormat = value; }
        }

        /// <summary>
        /// Use this property to capture the frames in HDR (if the setup supports it).
        /// </summary>
        public bool CaptureHDR
        {
            get => CanCaptureHDRFrames() && m_ColorSpace == ImageRecorderSettings.ColorSpaceType.Unclamped_linear_sRGB;
        }

        /// <summary>
        /// The settings of the input image.
        /// </summary>
        public ImageInputSettings imageInputSettings
        {
            get { return m_AOVImageInputSelector.imageInputSettings; }
            set { m_AOVImageInputSelector.imageInputSettings = value; }
        }

        protected internal override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);

#if !HDRP_AVAILABLE
            errors.Add("HDRP package not available");
#endif
        }

#if HDRP_AVAILABLE
        // Return true if and only if the selected AOV is valid in the current context
        private bool IsAOVSelectionValid()
        {
            // See if it is found in the dictionary of supported AOVs
            return AOVCameraAOVRequestAPIInput.m_Aovs.Keys.ToList().FindIndex(k => k == m_AOVSelection) != -1;
        }

#endif

        protected internal override bool HasErrors()
        {
#if HDRP_AVAILABLE
            // Is the selection supported?
            return !IsAOVSelectionValid();
#else
            var hdrpMinVersion = 6.6; // Since 2019.2
            Enabled = false;
            Debug.LogError("AOV Recorder requires the HDRP package version " + hdrpMinVersion + " or greater to be installed");
            return true;
#endif
        }

        /// <summary>
        /// The list of settings of the Recorder Inputs.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_AOVImageInputSelector.Selected; }
        }
    }
}
