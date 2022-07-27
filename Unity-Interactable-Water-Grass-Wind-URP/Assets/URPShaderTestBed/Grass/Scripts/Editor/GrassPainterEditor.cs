using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace URPShaderTestBed.Grass
{
    [CustomEditor(typeof(GeometryGrassPainter))]
    public class GrassPainterEditor : UnityEditor.Editor
    {
        private static string _grassPainterType = "GrassPaintBrushSetting";

        GeometryGrassPainter grassPainter;
        readonly string[] toolbarStrings = {"Add", "Remove", "Edit", "Smoothing"};
        private List<string> loadedBrushSettings = new List<string>();

        private GrassPaintBrushSetting _brushSetting = null;

        private bool shouldDisplayBrushDropDownPanel = false;

        private SerializedProperty _grassMeshDataProperty;
        private SerializedProperty _grassSmoothLengthProperty;
        private SerializedProperty _grassSmoothColorProperty;
        private SerializedProperty _grassAdjustLengthProperty;
        private SerializedProperty _grassDeltaLengthProperty;
        private SerializedProperty _grassDeltaWidthProperty;
        
        private SerializedProperty _grassMultiplyColorProperty;
        private SerializedProperty _grassAddColorProperty;
        private SerializedProperty _grassMultiplierColorProperty;
        private SerializedProperty _grassAddingColorProperty;

        private SerializedProperty _grassBrushChangeWidth;
        private SerializedProperty _grassBrushChangeHeight;

        private SerializedProperty _grassNormalUpFactorProperty;
        

        private AnimBool m_ShowGrassLimitFields;
        private AnimBool m_ShowPaintStatusFields;
        private AnimBool m_ShowGrassBrushSettingFields;
        private AnimBool m_ShowColorSettingFields;

        private bool DisplayAdding => grassPainter.toolbarInt == 0;
        private bool DisplayRemoving => grassPainter.toolbarInt == 1;
        private bool DisplaySmoothing => grassPainter.toolbarInt == 3;
        private bool DisplayEditing => grassPainter.toolbarInt == 2;
        
        // Disabling the Position Handle
        Tool LastTool = Tool.None;

        private void OnEnable()
        {
            // Disable the position handle
            LastTool = Tools.current;
            Tools.current = Tool.None;
            
            grassPainter = (GeometryGrassPainter) target;
            _grassMeshDataProperty = serializedObject.FindProperty("grassMeshPointData");
            _grassSmoothLengthProperty = serializedObject.FindProperty("_smoothLength");
            _grassSmoothColorProperty = serializedObject.FindProperty("_smoothColor");
            _grassAdjustLengthProperty = serializedObject.FindProperty("_adjustLength");
            
            _grassMultiplyColorProperty = serializedObject.FindProperty("_multiplyColor");
            _grassMultiplierColorProperty = serializedObject.FindProperty("_multiplierColor");
            _grassAddColorProperty = serializedObject.FindProperty("_addColor");
            _grassAddingColorProperty = serializedObject.FindProperty("_addingColor");

            _grassDeltaLengthProperty = serializedObject.FindProperty("_deltaLength");
            _grassDeltaWidthProperty = serializedObject.FindProperty("_deltaWidth");

            _grassBrushChangeWidth = serializedObject.FindProperty("_brushChangeWidth");
            _grassBrushChangeHeight = serializedObject.FindProperty("_brushChangeHeight");

            _grassNormalUpFactorProperty = serializedObject.FindProperty("_normalUpFactor");
            
            SceneView.duringSceneGui += DuringSceneGUI;
            Undo.undoRedoPerformed += grassPainter.RedoUndoMeshCheck;

            //Setup anim bools
            m_ShowGrassLimitFields = new AnimBool(true);
            m_ShowGrassLimitFields.valueChanged.AddListener(new UnityAction(base.Repaint));

            m_ShowPaintStatusFields = new AnimBool(true);
            m_ShowPaintStatusFields.valueChanged.AddListener(new UnityAction(base.Repaint));

            m_ShowGrassBrushSettingFields = new AnimBool(true);
            m_ShowGrassBrushSettingFields.valueChanged.AddListener(new UnityAction(base.Repaint));

            m_ShowColorSettingFields = new AnimBool(true);
            m_ShowColorSettingFields.valueChanged.AddListener(new UnityAction(base.Repaint));

            EnsurePrivateBrushSetting();

            UpdateBrushSettings();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= grassPainter.RedoUndoMeshCheck;
            SceneView.duringSceneGui -= DuringSceneGUI;
            Tools.current = LastTool;
        }

        void DuringSceneGUI(SceneView sceneView)
        {
            if (grassPainter != null && grassPainter.grassMeshPointData != null)
            {
                if (Selection.activeGameObject == grassPainter.gameObject && grassPainter.gameObject.activeSelf)
                {
                    //Draw the grass-spawning sphere.
                    if (grassPainter.toolbarInt == 0)
                    {
                        Handles.color = Color.cyan;
                        Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                        Handles.color = new Color(0, 0.5f, 0.5f, 0.25f);
                        Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);   
                    }

                    if (grassPainter.toolbarInt == 1)
                    {
                        Handles.color = Color.red;
                        Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                        Handles.color = new Color(0.5f, 0f, 0f, 0.25f);
                        Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                    }

                    if (grassPainter.toolbarInt == 2)
                    {
                        Handles.color = Color.yellow;
                        Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                        Handles.color = new Color(0.5f, 0.5f, 0f, 0.05f);
                        Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                    }

                    if (grassPainter.toolbarInt == 3)
                    {
                        Handles.color = Color.white;
                        Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                        Handles.color = new Color(1.0f, 1.0f, 1.0f, 0.05f);
                        Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
                    }

                    //Disable the left-click for selection.
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    grassPainter.OnScene(sceneView);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Grass Mesh Point Data Not Initialized.
            if (grassPainter.grassMeshPointData == null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.HelpBox("The Grass Mesh Data is not initialized, please initialize the data!",
                        MessageType.Error);
                    if (GUILayout.Button("Create Grass Mesh Data", EditorStyles.toolbarButton))
                    {
                        string path = "Assets/GrassMeshData.asset";
                        if (path.Length > 0)
                        {
                            GrassMeshPointData asset = ScriptableObject.CreateInstance<GrassMeshPointData>();
                            path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path);
                            Debug.Log(path);

                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();

                            EditorUtility.FocusProjectWindow();
                            AssetDatabase.Refresh();
                            grassPainter.grassMeshPointData = asset;
                        }
                    }

                    EditorGUILayout.LabelField("Or Assign Grass Mesh Data", GUILayout.ExpandWidth(true));
                    _grassMeshDataProperty.objectReferenceValue = EditorGUILayout.ObjectField(
                        new GUIContent("Source", "Add Grass Mesh Data"), _grassMeshDataProperty.objectReferenceValue,
                        typeof(GrassMeshPointData), false);
                    grassPainter.grassMeshPointData = _grassMeshDataProperty.objectReferenceValue as GrassMeshPointData;
                }
            }
            // Mesh Point Data Initialized, Normally display other editor contents. 
            else
            {
                // Draw Grass Limit Fields
                m_ShowGrassLimitFields.target =
                    CoreEditorUtils.DrawHeaderFoldout("Grass Limit Setting", m_ShowGrassLimitFields.target);
                using (var group = new EditorGUILayout.FadeGroupScope(m_ShowGrassLimitFields.faded))
                {
                    if (group.visible)
                    {
                        EditorGUILayout.Space();
                        
                        EditorGUILayout.LabelField("Grass Limit", EditorStyles.boldLabel);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(grassPainter.indexCount.ToString(), EditorStyles.label,
                            GUILayout.MaxWidth(60));
                        EditorGUILayout.LabelField("/", EditorStyles.label, GUILayout.MaxWidth(10));
                        grassPainter.grassLimit = EditorGUILayout.IntField(grassPainter.grassLimit);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();
                    }
                }

                CoreEditorUtils.DrawSplitter();

                // Draw Paint Mode Setting
                m_ShowPaintStatusFields.target =
                    CoreEditorUtils.DrawHeaderFoldout("Paint Mode", m_ShowPaintStatusFields.target);
                using (var group = new EditorGUILayout.FadeGroupScope(m_ShowPaintStatusFields.faded))
                {
                    if (group.visible)
                    {
                        EditorGUILayout.Space();
                        
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField("Paint Status (Left-Mouse Button to paint)",
                                EditorStyles.boldLabel);
                            grassPainter.toolbarInt = GUILayout.Toolbar(grassPainter.toolbarInt, toolbarStrings);
                            EditorGUILayout.Space();
                        }
                        
                        EditorGUILayout.Space();
                    }
                }
                
                CoreEditorUtils.DrawSplitter();

                // Draw Grass Brush Setting
                m_ShowGrassBrushSettingFields.target =
                    CoreEditorUtils.DrawHeaderFoldout("Grass Brush Settings", m_ShowGrassBrushSettingFields.target);
                using (var group = new EditorGUILayout.FadeGroupScope(m_ShowGrassBrushSettingFields.faded))
                {
                    if (group.visible)
                    {
                        EditorGUILayout.Space();

                        if (DisplaySmoothing)
                        {
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                EditorGUILayout.LabelField("Smooth Setting", EditorStyles.boldLabel);
                                serializedObject.Update();
                                EditorGUILayout.PropertyField(_grassSmoothLengthProperty, new GUIContent("Smooth Width & Height"));
                                EditorGUILayout.PropertyField(_grassSmoothColorProperty, new GUIContent("Smooth Color"));
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                        else
                        {
                            // Draw Grass Brush Settings
                            if (!DisplayRemoving)
                            {
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                {
                                    EditorGUILayout.LabelField("Grass Setting (Width and Height)", EditorStyles.boldLabel);
                                    if (shouldDisplayBrushDropDownPanel)
                                    {
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            grassPainter.brushSettingInt = EditorGUILayout.Popup("", grassPainter.brushSettingInt,
                                                loadedBrushSettings.ToArray());
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                Repaint();
                                            }

                                            if (GUILayout.Button("Save"))
                                            {
                                                if (_brushSetting != null && grassPainter.brushSettingInt < grassPainter.BrushSettings.Count)
                                                {
                                                    // integer 0, 1 or 2 corresponding to ok, cancel and alt buttons
                                                    int res = EditorUtility.DisplayDialogComplex("Save Brush Settings",
                                                        "Overwrite brush preset, or Create a New brush preset? ", "Overwrite", "Create New",
                                                        "Cancel");
                                                    // Create New
                                                    if (res == 1)
                                                    {
                                                        var newBrushSetting =
                                                            GrassPainterEditor.AddNew(
                                                                grassPainter.BrushSettings[grassPainter.brushSettingInt]);
                                                        SetBrushSettings(newBrushSetting);
                                                        EditorGUIUtility.PingObject(newBrushSetting);
                                                    }
                                                    GUIUtility.ExitGUI();
                                                }
                                                else if (grassPainter.brushSettingInt >= grassPainter.BrushSettings.Count)
                                                {
                                                    var newBrushSetting = GrassPainterEditor.AddNew(_brushSetting);
                                                    SetBrushSettings(newBrushSetting);
                                                    EditorGUIUtility.PingObject(newBrushSetting);
                                                }
                                                else
                                                {
                                                    Debug.LogWarning("Something went wrong saving brush settings.");
                                                }
                                            }
                                        }
                                    
                                        using (new EditorGUILayout.VerticalScope())
                                        {
                                            serializedObject.Update();
                                            if (grassPainter.brushSettingInt >= grassPainter.BrushSettings.Count)
                                            {
                                                EditorGUILayout.BeginHorizontal();
                                                grassPainter.BrushSettings[grassPainter.brushSettingInt-1].GrassWidth =
                                                    EditorGUILayout.FloatField("Width",
                                                        grassPainter.BrushSettings[grassPainter.brushSettingInt-1].GrassWidth);
                                                EditorGUILayout.PropertyField(_grassBrushChangeWidth);
                                                EditorGUILayout.EndHorizontal();

                                                EditorGUILayout.BeginHorizontal();
                                                grassPainter.BrushSettings[grassPainter.brushSettingInt-1].GrassHeight =
                                                    EditorGUILayout.FloatField("Length",
                                                        grassPainter.BrushSettings[grassPainter.brushSettingInt-1].GrassHeight);
                                                EditorGUILayout.PropertyField(_grassBrushChangeHeight);
                                                EditorGUILayout.EndHorizontal();
                                            }
                                            else
                                            {
                                                EditorGUILayout.BeginHorizontal();
                                                grassPainter.BrushSettings[grassPainter.brushSettingInt].GrassWidth =
                                                    EditorGUILayout.FloatField("Width",
                                                        grassPainter.BrushSettings[grassPainter.brushSettingInt].GrassWidth);
                                                EditorGUILayout.PropertyField(_grassBrushChangeWidth);
                                                EditorGUILayout.EndHorizontal();

                                                EditorGUILayout.BeginHorizontal();
                                                grassPainter.BrushSettings[grassPainter.brushSettingInt].GrassHeight =
                                                    EditorGUILayout.FloatField("Length",
                                                        grassPainter.BrushSettings[grassPainter.brushSettingInt].GrassHeight);  
                                                EditorGUILayout.PropertyField(_grassBrushChangeHeight);
                                                EditorGUILayout.EndHorizontal();
                                            }

                                            serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }   
                                EditorGUILayout.Space();
                            }

                            // If Add mode
                            if (DisplayAdding)
                            {
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                {
                                    serializedObject.Update();
                                    EditorGUILayout.LabelField("Grass Initialization Tweak", EditorStyles.boldLabel);
                                    EditorGUILayout.LabelField(" ≡ Grass Normal");
                                    using (new  EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField("Surface", GUILayout.MaxWidth(50));
                                        EditorGUILayout.Slider(_grassNormalUpFactorProperty, 0f, 1f, "");
                                        EditorGUILayout.LabelField("Up", GUILayout.MaxWidth(20));
                                    }
                                    serializedObject.ApplyModifiedProperties();
                                }
                                EditorGUILayout.Space();
                            }

                            // If Edit mode
                            if (DisplayEditing)
                            {
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                {
                                    serializedObject.Update();
                                    EditorGUILayout.LabelField("Adjust Grass Height & Width", EditorStyles.boldLabel);
                                    EditorGUILayout.PropertyField(_grassAdjustLengthProperty,
                                        new GUIContent("Override Brush"));

                                    if (_grassAdjustLengthProperty.boolValue)
                                    {
                                        EditorGUI.indentLevel++;
                                        EditorGUILayout.PropertyField(_grassDeltaLengthProperty,
                                            new GUIContent("Delta Length"));
                                        EditorGUILayout.PropertyField(_grassDeltaWidthProperty,
                                            new GUIContent("Delta Width"));
                                        EditorGUI.indentLevel--;
                                    }

                                    EditorGUILayout.LabelField("Adjust Grass Color", EditorStyles.boldLabel);
                                    
                                    EditorGUI.BeginChangeCheck();
                                    
                                    // Adding Color
                                    EditorGUILayout.PropertyField(_grassMultiplyColorProperty,
                                        new GUIContent("Multiply Color"));
                                    if (_grassMultiplyColorProperty.boolValue)
                                    {
                                        EditorGUI.indentLevel++;
                                        EditorGUILayout.PropertyField(_grassMultiplierColorProperty,
                                            new GUIContent("Multiply Color"));
                                        EditorGUI.indentLevel--;
                                    }
                                    // Multiplying Color
                                    EditorGUILayout.PropertyField(_grassAddColorProperty,
                                        new GUIContent("Add Color"));
                                    if (_grassAddColorProperty.boolValue)
                                    {
                                        EditorGUI.indentLevel++;
                                        EditorGUILayout.PropertyField(_grassAddingColorProperty,
                                            new GUIContent("Adding Color"));
                                        EditorGUI.indentLevel--;
                                    }

                                    serializedObject.ApplyModifiedProperties();
                                }
                                EditorGUILayout.Space();
                            }
                        }

                        // Draw Brush Settings
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField("Brush Setting", EditorStyles.boldLabel);

                            LayerMask tempMask = EditorGUILayout.MaskField("Hit Mask",
                                InternalEditorUtility.LayerMaskToConcatenatedLayersMask(grassPainter.hitMask),
                                InternalEditorUtility.layers);
                            grassPainter.hitMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                            LayerMask tempMask2 = EditorGUILayout.MaskField("Painting Mask",
                                InternalEditorUtility.LayerMaskToConcatenatedLayersMask(grassPainter.paintMask),
                                InternalEditorUtility.layers);
                            grassPainter.paintMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);

                            grassPainter.brushSize = EditorGUILayout.Slider("Brush Size", grassPainter.brushSize, 0.1f, 10f);
                            grassPainter.density = EditorGUILayout.Slider("Density", grassPainter.density, 0.1f, 10f);
                            grassPainter.normalLimit = EditorGUILayout.Slider("Normal Limit", grassPainter.normalLimit, 0f, 1f);
                        }   

                        EditorGUILayout.Space();
                    }
                }

                CoreEditorUtils.DrawSplitter();
                
                m_ShowColorSettingFields.target = 
                    CoreEditorUtils.DrawHeaderFoldout("Grass Color Setting", m_ShowColorSettingFields.target);
                using (var group = new EditorGUILayout.FadeGroupScope(m_ShowColorSettingFields.faded))
                {
                    if (group.visible)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
                        grassPainter.AdjustedColor = EditorGUILayout.ColorField("Brush Color", grassPainter.AdjustedColor);
                        EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
                        grassPainter.rangeR = EditorGUILayout.Slider("Red", grassPainter.rangeR, 0f, 1f);
                        grassPainter.rangeG = EditorGUILayout.Slider("Green", grassPainter.rangeG, 0f, 1f);
                        grassPainter.rangeB = EditorGUILayout.Slider("Blue", grassPainter.rangeB, 0f, 1f);
                        EditorGUILayout.Space();
                    }
                }

                CoreEditorUtils.DrawSplitter();
                
                if (GUILayout.Button("Set All Vertex Color To White"))
                {
                    if (EditorUtility.DisplayDialog("Set All Color To White",
                        "Are you sure you want to set all color to white?", "Yes", "Cancel"))
                    {
                        //grassPainter.ClearMesh();
                        grassPainter.SetAllVertexColorToColor(Color.white);
                    }
                }
                
                if (GUILayout.Button("Clear Mesh"))
                {
                    if (EditorUtility.DisplayDialog("Clear Painted Mesh?",
                        "Are you sure you want to clear the mesh?", "Clear", "Don't Clear"))
                    {
                        grassPainter.ClearMesh();
                    }
                }
            }
        }

        private void UpdateData()
        {
            loadedBrushSettings.Clear();
            shouldDisplayBrushDropDownPanel = true;
            var brushSettings = grassPainter.BrushSettings;
            if (brushSettings.Count > 0)
            {
                foreach (var brushSetting in brushSettings)
                {
                    loadedBrushSettings.Add(brushSetting.name);
                }
            }
            loadedBrushSettings.Add("Add Brush...");
        }

        private void EnsurePrivateBrushSetting()
        {
            _brushSetting = ScriptableObject.CreateInstance<GrassPaintBrushSetting>();
            _brushSetting.SetDefaultValues();
            _brushSetting.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Update Brush Settings From Local Asset Path
        /// </summary>
        private void UpdateBrushSettings()
        {
            //Retrieve All Brush Setting Data From Asset Database.
            var guids = AssetDatabase.FindAssets("t:" + _grassPainterType, new[] {"Assets"});
            List<GrassPaintBrushSetting> grassPaintBrushSettings = new List<GrassPaintBrushSetting>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                grassPaintBrushSettings.Add((GrassPaintBrushSetting)AssetDatabase.LoadAssetAtPath(path, typeof(GrassPaintBrushSetting)));
            }
            grassPaintBrushSettings.Add(_brushSetting);
            grassPainter.BrushSettings = grassPaintBrushSettings;
            
            UpdateData();
        }
        
        /// <summary>
        /// Change brush settings
        /// </summary>
        /// <param name="settings">The new brush settings</param>
        internal void SetBrushSettings(GrassPaintBrushSetting settings)
        {
            UpdateBrushSettings();
            grassPainter.brushSettingInt = grassPainter.BrushSettings.FindIndex(setting => setting.Equals(settings));
            // Implement this later
            //if(brushSettings != null && brushSettings != settings)
            //    DestroyImmediate(brushSettings);
//
            //EditorPrefs.SetString(k_BrushSettingsAssetPref, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings)));
            //EditorPrefs.SetString(k_BrushSettingsName, settings.name);
//
            //brushSettingsAsset = settings;
            //brushSettings = settings.DeepCopy();
            //brushSettings.hideFlags = HideFlags.HideAndDontSave;
        }
        
        /// <summary>
        /// Create a New BrushSettings Asset
        /// </summary>
        /// <returns>the newly created BrushSettings</returns>
        internal static GrassPaintBrushSetting AddNew(GrassPaintBrushSetting prevSettings = null)
        {
            string path = "Assets/Settings/" + "GrassPainterBrushSetting";

            if(string.IsNullOrEmpty(path))
                path = "Assets";

            path = AssetDatabase.GenerateUniqueAssetPath(path + "/New Brush.asset");

            if(!string.IsNullOrEmpty(path))
            {
                GrassPaintBrushSetting settings = ScriptableObject.CreateInstance<GrassPaintBrushSetting>();
                if (prevSettings != null) {
                    string name = settings.name;
                    prevSettings.CopyTo(settings);
                    settings.name = name;	// want to retain the unique name generated by AddNew()
                }
                else
                {
                    settings.SetDefaultValues();
                }

                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(settings);

                return settings;
            }

            return null;
        }
    
    }
}