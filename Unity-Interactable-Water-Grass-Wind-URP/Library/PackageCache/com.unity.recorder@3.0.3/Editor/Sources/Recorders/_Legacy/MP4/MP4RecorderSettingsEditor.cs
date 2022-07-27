using UnityEngine;

namespace UnityEditor.Recorder.FrameCapturer
{
    [CustomEditor(typeof(MP4RecorderSettings))]
    class MP4RecorderSettingsEditor : RecorderEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The selected legacy MP4 recorder has been deprecated.");
        }
    }
}
