using System;
using UnityEditor.Recorder.FrameCapturer;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Deprecated GIF Recorder.
    /// </summary>
    [Obsolete("The GIFRecorder is deprecated")]
    [RecorderSettings(typeof(GIFRecorder), "GIF Animation", "imagesequence_16", true)]
    public class GIFRecorderSettings : BaseFCRecorderSettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        [Obsolete("The GIFRecorder is deprecated")]
        public GIFRecorderSettings()
        {
        }

        /// <inheritdoc/>
        protected internal override string Extension
        {
            get { return "gif"; }
        }
    }
}
