using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(RecorderBindings))]
    internal class RecorderBindingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                base.OnInspectorGUI();
            }
        }
    }
}
