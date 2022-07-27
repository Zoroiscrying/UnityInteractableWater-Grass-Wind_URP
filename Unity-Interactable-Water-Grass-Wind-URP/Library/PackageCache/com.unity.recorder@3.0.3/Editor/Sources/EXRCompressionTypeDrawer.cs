using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(ImageRecorderSettings.EXRCompressionType))]
    class CompressionTypePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var compressionLabels = Enum.GetNames(typeof(ImageRecorderSettings.EXRCompressionType)).ToArray();
            var newValue = EditorGUI.Popup(position, label.text, property.enumValueIndex, compressionLabels);

            property.enumValueIndex = newValue;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
