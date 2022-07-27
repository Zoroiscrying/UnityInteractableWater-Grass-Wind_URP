using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[ExecuteAlways]
public class TerrainBlendingManager : MonoBehaviour
{
    #region Properties and Variables
    
    //[Header("Terrain Depth Baking")]
    //Shader that renders object based on distance to camera
    public Shader UnlitTerrainShader;
    public LayerMask TerrainMask;
    
    private readonly int _terrainDepthTextureID = Shader.PropertyToID("_TerrainDepthTexture");
    private readonly int _terrainColorTextureID = Shader.PropertyToID("_TerrainColorTexture");
    private readonly int _terrainCamScale = Shader.PropertyToID("_TerrainCamScale");
    private readonly int _terrainCamOffsetX = Shader.PropertyToID("_TerrainCamOffsetX");
    private readonly int _terrainCamOffsetZ = Shader.PropertyToID("_TerrainCamOffsetZ");
    private readonly int _terrainCamOffsetY = Shader.PropertyToID("_TerrainCamOffsetY");
    private readonly int _terrainCamFarClip = Shader.PropertyToID("_TerrainCamFarClip");
    
    private Camera _camera;
    [SerializeField]
    private RenderTexture _terrainDepthTexture;
    [SerializeField]
    private RenderTexture _terrainColorTexture;

    #endregion

    #region Unity Functions

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        CleanUp();
    }

    #endregion

    #region Custom Functions

    public void Init()
    {
        //Create Depth cam and Color cam
        InitializeDepthCamAndTexture();
        
        //Make sure the shader and texture are assigned in the inspector
        //if (depthShader != null && _terrainDepthTexture != null)
        //{
        //    //Set the camera replacement shader to the depth shader that we will assign in the inspector 
        //    _camera.SetReplacementShader(depthShader, "RenderType");
        //    //set the target render texture of the camera to the depth texture 
        //    _camera.targetTexture = _terrainDepthTexture;
        //    //set the render texture we just created as a global shader texture variable
        //    Shader.SetGlobalTexture(_terrainDepthTextureID, _terrainDepthTexture);
        //}
        //else
        //{
        //    Debug.Log("You need to assign the depth shader and depth texture in the inspector");
        //}
    }

    private void CleanUp()
    {
        if (_camera)
        {
            _camera.targetTexture = null;
            SafeDestroy(_camera.gameObject);
        }
        if (_terrainDepthTexture)
        {
            SafeDestroy(_terrainDepthTexture);
        }
    }

    [ContextMenu("Bake Terrain Depth Texture")]
    public void BackTerrainDepth()
    {
        InitializeDepthCamAndTexture();
    }

    private void InitializeDepthCamAndTexture()
    {
        //if the camera hasn't been assigned then assign it
        //test code
        if (_camera == null)
        {
            //test code
            //_camera = GetComponent<Camera>();
            var go =
                new GameObject("depthCamera") {hideFlags = HideFlags.HideAndDontSave}; //create the cameraObject
            _camera = go.AddComponent<Camera>();
        }
        var additionalCamData = _camera.GetUniversalAdditionalCameraData();
        additionalCamData.renderShadows = false;
        additionalCamData.requiresColorOption = CameraOverrideOption.Off;
        additionalCamData.requiresDepthOption = CameraOverrideOption.Off;

        var t = _camera.transform;
        //var depthExtra = 100.0f;
        //t.position = Vector3.up * (transform.position.y + depthExtra);//center the camera on this water plane height
        t.position = transform.position;
        t.up = Vector3.forward;//face the camera down
        
        _camera.enabled = true;
        _camera.orthographic = true;
        //_camera.orthographicSize = 250;//hardcoded = 1k area - TODO
        _camera.orthographicSize = 250;//hardcoded = 1k area - TODO
        _camera.nearClipPlane =0.01f;
        _camera.farClipPlane = 300f;
        _camera.allowHDR = false;
        _camera.allowMSAA = false;
        _camera.cullingMask = TerrainMask;

        //Generate RT
        if (!_terrainDepthTexture)
        {
            _terrainDepthTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        }

        if (!_terrainColorTexture)
        {
            _terrainColorTexture = new RenderTexture(2048, 2048, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        }
        
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
        {
            _terrainDepthTexture.filterMode = FilterMode.Point;
            _terrainColorTexture.filterMode = FilterMode.Point;
        }
        
        _terrainDepthTexture.wrapMode = TextureWrapMode.Clamp;
        _terrainDepthTexture.name = "TerrainDepthMap";

        _terrainColorTexture.wrapMode = TextureWrapMode.Clamp;
        _terrainColorTexture.name = "TerrainColorMap";
        
        //do depth capture
        _camera.targetTexture = _terrainDepthTexture;
        _camera.Render();
        Shader.SetGlobalTexture(_terrainDepthTextureID, _terrainDepthTexture);
        
        //do color capture
        _camera.targetTexture = _terrainColorTexture;
        _camera.SetReplacementShader(UnlitTerrainShader, "RenderType");
        _camera.Render();
        Shader.SetGlobalTexture(_terrainColorTextureID, _terrainColorTexture);
        //Debug.Log("Finished Depth Rendering");
        
        //the total width of the bounding box of our cameras view
        Shader.SetGlobalFloat(_terrainCamScale, _camera.orthographicSize * 2);
        var camPosition = _camera.transform.position;
        //find the bottom corner of the texture in world scale by subtracting the size of the camera from its x and z position
        Shader.SetGlobalFloat(_terrainCamOffsetX, camPosition.x - _camera.orthographicSize);
        Shader.SetGlobalFloat(_terrainCamOffsetZ, camPosition.z - _camera.orthographicSize);
        //we'll also need the relative y position of the camera, lets get this by subtracting the far clip plane from the camera y position
        Shader.SetGlobalFloat(_terrainCamOffsetY, camPosition.y - _camera.farClipPlane);
        //we'll also need the far clip plane itself to know the range of y values in the depth texture
        Shader.SetGlobalFloat(_terrainCamFarClip, _camera.farClipPlane);

        _camera.enabled = false;
        _camera.targetTexture = null;
    }

    private static void SafeDestroy(Object o)
    {
        if(Application.isPlaying)
            Destroy(o);
        else
            DestroyImmediate(o);
    }
    #endregion



}
