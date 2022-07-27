using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    /// <summary>
    /// A class that represents the settings of a 360 View.
    /// </summary>
    [DisplayName("360 View")]
    [Serializable]
    public class Camera360InputSettings : ImageInputSettings
    {
        /// <summary>
        /// The source camera for the 360 View.
        /// </summary>
        public ImageSource Source
        {
            get { return source; }
            set { source = value; }
        }
        [SerializeField] ImageSource source = ImageSource.MainCamera;

        /// <summary>
        /// Indicates the GameObject tag of the Camera used for the capture.
        /// </summary>
        public string CameraTag
        {
            get => cameraTag;
            set => cameraTag = value;
        }
        [SerializeField] string cameraTag;

        /// <summary>
        /// Use this property if you need to vertically flip the final output.
        /// </summary>
        public bool FlipFinalOutput
        {
            get { return flipFinalOutput; }
            set { flipFinalOutput = value; }
        }

        [SerializeField] bool flipFinalOutput = false;

        /// <summary>
        /// Use this property to render stereoscopic views in separate left and right outputs.
        /// </summary>
        public bool RenderStereo
        {
            get => renderStereo;
            set => renderStereo = value;
        }

        [SerializeField] bool renderStereo = true;

        /// <summary>
        /// Indicates the interocular angle (on the camera's Y axis) when using stereoscopic rendering.
        /// </summary>
        public float StereoSeparation
        {
            get => stereoSeparation;
            set => stereoSeparation = value;
        }

        [SerializeField] float stereoSeparation = 0.065f;


        /// <summary>
        /// Indicates the size of the cube map to use for the 360-degree environment projection.
        /// </summary>
        public int MapSize
        {
            get => mapSize;
            set => mapSize = value;
        }
        [SerializeField] int mapSize = 1024;


        [SerializeField] int m_OutputWidth = 1024;
        [SerializeField] int m_OutputHeight = 2048;

        protected internal override Type InputType
        {
            get { return typeof(Camera360Input); }
        }

        /// <summary>
        /// The width in pixels of the 360 View image.
        /// </summary>
        public override int OutputWidth
        {
            get { return m_OutputWidth; }
            set { m_OutputWidth = value; }
        }

        /// <summary>
        /// The height in pixels of the 360 View image.
        /// </summary>
        public override int OutputHeight
        {
            get { return m_OutputHeight; }
            set { m_OutputHeight = value; }
        }

        protected internal override void CheckForErrors(List<string> errors)
        {
            base.CheckForErrors(errors);

            if (source == ImageSource.TaggedCamera && string.IsNullOrEmpty(cameraTag))
                errors.Add("Missing camera tag");

            if (m_OutputWidth != (1 << (int)Math.Log(m_OutputWidth, 2)))
                errors.Add("Output width must be a power of 2.");

            if (m_OutputWidth < 128 || m_OutputWidth > 8 * 1024)
                errors.Add($"Output width must fall between {128} and {8 * 1024}.");

            if (m_OutputHeight != (1 << (int)Math.Log(m_OutputHeight, 2)))
                errors.Add("Output height must be a power of 2.");

            if (m_OutputHeight < 128 || m_OutputHeight > 8 * 1024)
                errors.Add($"Output height must fall between {128} and {8 * 1024}.");

            if (mapSize != (1 << (int)Math.Log(mapSize, 2)))
                errors.Add("Cube Map size must be a power of 2.");

            if (mapSize < 16 || mapSize > 8 * 1024)
                errors.Add(string.Format("Cube Map size must fall between {0} and {1}.", 16, 8 * 1024));

            if (renderStereo && stereoSeparation < float.Epsilon)
                errors.Add("Stereo separation value is too small.");
        }
    }
}
