using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Recorder.AOV;
using UnityEditor.Recorder.AOV.Input;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.Recorder
{
    class AOVRecorder : BaseTextureRecorder<AOVRecorderSettings>
    {
        Queue<string> m_PathQueue = new Queue<string>();
        protected override TextureFormat ReadbackTextureFormat
        {
            get
            {
                return Settings.m_OutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.EXR
                    ? TextureFormat.RGBA32
                    : TextureFormat.RGBAFloat;
            }
        }

        protected internal override bool BeginRecording(RecordingSession session)
        {
#if !HDRP_AVAILABLE
            // This can happen with an AOV Recorder Clip in a project after removing HDRP
            return settings.HasErrors();
#else
            if (!base.BeginRecording(session))
            {
                return false;
            }

            if (settings.HasErrors())
            {
                Debug.LogError($"The '{settings.name}' AOV Recorder has errors and cannot record any data.");
                return false;
            }

            // Did the user request a vertically flipped image? This is not supported.
            var input = settings.InputsSettings.First() as AOVCameraInputSettings;
            if (input != null && input.FlipFinalOutput)
            {
                Debug.LogWarning($"The '{settings.name}' AOV Recorder can't vertically flip the image as requested. This option is not supported in AOV recording context.");
            }

            Settings.FileNameGenerator.CreateDirectory(session);
            return true;
#endif
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);
        }

        protected internal override void RecordFrame(RecordingSession session)
        {
            if (settings.HasErrors())
                return;

            if (m_Inputs.Count != 1)
                throw new Exception("Unsupported number of sources");
            // Store path name for this frame into a queue, as WriteFrame may be called
            // asynchronously later on, when the current frame is no longer the same (thus creating
            // a file name that isn't in sync with the session's current frame).
            m_PathQueue.Enqueue(Settings.FileNameGenerator.BuildAbsolutePath(session));
            base.RecordFrame(session);
        }

        protected override void WriteFrame(Texture2D tex)
        {
            byte[] bytes;

            Profiler.BeginSample("AOVRecorder.EncodeImage");
            try
            {
                switch (Settings.m_OutputFormat)
                {
                    case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                    {
                        bytes = tex.EncodeToEXR(ImageRecorderSettings.ToNativeType(Settings.EXRCompression));
                        WriteToFile(bytes);
                        break;
                    }
                    case ImageRecorderSettings.ImageRecorderOutputFormat.PNG:
                        bytes = tex.EncodeToPNG();
                        WriteToFile(bytes);
                        break;
                    case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                        bytes = tex.EncodeToJPG();
                        WriteToFile(bytes);
                        break;
                    default:
                        Profiler.EndSample();
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                Profiler.EndSample();
            }

            if (m_Inputs[0] is BaseRenderTextureInput || Settings.m_OutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
                UnityHelpers.Destroy(tex);
        }

        private void WriteToFile(byte[] bytes)
        {
            Profiler.BeginSample("AOVRecorder.WriteToFile");
            File.WriteAllBytes(m_PathQueue.Dequeue(), bytes);
            Profiler.EndSample();
        }
    }
}
