using UnityEngine;

namespace UnityEditor.Recorder.FrameCapturer
{
    [RecorderSettings(typeof(MP4Recorder), "Legacy/MP4", true)]
#pragma warning disable 618
    class MP4RecorderSettings : BaseFCRecorderSettings
    {
#pragma warning restore 618
        protected internal override string Extension
        {
            get { return "mp4"; }
        }
    }
}
