using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(ImageRecorderSettings))]
    class ImageRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
        SerializedProperty m_CaptureAlpha;
        SerializedProperty m_ColorSpace;
        SerializedProperty m_EXRCompression;

        static class Styles
        {
            internal static readonly GUIContent FormatLabel = new GUIContent("Media File Format", "The file encoding format of the recorded output.");
            internal static readonly GUIContent CaptureAlphaLabel = new GUIContent("Include Alpha", "To include the alpha channel in the recording.\n\nIn the High Definition Render Pipeline (HDRP), you need to set the buffer format to R16G16B16A16.");
            internal static readonly GUIContent CLabel = new GUIContent("Compression", "The data compression method to apply when using the EXR format.");
            internal static readonly GUIContent ColorSpace = new GUIContent("Color Space", "The color space (gamma curve, gamut) to use in the output images.\n\nIf you select an option to get unclamped values, you must:\n- Use High Definition Render Pipeline (HDRP).\n- Disable any Tonemapping in your Scene.\n- Disable Dithering on the selected Camera.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            m_OutputFormat = serializedObject.FindProperty("outputFormat");
            m_CaptureAlpha = serializedObject.FindProperty("captureAlpha");
            m_EXRCompression = serializedObject.FindProperty("m_EXRCompression");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");
        }

        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.PropertyField(m_OutputFormat, Styles.FormatLabel);
            var imageSettings = (ImageRecorderSettings)target;
            if (!UnityHelpers.UsingURP())
            {
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureAlpha()))
                {
                    EditorGUILayout.PropertyField(m_CaptureAlpha, Styles.CaptureAlphaLabel);
                }
            }

            string[] list_of_colorspaces = new[] {"sRGB, sRGB", "Linear, sRGB (unclamped)"};

            if (imageSettings.CanCaptureHDRFrames())
            {
                m_ColorSpace.intValue =
                    EditorGUILayout.Popup(Styles.ColorSpace, m_ColorSpace.intValue, list_of_colorspaces);
            }
            else
            {
                // Disable the dropdown but show sRGB
                using (new EditorGUI.DisabledScope(!imageSettings.CanCaptureHDRFrames()))
                    EditorGUILayout.Popup(Styles.ColorSpace, 0, list_of_colorspaces);
            }

            if ((ImageRecorderSettings.ImageRecorderOutputFormat)m_OutputFormat.enumValueIndex ==
                ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
            {
                EditorGUILayout.PropertyField(m_EXRCompression, Styles.CLabel);
            }
        }
    }
}
