using UnityEngine.ProBuilder;
using UnityEngine;

namespace UnityEditor.ProBuilder
{
    /// <inheritdoc />
    /// <summary>
    /// Popup window in UV editor with the "Render UV Template" options.
    /// </summary>
    sealed class UVRenderOptions : EditorWindow
    {
        Pref<ImageSize> m_ImageSize = new Pref<ImageSize>("UVRenderOptions.imageSize", ImageSize._1024, SettingsScope.User);
        Pref<Color> m_LineColor = new Pref<Color>("UVRenderOptions.lineColor", Color.green, SettingsScope.User);
        Pref<Color> m_BackgroundColor = new Pref<Color>("UVRenderOptions.backgroundColor", Color.black, SettingsScope.User);
        Pref<bool> m_TransparentBackground = new Pref<bool>("UVRenderOptions.transparentBackground", false, SettingsScope.User);
        Pref<bool> m_HideGrid = new Pref<bool>("UVRenderOptions.hideGrid", true, SettingsScope.User);
        Pref<bool> m_RenderTexture = new Pref<bool>("UVRenderOptions.renderTexture", true, SettingsScope.User);

        enum ImageSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
        };

        public delegate void ScreenshotFunc(int ImageSize, bool HideGrid, Color LineColor, bool TransparentBackground, Color BackgroundColor, bool RenderTexture);
        public ScreenshotFunc screenFunc;
        Vector2 m_Scroll;

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            GUILayout.Label("Render UVs", EditorStyles.boldLabel);
            GUILayout.Space(2);

            m_ImageSize.value = (ImageSize)EditorGUILayout.EnumPopup(new GUIContent("Image Size", "The pixel size of the image to be rendered."), m_ImageSize);
            m_HideGrid.value = EditorGUILayout.Toggle(new GUIContent("Hide Grid", "Hide or show the grid lines."), m_HideGrid);
            m_LineColor.value = EditorGUILayout.ColorField(new GUIContent("Line Color", "The color of the template lines."), m_LineColor);
            m_RenderTexture.value = EditorGUILayout.Toggle(new GUIContent("Include Texture", "If true, a preview image of the first selected face's material will be rendered as part of the UV template.\n\nNote that this depends on the Material's shader having a _mainTexture property."), m_RenderTexture);
            m_TransparentBackground.value = EditorGUILayout.Toggle(new GUIContent("Transparent Background", "If true, only the template lines will be rendered, leaving the background fully transparent."), m_TransparentBackground);

            GUI.enabled = !m_TransparentBackground;
            EditorGUI.indentLevel++;
            m_BackgroundColor.value = EditorGUILayout.ColorField(new GUIContent("Background Color", "If `TransparentBackground` is off, this will be the fill color of the image."), m_BackgroundColor);
            EditorGUI.indentLevel--;
            GUI.enabled = true;

            if (GUILayout.Button("Save UV Template"))
            {
                if (ProBuilderEditor.instance == null || MeshSelection.selectedObjectCount < 1)
                {
                    Debug.LogWarning("Abandoning UV render because no ProBuilder objects are selected.");
                    Close();
                    return;
                }

                screenFunc((int)m_ImageSize.value, m_HideGrid, m_LineColor, m_TransparentBackground, m_BackgroundColor, m_RenderTexture);
                Close();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
