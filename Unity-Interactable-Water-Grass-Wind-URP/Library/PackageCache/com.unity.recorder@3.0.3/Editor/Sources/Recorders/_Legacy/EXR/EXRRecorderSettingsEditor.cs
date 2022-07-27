using UnityEngine;

namespace UnityEditor.Recorder.FrameCapturer
{
    [CustomEditor(typeof(EXRRecorderSettings))]
    class EXRRecorderSettingsEditor : RecorderEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The selected legacy EXR recorder has been deprecated.");
        }
    }
}
