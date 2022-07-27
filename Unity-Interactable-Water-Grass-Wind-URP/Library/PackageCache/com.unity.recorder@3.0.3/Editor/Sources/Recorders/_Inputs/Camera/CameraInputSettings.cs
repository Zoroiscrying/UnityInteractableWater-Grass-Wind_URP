using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Recorder.Input
{
    /// <summary>
    /// Use this class to manage all the information required to record from a Scene Camera.
    /// </summary>
    [DisplayName("Targeted Camera")]
    [Serializable]
    public class CameraInputSettings : StandardImageInputSettings
    {
        /// <summary>
        /// Indicates the Camera input type.
        /// </summary>
        public ImageSource Source
        {
            get { return source; }
            set { source = value; }
        }

        [SerializeField] private ImageSource source = ImageSource.MainCamera;

        /// <summary>
        /// Indicates the GameObject tag of the Camera used for the capture.
        /// </summary>
        public string CameraTag
        {
            get { return cameraTag; }
            set { cameraTag = value; }
        }

        [SerializeField] private string cameraTag;

        /// <summary>
        /// Use this property if you want to apply a vertical flip to the final output.
        /// </summary>
        public bool FlipFinalOutput
        {
            get { return flipFinalOutput; }
            set { flipFinalOutput = value; }
        }
        [SerializeField] private bool flipFinalOutput;

        /// <summary>
        /// Use this property to include the UI GameObjects to the recording.
        /// </summary>
        public bool CaptureUI
        {
            get { return captureUI; }
            set { captureUI = value; }
        }
        [SerializeField] private bool captureUI;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CameraInputSettings()
        {
            outputImageHeight = ImageHeight.Window;
        }

        /// <inheritdoc/>
        protected internal override Type InputType
        {
            get { return typeof(CameraInput); }
        }

        /// <inheritdoc/>
        protected internal override void CheckForErrors(List<string> errors)
        {
            base.CheckForErrors(errors);
            if (Source == ImageSource.TaggedCamera && string.IsNullOrEmpty(CameraTag))
                errors.Add("Missing tag for camera selection");
        }
    }
}
