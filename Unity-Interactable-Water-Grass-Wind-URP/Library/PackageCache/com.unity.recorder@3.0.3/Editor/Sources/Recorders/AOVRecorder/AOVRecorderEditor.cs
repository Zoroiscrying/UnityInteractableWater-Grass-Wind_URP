using System;
using System.Linq;
using UnityEditor.Recorder.AOV;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(AOVRecorderSettings))]
    class AOVRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_AOVSelection;
        SerializedProperty m_EXRCompression;
        SerializedProperty m_ColorSpace;

        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new GUIContent("Format");
            internal static readonly GUIContent AOVLabel = new GUIContent("Aov to Export", "The AOV render pass to record.");
            internal static readonly GUIContent AOVCLabel = new GUIContent("Compression", "The data compression method to apply when using the EXR format.");
            internal static readonly GUIContent ColorSpace = new GUIContent("Color Space", "The color space (gamma curve, gamut) to use in the output images.\n\nIf you select an option to get unclamped values, you must:\n- Use High Definition Render Pipeline (HDRP).\n- Disable any Tonemapping in your Scene.\n- Disable Dithering on the selected Camera.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_OutputFormat = serializedObject.FindProperty("m_OutputFormat");
            m_AOVSelection = serializedObject.FindProperty("m_AOVSelection");
            m_EXRCompression = serializedObject.FindProperty("m_EXRCompression");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");
        }

        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.PropertyField(m_OutputFormat, Styles.FormatLabel);
            var imageSettings = (AOVRecorderSettings)target;
            string[] list_of_colorspaces = new[] {"sRGB, sRGB", "Linear, sRGB (unclamped)"};

            if (imageSettings.CanCaptureHDRFrames())
            {
                m_ColorSpace.intValue =
                    EditorGUILayout.Popup(Styles.ColorSpace, m_ColorSpace.intValue, list_of_colorspaces);
            }
            else
            {
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureHDRFrames()))
                {
                    EditorGUILayout.Popup(Styles.ColorSpace, 0, list_of_colorspaces);
                }
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                EditorGUILayout.PropertyField(m_EXRCompression, Styles.AOVCLabel);
            }
        }

        protected override void AOVGUI()
        {
#if HDRP_AVAILABLE
            base.AOVGUI();

            // This is the list of items in the GUI, but it does not necessarily include all elements of the AOVType enum.
            var aovTypesSubset = AOVCameraAOVRequestAPIInput.m_Aovs.Keys.Select(i => i.ToString()).ToArray();
            AOVType selectedAOV = (AOVType)m_AOVSelection.intValue; // we load enum values not GUI dropdown values

            // What is the index of selectedAOV in aovTypesSubset?
            var selectedAOVIndexInSubset = AOVCameraAOVRequestAPIInput.m_Aovs.Keys.ToList().FindIndex(k => k == selectedAOV);
            if (selectedAOVIndexInSubset == -1)
            {
                // The selected AOV is not supported in the current config, most likely because the
                // HDRP_LIGHTDECO_API support has been removed from the project due to HDRP downgrading.
                var colorToRestore = EditorStyles.label.normal.textColor;
                EditorStyles.label.normal.textColor = Color.red;
                EditorGUILayout.LabelField(Styles.AOVLabel.text, $"You need HDRP 8.9.9 or above to record '{selectedAOV}'.");
                EditorStyles.label.normal.textColor = colorToRestore;
            }
            else
            {
                selectedAOVIndexInSubset = EditorGUILayout.Popup(Styles.AOVLabel, selectedAOVIndexInSubset, aovTypesSubset);
                var newSelectedAOV = AOVCameraAOVRequestAPIInput.m_Aovs.Keys.ElementAt(selectedAOVIndexInSubset);
                m_AOVSelection.intValue = (int)newSelectedAOV;
            }

            DrawSeparator();
#else
            // Draw nothing, user will see errors (see AOVRecorderSettings.HasErrors) and an empty AOV GUI
#endif
        }
    }
}
