namespace UnityEditor.Recorder.FrameCapturer
{
    [RecorderSettings(typeof(PNGRecorder), "Legacy/PNG", true)]
#pragma warning disable 618
    class PNGRecorderSettings : BaseFCRecorderSettings
    {
#pragma warning restore 618
        protected internal override string Extension
        {
            get { return "png"; }
        }
    }
}
