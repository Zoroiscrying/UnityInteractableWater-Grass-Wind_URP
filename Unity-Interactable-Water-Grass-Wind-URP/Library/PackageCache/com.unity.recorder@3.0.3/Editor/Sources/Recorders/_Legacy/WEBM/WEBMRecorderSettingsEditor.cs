using UnityEngine;

namespace UnityEditor.Recorder.FrameCapturer
{
    [CustomEditor(typeof(WEBMRecorderSettings))]
    class WEBMRecorderSettingsEditor : RecorderEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The selected legacy WEBM recorder has been deprecated.");
        }
    }
}
