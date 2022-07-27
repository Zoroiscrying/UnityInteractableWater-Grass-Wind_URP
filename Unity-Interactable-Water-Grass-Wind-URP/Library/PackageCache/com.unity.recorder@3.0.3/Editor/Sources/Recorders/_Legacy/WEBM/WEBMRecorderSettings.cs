namespace UnityEditor.Recorder.FrameCapturer
{
    [RecorderSettings(typeof(WEBMRecorder), "Legacy/WebM", true)]
#pragma warning disable 618
    class WEBMRecorderSettings : BaseFCRecorderSettings
    {
#pragma warning restore 618
        protected internal override string Extension
        {
            get { return "webm"; }
        }
    }
}
