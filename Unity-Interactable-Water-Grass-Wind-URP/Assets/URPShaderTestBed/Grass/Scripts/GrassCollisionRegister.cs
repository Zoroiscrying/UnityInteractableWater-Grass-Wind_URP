using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class GrassCollisionRegister : MonoBehaviour
{
    //public Shader GrassShader;
    static int _collisionPosition = Shader.PropertyToID("_ColliderPosition");

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCamRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCamRendering;
    }

    private void BeginCamRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        Shader.SetGlobalVector(_collisionPosition, this.transform.position);
    }
}
