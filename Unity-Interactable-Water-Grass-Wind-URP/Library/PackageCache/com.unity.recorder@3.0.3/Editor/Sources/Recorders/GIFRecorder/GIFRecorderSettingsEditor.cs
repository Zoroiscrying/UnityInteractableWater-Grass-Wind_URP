using UnityEngine;

namespace UnityEditor.Recorder
{
#pragma warning disable 618
    [CustomEditor(typeof(GIFRecorderSettings))]
#pragma warning restore 618
    class GIFRecorderSettingsEditor : RecorderEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("The selected GIF Animation recorder has been deprecated.");
        }
    }
}
