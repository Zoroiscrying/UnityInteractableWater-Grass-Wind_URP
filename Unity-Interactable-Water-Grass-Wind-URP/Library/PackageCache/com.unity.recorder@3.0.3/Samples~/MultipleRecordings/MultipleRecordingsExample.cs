#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEngine.Recorder.Examples
{
    /// <summary>
    /// This example shows how to set up a recording session via script.
    /// To use this example, add the MultipleRecordingsExample component to a GameObject.
    ///
    /// Enter the Play Mode to start the recording.
    /// The recording automatically stops when you exit the Play Mode or when you disable the component.
    ///
    /// This script saves the recording outputs in [Project Folder]/SampleRecordings (except for the recorded animation,
    /// which is saved in Assets/SampleRecordings).
    /// </summary>
    public class MultipleRecordingsExample : MonoBehaviour
    {
        RecorderController m_RecorderController;

        void OnEnable()
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            m_RecorderController = new RecorderController(controllerSettings);

            var mediaOutputFolder = Path.Combine(Application.dataPath, "..", "SampleRecordings");
            // animation output is an asset that must be created in Assets folder
            var animationOutputFolder = Path.Combine(Application.dataPath, "SampleRecordings");

            // Video
            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "My Video Recorder";
            videoRecorder.Enabled = true;

            videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.Low;

            videoRecorder.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };

            videoRecorder.AudioInputSettings.PreserveAudio = true;
            videoRecorder.OutputFile = Path.Combine(mediaOutputFolder, "video_v") + DefaultWildcard.Take;

            // Animation
            var animationRecorder = ScriptableObject.CreateInstance<AnimationRecorderSettings>();
            animationRecorder.name = "My Animation Recorder";
            animationRecorder.Enabled = true;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            animationRecorder.AnimationInputSettings = new AnimationInputSettings
            {
                gameObject = sphere,
                Recursive = true,
            };

            animationRecorder.AnimationInputSettings.AddComponentToRecord(typeof(Transform));
            animationRecorder.OutputFile = Path.Combine(animationOutputFolder, "anim_") + DefaultWildcard.GeneratePattern("GameObject") + "_v" + DefaultWildcard.Take;

            // Image Sequence
            var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            imageRecorder.name = "My Image Recorder";
            imageRecorder.Enabled = true;

            imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            imageRecorder.CaptureAlpha = true;

            imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, "_png", "image_v") + DefaultWildcard.Take + "." + DefaultWildcard.Frame;

            imageRecorder.imageInputSettings = new CameraInputSettings
            {
                Source = ImageSource.MainCamera,
                OutputWidth = 1920,
                OutputHeight = 1080
            };

            // Setup Recording
            controllerSettings.AddRecorderSettings(videoRecorder);
            controllerSettings.AddRecorderSettings(animationRecorder);
            controllerSettings.AddRecorderSettings(imageRecorder);

            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 60.0f;

            RecorderOptions.VerboseMode = false;
            m_RecorderController.PrepareRecording();
            m_RecorderController.StartRecording();
        }

        void OnDisable()
        {
            m_RecorderController.StopRecording();
        }
    }
}

#endif
