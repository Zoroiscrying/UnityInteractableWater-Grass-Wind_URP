using UnityEngine;

namespace UnityEditor.Recorder.FrameCapturer
{
    [CustomEditor(typeof(PNGRecorderSettings))]
    class PNGRecorderSettingsEditor : RecorderEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The selected legacy PNG recorder has been deprecated.");
        }
    }
}
