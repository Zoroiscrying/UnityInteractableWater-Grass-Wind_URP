using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace URPShaderTestBed.Grass
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class GeometryGrassPainter : MonoBehaviour
    {

        // Brush SO Configurations
        [HideInInspector]
        public List<GrassPaintBrushSetting> BrushSettings = new List<GrassPaintBrushSetting>();

        public int brushSettingInt = 0;
    
        private Mesh mesh;
        MeshFilter filter;

        public Color AdjustedColor;

        [Range(1, 600000)]
        public int grassLimit = 50000;

        private Vector3 lastPosition = Vector3.zero;

        public int toolbarInt = 0;

        [SerializeField]
        private List<Vector3> positions = new List<Vector3>();
        [SerializeField]
        private List<Color> colors = new List<Color>();
        [SerializeField]
        private List<int> indices = new List<int>();
        [SerializeField]
        private List<Vector3> normals = new List<Vector3>();
        [SerializeField]
        private List<Vector2> length = new List<Vector2>();

        public bool painting;
        public bool removing;
        public bool editing;

        [FormerlySerializedAs("i")] public int indexCount = 0;

        public float sizeWidth = 1f;
        public float sizeLength = 1f;
        public float density = 1f;


        public float normalLimit = 1;

        public float rangeR, rangeG, rangeB;
        public LayerMask hitMask = 1;
        public LayerMask paintMask = 1;
    
        public float brushSize = 1.0f;

        public Vector3 mousePos;

        [HideInInspector]
        public Vector3 hitPosGizmo;

        public Vector3 hitPos;

        [HideInInspector]
        public Vector3 hitNormal;

        private bool _meshObjectChanged = false;
    
        [SerializeField]
        public GrassMeshPointData grassMeshPointData;

        [FormerlySerializedAs("normalUpFactor")] [SerializeField]
        private float _normalUpFactor = 0f;
        
        private List<int> _localIndices = new List<int>();
        [SerializeField] private bool _brushChangeWidth = true;
        [SerializeField] private bool _brushChangeHeight = true;
        
        [SerializeField] private bool _smoothLength = true;
        [SerializeField] private bool _smoothColor = true;

        [SerializeField] private bool _adjustLength = false;
        [SerializeField] private float _deltaLength = 0.5f;
        [SerializeField] private float _deltaWidth = 0.5f;

        [SerializeField] private float _deltaColorFactor = 30.0f;
        [SerializeField] private bool _multiplyColor = false;
        [SerializeField] private bool _addColor = false;
        [SerializeField] private Color _multiplierColor = Color.white;
        [SerializeField] private Color _addingColor = Color.black;

        int[] indi;
    
#if UNITY_EDITOR
    
    
        //void OnFocus()
        //{
        //    // Remove delegate listener if it has previously
        //    // been assigned.
        //    SceneView.duringSceneGui -= this.OnScene;
        //    // Add (or re-add) the delegate.
        //    SceneView.duringSceneGui += this.OnScene;
        //}
//
        //void OnDestroy()
        //{
        //    // When the window is destroyed, remove the delegate
        //    // so that it will no longer do any drawing.
        //    SceneView.duringSceneGui -= this.OnScene;
        //}
//
        private void OnEnable()
        {
            filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
            }
            else
            {
                mesh = new Mesh();
                filter.sharedMesh = mesh;
            }
        }

        private void OnDestroy()
        {
            //Debug.Log("On Destroy");
        }

        public void SetAllVertexColorToColor( Color color )
        {
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i] = color;
            }
        }
        
        public void ClearMesh()
        {
            indexCount = 0;
            positions = new List<Vector3>();
            indices = new List<int>();
            colors = new List<Color>();
            normals = new List<Vector3>();
            length = new List<Vector2>();
        
            if (grassMeshPointData != null)
            {
                Undo.RegisterCompleteObjectUndo(grassMeshPointData, "Clear Grass Point Mesh");
                grassMeshPointData.ClearData();
            }
        }

        public void OnScene(SceneView scene)
        {
            //if (_grassMeshPointData == null)
            //{
            //    _grassMeshPointData = new GrassMeshPointData();
            //    return;
            //}
        
            _meshObjectChanged = false;
            // only allow painting while this object is selected
            if ((Selection.Contains(gameObject)))
            {
                if (BrushSettings.Count > 0)
                {
                    if (brushSettingInt >= BrushSettings.Count)
                    {
                        sizeWidth = BrushSettings[brushSettingInt-1].GrassWidth;
                        sizeLength = BrushSettings[brushSettingInt-1].GrassHeight;   
                    }
                    else
                    {
                        sizeWidth = BrushSettings[brushSettingInt].GrassWidth;
                        sizeLength = BrushSettings[brushSettingInt].GrassHeight;  
                    }
                    //Debug.Log(BrushSettings.Count + " " + brushSettingInt);
                }
                else
                {
                    sizeWidth = 1.0f;
                    sizeLength = 1.0f;
                }

                Event e = Event.current;
                RaycastHit terrainHit;
                mousePos = e.mousePosition;
                float ppp = EditorGUIUtility.pixelsPerPoint;
                mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
                mousePos.x *= ppp;

                // ray for gizmo(disc)
                Ray rayGizmo = scene.camera.ScreenPointToRay(mousePos);
                RaycastHit hitGizmo;

                if (Physics.Raycast(rayGizmo, out hitGizmo, 200f, hitMask.value))
                {
                    hitPosGizmo = hitGizmo.point;
                }

                if (e.type == EventType.MouseDrag && e.button == 0 && toolbarInt == 0)
                {
                    // place based on density
                    for (int k = 0; k < density; k++)
                    {
                        // brush range
                        float t = 2f * Mathf.PI * Random.Range(0f, brushSize);
                        float u = Random.Range(0f, brushSize) + Random.Range(0f, brushSize);
                        float r = (u > 1 ? 2 - u : u);
                        Vector3 origin = Vector3.zero;

                        // place random in radius, except for first one
                        if (k != 0)
                        {
                            origin.x += r * Mathf.Cos(t);
                            origin.y += r * Mathf.Sin(t);
                        }
                        else
                        {
                            origin = Vector3.zero;
                        }

                        // add random range to ray
                        Ray ray = scene.camera.ScreenPointToRay(mousePos);
                        ray.origin += origin;

                        // if the ray hits something thats on the layer mask,  within the grass limit and within the y normal limit
                        if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value) && indexCount < grassLimit && terrainHit.normal.y <= (1 + normalLimit) && terrainHit.normal.y >= (1 - normalLimit))
                        {
                            if ((paintMask.value & (1 << terrainHit.transform.gameObject.layer)) > 0)
                            {
                                hitPos = terrainHit.point;
                                hitNormal = terrainHit.normal;
                                var calculatedNormal = Vector3.Lerp(hitNormal, Vector3.up, _normalUpFactor);
                                if (k != 0)
                                {
                                    _meshObjectChanged = true;
                                    var grassPosition = hitPos;// + Vector3.Cross(origin, hitNormal);
                                    grassPosition -= this.transform.position;

                                    positions.Add((grassPosition));
                                    indices.Add(indexCount);
                                    length.Add(new Vector2(sizeWidth, sizeLength));
                                    // add random color variations                          
                                    colors.Add(new Color(AdjustedColor.r + (Random.Range(0, 1.0f) * rangeR), AdjustedColor.g + (Random.Range(0, 1.0f) * rangeG), AdjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));

                                    //colors.Add(temp);
                                    normals.Add(calculatedNormal);
                                    indexCount++;
                                }
                                else
                                {// to not place everything at once, check if the first placed point far enough away from the last placed first one
                                    if (Vector3.Distance(terrainHit.point, lastPosition) > brushSize)
                                    {
                                        _meshObjectChanged = true;
                                        var grassPosition = hitPos;
                                        grassPosition -= this.transform.position;
                                        positions.Add((grassPosition));
                                        indices.Add(indexCount);
                                        length.Add(new Vector2(sizeWidth, sizeLength));
                                        colors.Add(new Color(AdjustedColor.r + (Random.Range(0, 1.0f) * rangeR), AdjustedColor.g + (Random.Range(0, 1.0f) * rangeG), AdjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));
                                        normals.Add(calculatedNormal);
                                        indexCount++;

                                        if (origin == Vector3.zero)
                                        {
                                            lastPosition = hitPos;
                                        }
                                    }
                                }
                            }

                        }

                    }
                    e.Use();
                }
                // removing mesh points
                if (e.type == EventType.MouseDrag && e.button == 0 && toolbarInt == 1)
                {
                    Ray ray = scene.camera.ScreenPointToRay(mousePos);

                    if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
                    {
                        hitPos = terrainHit.point;
                        hitPosGizmo = hitPos;
                        hitNormal = terrainHit.normal;
                        for (int j = 0; j < positions.Count; j++)
                        {
                            Vector3 pos = positions[j];

                            pos += this.transform.position;
                            float dist = Vector3.Distance(terrainHit.point, pos);

                            // if its within the radius of the brush, remove all info
                            if (dist <= brushSize)
                            {
                                _meshObjectChanged = true;
                                positions.RemoveAt(j);
                                colors.RemoveAt(j);
                                normals.RemoveAt(j);
                                length.RemoveAt(j);
                                indices.RemoveAt(j);
                                indexCount--;
                                for (int i = 0; i < indices.Count; i++)
                                {
                                    indices[i] = i;
                                }
                            }
                        }
                    }
                    e.Use();
                }
                
                //editing mesh points
                if (e.type == EventType.MouseDrag && e.button == 0 && toolbarInt == 2)
                {
                    Ray ray = scene.camera.ScreenPointToRay(mousePos);

                    if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
                    {
                        _meshObjectChanged = true;
                        hitPos = terrainHit.point;
                        hitPosGizmo = hitPos;
                        hitNormal = terrainHit.normal;
                        for (int j = 0; j < positions.Count; j++)
                        {
                            Vector3 pos = positions[j];

                            pos += this.transform.position;
                            float dist = Vector3.Distance(terrainHit.point, pos);

                            // if its within the radius of the brush, remove all info
                            if (dist <= brushSize)
                            {
                                if (_adjustLength)
                                {
                                    length[j] += new Vector2(_deltaWidth, _deltaLength) * Time.deltaTime;
                                }
                                else
                                {
                                    if (_brushChangeWidth)
                                    {
                                        length[j] = new Vector2(sizeWidth, length[j].y);
                                    }

                                    if (_brushChangeHeight)
                                    {
                                        length[j] = new Vector2(length[j].x, sizeLength);
                                    }
                                    //length[j] = new Vector2(sizeWidth, sizeLength);
                                }

                                if (_multiplyColor)
                                {
                                    var targetColor = colors[j] * _multiplierColor;
                                    colors[j] = Color.Lerp(colors[j], targetColor, _deltaColorFactor * Time.deltaTime);
                                }

                                if (_addColor)
                                {
                                    colors[j] += _addingColor * _deltaColorFactor * Time.deltaTime;
                                }

                                if (!_multiplyColor && !_addColor)
                                {
                                    colors[j] = (new Color(
                                        AdjustedColor.r + (Random.Range(0, 1.0f) * rangeR),
                                        AdjustedColor.g + (Random.Range(0, 1.0f) * rangeG),
                                        AdjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 
                                        1));
                                }
                            }
                        }
                    }
                    e.Use();
                }
                // Smoothing mesh points
                if (e.type == EventType.MouseDrag && e.button == 0 && toolbarInt == 3)
                {
                    Ray ray = scene.camera.ScreenPointToRay(mousePos);

                    if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
                    {
                        _meshObjectChanged = true;
                        hitPos = terrainHit.point;
                        hitPosGizmo = hitPos;
                        hitNormal = terrainHit.normal;
                        for (int j = 0; j < positions.Count; j++)
                        {
                            Vector3 pos = positions[j];

                            pos += this.transform.position;
                            float dist = Vector3.Distance(terrainHit.point, pos);

                            // if its within the radius of the brush, remove all info
                            if (dist <= brushSize)
                            {
                                _localIndices.Add(j);
                            }
                        }

                        float averageWidth = 0;
                        float averageHeight = 0;
                        Color averageColor = Color.black;

                        foreach (var index in _localIndices)
                        {
                            averageWidth += length[index].x / _localIndices.Count;
                            averageHeight += length[index].y / _localIndices.Count;
                            averageColor += colors[index] / _localIndices.Count;
                        }

                        foreach (var index in _localIndices)
                        {
                            var deltaTime = 0.75f * Time.deltaTime;
                            // Length Smoothing
                            if (_smoothLength)
                            {
                                length[index] = new Vector2(
                                    Mathf.Lerp(length[index].x, averageWidth, deltaTime),
                                    Mathf.Lerp(length[index].y, averageHeight, deltaTime));   
                            }
                            // Color Smoothing
                            if (_smoothColor)
                            {
                                colors[index] = Color.Lerp(colors[index], averageColor, deltaTime);   
                            }
                        }
                        
                        _localIndices.Clear();
                    }
                    e.Use();
                }

                if (_meshObjectChanged && grassMeshPointData != null)
                {
                    Undo.RegisterCompleteObjectUndo(grassMeshPointData, "Mesh Modification");   
                }
                // set all info to mesh
                mesh.Clear();

                grassMeshPointData.indexCount = indexCount;
            
                grassMeshPointData.positions = positions.ToArray();
                mesh.SetVertices(positions);
            
                indi = indices.ToArray();
                grassMeshPointData.indices = indi;
                mesh.SetIndices(indi, MeshTopology.Points, 0);

                grassMeshPointData.length = length.ToArray();
                mesh.SetUVs(0, length);
                grassMeshPointData.colors = colors.ToArray();
                mesh.SetColors(colors);
                grassMeshPointData.normals = normals.ToArray();
                mesh.SetNormals(normals);

                filter.sharedMesh = mesh;
            }
        }

        public void RedoUndoMeshCheck()
        {
            if (filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
            }
            else
            {
                mesh = new Mesh();
                filter.sharedMesh = mesh;
            }
        
            mesh.Clear();
        
            mesh.SetVertices(grassMeshPointData.positions);
            mesh.SetIndices(grassMeshPointData.indices, MeshTopology.Points, 0);
            mesh.SetUVs(0, grassMeshPointData.length);
            mesh.SetColors(grassMeshPointData.colors);
            mesh.SetNormals(grassMeshPointData.normals);
        
            filter.sharedMesh = mesh;
        
            //Debug.Log("Redo GrassMeshIndices Count:" + grassMeshPointData.indices.Length);
            indexCount = grassMeshPointData.indexCount;
            positions = grassMeshPointData.positions.ToList();
            indices = grassMeshPointData.indices.ToList();
            colors = grassMeshPointData.colors.ToList();
            normals = grassMeshPointData.normals.ToList();
            length = grassMeshPointData.length.ToList();
            //Debug.Log("Redo GrassMeshIndices Count:" + indices.Count);
        }
    
#endif
    }
}
