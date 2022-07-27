using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Recorder.FrameCapturer
{
    /// <summary>
    /// The settings common to all recordings that capture image data.
    /// </summary>
    [Obsolete("The legacy recorders are deprecated")]
    public abstract class BaseFCRecorderSettings : RecorderSettings
    {
        [SerializeField] internal UTJImageInputSelector m_ImageInputSelector = new UTJImageInputSelector();

        /// <summary>
        /// The properties of the image input.
        /// </summary>
        public ImageInputSettings imageInputSettings
        {
            get { return m_ImageInputSelector.imageInputSettings; }
            set { m_ImageInputSelector.imageInputSettings = value; }
        }

        /// <summary>
        /// The list of settings of the Recorder inputs.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_ImageInputSelector.Selected; }
        }
    }
}
