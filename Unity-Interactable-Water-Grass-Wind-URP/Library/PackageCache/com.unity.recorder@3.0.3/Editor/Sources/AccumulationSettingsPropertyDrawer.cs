using UnityEngine;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(AccumulationSettings))]
    class AccumulationSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            // Accumulation motion blur
            internal static readonly GUIContent CaptureAccumulationLabel = new GUIContent("Accumulation", "Capture multiple sub-frames and accumulate them on each final recorded frame to get a motion blur effect or Path Tracing convergence.\nNote: this option might considerably slow down your recording process as it involves a higher amount of rendering steps.");
            internal static readonly GUIContent ShutterInterval = new GUIContent("Shutter Interval", "The portion of the interval between two subsequent frames in which the shutter opens and closes according to the specified Shutter Profile.");
            internal static readonly GUIContent ShutterProfile = new GUIContent("Shutter Profile", "Defines a response profile to simulate the physical motion of a camera shutter at each frame when capturing sub-frames. Either specify a Range to set up a trapezoid-based profile or select an animation Curve.");
            internal static readonly GUIContent AccumulationSamples = new GUIContent("Samples", "The number of sub-frames to capture and accumulate on each final recorded frame.");
            internal static readonly GUIContent ShutterProfileType = new GUIContent("");
        }

        private SerializedProperty m_CaptureAccumulation;
        private SerializedProperty m_Samples;
        private SerializedProperty m_ShutterInterval;
        private SerializedProperty m_ShutterType;
        private SerializedProperty m_ShutterProfileCurve;
        private SerializedProperty m_ShutterFullyOpen;
        private SerializedProperty m_ShutterBeginsClosing;
        private const int k_MaxSamples = 8192;
        void Initialize(SerializedProperty property)
        {
            m_CaptureAccumulation = property.FindPropertyRelative("captureAccumulation");
            m_Samples = property.FindPropertyRelative("samples");
            m_ShutterInterval = property.FindPropertyRelative("shutterInterval");
            m_ShutterType = property.FindPropertyRelative("shutterProfileType");
            m_ShutterProfileCurve = property.FindPropertyRelative("shutterProfileCurve");
            m_ShutterFullyOpen = property.FindPropertyRelative("shutterFullyOpen");
            m_ShutterBeginsClosing = property.FindPropertyRelative("shutterBeginsClosing");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            var captureAccumulation = m_CaptureAccumulation.boolValue;
            var samples = m_Samples.intValue;
            var shutterInterval = m_ShutterInterval.floatValue;

            EditorGUI.BeginProperty(position, label, property);
            captureAccumulation = EditorGUILayout.Toggle(Styles.CaptureAccumulationLabel, captureAccumulation);
            m_CaptureAccumulation.boolValue = captureAccumulation;
            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(!captureAccumulation))
            {
                m_Samples.intValue = (int)EditorGUILayout.Slider(
                    Styles.AccumulationSamples,
                    samples, 1, k_MaxSamples);
                m_ShutterInterval.floatValue =
                    EditorGUILayout.Slider(Styles.ShutterInterval, shutterInterval, 0.0f,
                        1.0f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel(Styles.ShutterProfile);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.PropertyField(m_ShutterType, Styles.ShutterProfileType);

                    if (m_ShutterType.intValue == 1) // curve value
                    {
                        m_ShutterProfileCurve.animationCurveValue = EditorGUILayout.CurveField(
                            m_ShutterProfileCurve.animationCurveValue, Color.red, new Rect(0.0f, 0.0f, 1.0f, 1.0f));
                    }
                    else
                    {
                        float fullyOpen = m_ShutterFullyOpen.floatValue;
                        float beginsClosing = m_ShutterBeginsClosing.floatValue;

                        EditorGUI.BeginChangeCheck();
                        fullyOpen = EditorGUILayout.FloatField(fullyOpen);
                        fullyOpen = Mathf.Clamp(
                            fullyOpen, 0.0f,
                            beginsClosing);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_ShutterFullyOpen.floatValue = fullyOpen;
                        }

                        float minValue = fullyOpen;
                        float maxValue = beginsClosing;

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, 0.0f, 1.0f, null);
                        minValue = Mathf.Round(minValue * 100) / 100.0f;
                        maxValue = Mathf.Round(maxValue * 100) / 100.0f;

                        fullyOpen = minValue;
                        beginsClosing = maxValue;
                        beginsClosing =  EditorGUILayout.FloatField(beginsClosing);
                        beginsClosing =  Mathf.Clamp(beginsClosing,
                            fullyOpen, 1.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_ShutterFullyOpen.floatValue = fullyOpen;
                            m_ShutterBeginsClosing.floatValue = beginsClosing;
                        }
                    }
                    EditorGUI.indentLevel++;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
    }
}
