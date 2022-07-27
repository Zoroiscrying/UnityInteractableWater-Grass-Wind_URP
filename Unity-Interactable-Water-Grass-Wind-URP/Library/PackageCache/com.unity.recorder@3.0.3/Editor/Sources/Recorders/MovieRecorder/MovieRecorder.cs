using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Media;
using Unity.Media;
using static UnityEditor.Recorder.MovieRecorderSettings;

namespace UnityEditor.Recorder
{
    class MovieRecorder : BaseTextureRecorder<MovieRecorderSettings>
    {
        MediaEncoderHandle m_EncoderHandle = new MediaEncoderHandle();

        // The count of concurrent Movie Recorder instances. It is used to log a warning.
        static private int s_ConcurrentCount = 0;

        // Whether or not a warning was logged for concurrent movie recorders.
        static private bool s_WarnedUserOfConcurrentCount = false;

        // Whether or not recording was started properly.
        private bool m_RecordingStartedProperly = false;

        // Whether or not the recording has already been ended. To avoid messing with the count of concurrent recorders.
        private bool m_RecordingAlreadyEnded = false;

        protected override TextureFormat ReadbackTextureFormat
        {
            get
            {
                return Settings.GetCurrentEncoder().GetTextureFormat(Settings);
            }
        }

        protected internal override bool BeginRecording(RecordingSession session)
        {
            m_RecordingStartedProperly = false;
            if (!base.BeginRecording(session))
                return false;

            try
            {
                Settings.fileNameGenerator.CreateDirectory(session);
            }
            catch (Exception)
            {
                ConsoleLogMessage($"Unable to create the output directory \"{Settings.fileNameGenerator.BuildAbsolutePath(session)}\".", LogType.Error);
                Recording = false;
                return false;
            }

            var input = m_Inputs[0] as BaseRenderTextureInput;
            if (input == null)
            {
                ConsoleLogMessage("Movie Recorder could not find its input.", LogType.Error);
                Recording = false;
                return false;
            }
            int width = input.OutputWidth;
            int height = input.OutputHeight;

            var currentEncoderReg = Settings.GetCurrentEncoder();
            string errorMessage;
            var imageInputSettings = m_Inputs[0].settings as ImageInputSettings;
            var alphaWillBeInImage = imageInputSettings != null && imageInputSettings.SupportsTransparent && imageInputSettings.RecordTransparency;
            if (alphaWillBeInImage && !currentEncoderReg.SupportsTransparency(Settings, out errorMessage))
            {
                ConsoleLogMessage(errorMessage, LogType.Error);
                Recording = false;
                return false;
            }

            var videoAttrs = new VideoTrackAttributes
            {
                frameRate = RationalFromDouble(session.settings.FrameRate),
                width = (uint)width,
                height = (uint)height,
                includeAlpha = alphaWillBeInImage,
                bitRateMode = Settings.VideoBitRateMode
            };

            if (RecorderOptions.VerboseMode)
                ConsoleLogMessage(
                    $"MovieRecorder starting to write video {width}x{height}@[{videoAttrs.frameRate.numerator}/{videoAttrs.frameRate.denominator}] fps into {Settings.fileNameGenerator.BuildAbsolutePath(session)}",
                    LogType.Log);

            var audioInput = (AudioInput)m_Inputs[1];
            var audioAttrsList = new List<AudioTrackAttributes>();

            if (audioInput.audioSettings.PreserveAudio && !UnityHelpers.CaptureAccumulation(settings))
            {
#if UNITY_EDITOR_OSX
                // Special case with WebM and audio on older Apple computers: deactivate async GPU readback because there
                // is a risk of not respecting the WebM standard and receiving audio frames out of sync (see "monotonically
                // increasing timestamps"). This happens only with Target Cameras.
                if (m_Inputs[0].settings is CameraInputSettings && Settings.OutputFormat == VideoRecorderOutputFormat.WebM)
                {
                    UseAsyncGPUReadback = false;
                }
#endif
                var audioAttrs = new AudioTrackAttributes
                {
                    sampleRate = new MediaRational
                    {
                        numerator = audioInput.sampleRate,
                        denominator = 1
                    },
                    channelCount = audioInput.channelCount,
                    language = ""
                };

                audioAttrsList.Add(audioAttrs);

                if (RecorderOptions.VerboseMode)
                    ConsoleLogMessage($"Starting to write audio {audioAttrs.channelCount}ch @ {audioAttrs.sampleRate.numerator}Hz", LogType.Log);
            }
            else
            {
                if (RecorderOptions.VerboseMode)
                    ConsoleLogMessage("Starting with no audio.", LogType.Log);
            }

            try
            {
                var path =  Settings.fileNameGenerator.BuildAbsolutePath(session);

                // If an encoder already exist destroy it
                Settings.DestroyIfExists(m_EncoderHandle);

                // Get the currently selected encoder register and create an encoder
                m_EncoderHandle = currentEncoderReg.Register(Settings.m_EncoderManager);

                // Create the list of attributes for the encoder, Video, Audio and preset
                // TODO: Query the list of attributes from the encoder attributes
                var attr = new List<IMediaEncoderAttribute>();
                attr.Add(new VideoTrackMediaEncoderAttribute("VideoAttributes", videoAttrs));

                if (audioInput.audioSettings.PreserveAudio && !UnityHelpers.CaptureAccumulation(settings))
                {
                    if (audioAttrsList.Count > 0)
                    {
                        attr.Add(new AudioTrackMediaEncoderAttribute("AudioAttributes", audioAttrsList.ToArray()[0]));
                    }
                }

                attr.Add(new IntAttribute(AttributeLabels[MovieRecorderSettingsAttributes.CodecFormat], Settings.encoderPresetSelected));
                attr.Add(new IntAttribute(AttributeLabels[MovieRecorderSettingsAttributes.ColorDefinition], Settings.encoderColorDefinitionSelected));

                if (Settings.encoderPresetSelectedName == "Custom")
                {
                    // For custom
                    attr.Add(new StringAttribute(AttributeLabels[MovieRecorderSettingsAttributes.CustomOptions], Settings.encoderCustomOptions));
                }
                // Construct the encoder given the list of attributes
                Settings.m_EncoderManager.Construct(m_EncoderHandle, path, attr);

                s_ConcurrentCount++;

                m_RecordingStartedProperly = true;
                m_RecordingAlreadyEnded = false;
                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogMessage($"Unable to create encoder: '{ex.Message}'", LogType.Error);
                Recording = false;
                return false;
            }
        }

        protected internal override void RecordFrame(RecordingSession session)
        {
            if (m_Inputs.Count != 2)
                throw new Exception("Unsupported number of sources");

            if (!m_RecordingStartedProperly)
                return; // error will have been triggered in BeginRecording()

            base.RecordFrame(session);
            var audioInput = (AudioInput)m_Inputs[1];
            if (audioInput.audioSettings.PreserveAudio && !UnityHelpers.CaptureAccumulation(settings))
                Settings.m_EncoderManager.AddSamples(m_EncoderHandle, audioInput.mainBuffer);
        }

        protected internal override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);
            if (m_RecordingStartedProperly && !m_RecordingAlreadyEnded)
            {
                s_ConcurrentCount--;
                if (s_ConcurrentCount < 0)
                    ConsoleLogMessage($"Recording ended with no matching beginning recording.", LogType.Error);
                if (s_ConcurrentCount <= 1 && s_WarnedUserOfConcurrentCount)
                    s_WarnedUserOfConcurrentCount = false; // reset so that we can warn at the next occurence
                m_RecordingAlreadyEnded = true;
            }
        }

        /// <summary>
        /// Detect the ocurrence of concurrent recorders.
        /// </summary>
        private void WarnOfConcurrentRecorders()
        {
            if (s_ConcurrentCount > 1 && !s_WarnedUserOfConcurrentCount)
            {
                ConsoleLogMessage($"There are two or more concurrent Movie Recorders in your project. You should keep only one of them active per recording to avoid experiencing slowdowns or other issues.", LogType.Warning);
                s_WarnedUserOfConcurrentCount = true;
            }
        }

        protected override void WriteFrame(Texture2D t)
        {
            Settings.m_EncoderManager.AddFrame(m_EncoderHandle, t);
            WarnOfConcurrentRecorders();
        }

        // Override the parent code from BaseTextureRecorder because it converts the GPU readback request to a Texture2D, a costly operation.
        // The encoder API already provides a way to encode frames coming from a GPU readback request, leading to better performance.
        protected override void WriteFrame(AsyncGPUReadbackRequest r)
        {
            if (r.hasError)
            {
                ConsoleLogMessage("The rendered image has errors. Skipping this frame.", LogType.Error);
                return;
            }

            var format = Settings.GetCurrentEncoder().GetTextureFormat(Settings);
            Settings.m_EncoderManager.AddFrame(m_EncoderHandle, r.width, r.height, 0, format, r.GetData<byte>());
            WarnOfConcurrentRecorders();
        }

        protected override void DisposeEncoder()
        {
            if (!Settings.m_EncoderManager.Exists(m_EncoderHandle))
            {
                base.DisposeEncoder();
                return;
            }

            Settings.m_EncoderManager.Destroy(m_EncoderHandle);
            base.DisposeEncoder();

            // When adding a file to Unity's assets directory, trigger a refresh so it is detected.
            if (Settings.fileNameGenerator.Root == OutputPath.Root.AssetsFolder || Settings.fileNameGenerator.Root == OutputPath.Root.StreamingAssets)
                AssetDatabase.Refresh();
        }

        // https://stackoverflow.com/questions/26643695/converting-decimal-to-fraction-c
        static long GreatestCommonDivisor(long a, long b)
        {
            if (a == 0)
                return b;

            if (b == 0)
                return a;

            return (a < b) ? GreatestCommonDivisor(a, b % a) : GreatestCommonDivisor(b, a % b);
        }

        static MediaRational RationalFromDouble(double value)
        {
            var integral = Math.Floor(value);
            var frac = value - integral;

            const long precision = 10000000;

            var gcd = GreatestCommonDivisor((long)Math.Round(frac * precision), precision);
            var denom = precision / gcd;

            return new MediaRational()
            {
                numerator = (int)((long)integral * denom + ((long)Math.Round(frac * precision)) / gcd),
                denominator = (int)denom
            };
        }
    }
}
