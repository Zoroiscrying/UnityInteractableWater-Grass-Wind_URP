using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class VolumetricCloudRenderer : MonoBehaviour
{
    public int CloudResolution = 20;
    public float CloudHeight;
    public Mesh CloudMesh;
    public Material CloudMat;

    private float _offset;
    private Matrix4x4 _cloudPosMatrix;

    [SerializeField] private Gradient cloudGradient;
    private Texture2D _cloudGradientTexture;

    public bool shadowCasting, shadowReceive, useLightProbes;

    private void OnEnable()
    {
        InitializeCloudMatProperties();
        RenderPipelineManager.beginCameraRendering += ExecuteCloudMeshRendering;
    }

    private void OnValidate()
    {
        InitializeCloudMatProperties();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= ExecuteCloudMeshRendering;
    }

    private void InitializeCloudMatProperties()
    {
        var currentTransform = this.transform;
        CloudMat.SetFloat("_CloudsWorldPos", currentTransform.position.y);
        CloudMat.SetFloat("_CloudHeight", CloudHeight);

        if (!_cloudGradientTexture)
        {
            _cloudGradientTexture = new Texture2D(256, 1){wrapMode = TextureWrapMode.Clamp};
        }
        
        for (var x = 0; x < 256 ; x++)
        {
            var color = cloudGradient.Evaluate(x / 256f);
            _cloudGradientTexture.SetPixel(x,0,color);
        }
            
        _cloudGradientTexture.Apply();
        
        CloudMat.SetTexture("_TexCloudGradient", _cloudGradientTexture);
    }

    private void RenderCloudMesh()
    {
        var currentTransform = this.transform;
        _offset = CloudHeight / CloudResolution / 2f;
        var initPos = transform.position + Vector3.up * (_offset * CloudResolution) / 2f;
        for (int i = 0; i < CloudResolution; i++)
        {
            // take into consideration - translation, rotation, scale of clouds-gen object
            _cloudPosMatrix = Matrix4x4.TRS(
                initPos - Vector3.up * (_offset * i), 
                currentTransform.rotation, 
                currentTransform.localScale);
            // push mesh data to render without editor overhead of managing multiple objects 
            Graphics.DrawMesh(CloudMesh, _cloudPosMatrix, CloudMat, 
                0, Camera.current, 0, null, shadowCasting, shadowReceive);
        }
    }

    private void ExecuteCloudMeshRendering(ScriptableRenderContext context, Camera camera)
    {
        RenderCloudMesh();
    }

}
