using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Main class to use the Recorder framework via script.
    /// Controls recording states like start and stop.
    /// </summary>
    public class RecorderController
    {
        readonly SceneHook m_SceneHook;

        internal List<RecordingSession> m_RecordingSessions;
        readonly RecorderControllerSettings m_Settings;

        /// <summary>
        /// Current settings used by this RecorderControler.
        /// </summary>
        public RecorderControllerSettings Settings
        {
            get { return m_Settings; }
        }

        /// <summary>
        /// The constructor of the RecorderController.
        /// </summary>
        /// <param name="settings">The settings to be used by this RecorderController.</param>
        public RecorderController(RecorderControllerSettings settings)
        {
            m_Settings = settings;
            m_SceneHook = new SceneHook(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Prepares the recording context.
        /// To start recording once you've called this method, you must call <see cref="StartRecording"/>.
        /// </summary>
        /// <remarks>
        /// Sets up the internal data for the recording session and pauses the simulation to ensure a proper synchronization between the Recorder and the Unity Editor.
        /// </remarks>
        public void PrepareRecording()
        {
            if (!Application.isPlaying)
                throw new Exception("You can only call the PrepareRecording method in Play mode.");

            if (RecorderOptions.VerboseMode)
                Debug.Log("Prepare Recording.");

            if (m_Settings == null)
                throw new NullReferenceException("Can start recording without prefs");

            SceneHook.PrepareSessionRoot();
            m_RecordingSessions = new List<RecordingSession>();

            int numberOfSubframeRecorder = 0;
            int numberOfRecorderEnabled = 0;
            ValidateRecorderNames();

            foreach (var recorderSetting in m_Settings.RecorderSettings)
            {
                if (recorderSetting == null)
                {
                    if (RecorderOptions.VerboseMode)
                        Debug.Log("Ignoring unknown recorder.");

                    continue;
                }

                m_Settings.ApplyGlobalSetting(recorderSetting);

                if (recorderSetting.HasErrors())
                {
                    if (RecorderOptions.VerboseMode)
                        Debug.Log("Ignoring invalid recorder '" + recorderSetting.name + "'");

                    continue;
                }

                if (!recorderSetting.Enabled)
                {
                    if (RecorderOptions.VerboseMode)
                        Debug.Log("Ignoring disabled recorder '" + recorderSetting.name + "'");

                    continue;
                }

                if (recorderSetting.Enabled)
                {
                    numberOfRecorderEnabled++;
                }

                // Validate that only one recorder support enable capture SubFrames
                if (UnityHelpers.CaptureAccumulation(recorderSetting))
                {
                    numberOfSubframeRecorder++;

                    if (numberOfSubframeRecorder >= 1 && numberOfRecorderEnabled > 1)
                    {
                        Debug.LogError("You can only use one active Recorder at a time when you capture accumulation.");
                        continue;
                    }
                }

                var session = m_SceneHook.CreateRecorderSessionWithRecorderComponent(recorderSetting);

                m_RecordingSessions.Add(session);
            }
        }

        /// <summary>
        /// Starts the recording (works only in Play mode).
        /// To use this method, you must first have called <see cref="PrepareRecording"/> to set up the recording context.
        /// Also ensure that you've finished loading any additional Scene data required before you start recording.
        /// </summary>
        /// <returns>false if an error occured. The console usually contains logs about the errors.</returns>
        /// <exception cref="Exception">If not in Playmode.</exception>
        /// <exception cref="NullReferenceException">If settings is null.</exception>
        public bool StartRecording()
        {
            if (!Application.isPlaying)
                throw new Exception("You can only call the StartRecording method in Play mode.");

            if (IsRecording())
            {
                if (RecorderOptions.VerboseMode)
                    Debug.Log("Recording was already started.");

                return false;
            }

            if (RecorderOptions.VerboseMode)
                Debug.Log("Start Recording.");

            // The controller successfully starts recording if all the sessions were correctly created and at least one session begun recording properly
            var allSessionsCreated = m_RecordingSessions.All(r => r.SessionCreated());
            var atLeastOneSuccessfulRecording = false;
            foreach (var r in m_RecordingSessions)
                atLeastOneSuccessfulRecording |= r.BeginRecording();
            var success = m_RecordingSessions.Any() && allSessionsCreated && atLeastOneSuccessfulRecording;

            return success;
        }

        /// <summary>
        /// Use this method to know if all recorders are done recording.
        /// A recording stops:
        /// 1. The settings is set to a time (or frame) interval and the end time (or last frame) was reached.
        /// 2. Calling the StopRecording method.
        /// 3. Exiting Playmode.
        /// </summary>
        /// <returns>True if at least one recording is still active. false otherwise.</returns>
        /// <seealso cref="RecorderControllerSettings.SetRecordModeToSingleFrame"/>
        /// <seealso cref="RecorderControllerSettings.SetRecordModeToFrameInterval"/>
        /// <seealso cref="RecorderControllerSettings.SetRecordModeToTimeInterval"/>
        public bool IsRecording()
        {
            return m_RecordingSessions != null && m_RecordingSessions.Any(r => r.isRecording);
        }

        /// <summary>
        /// Stops all active recordings.
        /// Most recordings will create the recorded file once stopped.
        /// If the settings is using Manual recording mode. then the only way to stop recording is by calling this method or by exiting Playmode.
        /// </summary>
        /// <seealso cref="RecorderControllerSettings.SetRecordModeToManual"/>
        public void StopRecording()
        {
            if (RecorderOptions.VerboseMode)
                Debug.Log("Stop Recording.");

            if (m_RecordingSessions != null)
            {
                foreach (var session in m_RecordingSessions)
                {
                    session.Dispose();

                    if (session.recorderComponent != null)
                        UnityEngine.Object.Destroy(session.recorderComponent);
                }

                m_RecordingSessions = null;
            }
        }

        internal IEnumerable<RecordingSession> GetRecordingSessions()
        {
            return m_SceneHook.GetRecordingSessions();
        }

        internal void ValidateRecorderNames()
        {
            var outputPaths = new Dictionary<string, RecorderSettings>();
            foreach (var recSettings in m_Settings.RecorderSettings)
            {
                var path = recSettings.FileNameGenerator.BuildAbsolutePath(null);
                if (outputPaths.ContainsKey(path))
                {
                    var matchingRecorder = outputPaths[path];
                    if (!matchingRecorder.Enabled || !recSettings.Enabled) continue;

                    recSettings.IsOutputNameDuplicate = true;
                    matchingRecorder.IsOutputNameDuplicate = true;
                }
                else
                {
                    if (!recSettings.Enabled) continue;
                    recSettings.IsOutputNameDuplicate = false;
                    outputPaths.Add(path, recSettings);
                }
            }
        }
    }
}
