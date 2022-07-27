namespace UnityEditor.Recorder.FrameCapturer
{
    [RecorderSettings(typeof(EXRRecorder), "Legacy/OpenEXR", true)]
#pragma warning disable 618
    class EXRRecorderSettings : BaseFCRecorderSettings
    {
#pragma warning restore 618
        protected internal override string Extension
        {
            get { return "exr"; }
        }
    }
}
