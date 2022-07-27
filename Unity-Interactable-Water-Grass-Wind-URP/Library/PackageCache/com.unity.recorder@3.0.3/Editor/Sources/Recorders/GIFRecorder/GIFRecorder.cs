using System;
using UnityEngine;
using UnityEditor.Recorder.FrameCapturer;

namespace UnityEditor.Recorder
{
#pragma warning disable 618
    class GIFRecorder : GenericRecorder<GIFRecorderSettings>
    {
#pragma warning restore 618
        protected internal override void RecordFrame(RecordingSession session)
        {
        }
    }
}
