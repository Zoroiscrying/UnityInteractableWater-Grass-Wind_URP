using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GlobalWindManager : MonoBehaviour
{
    [SerializeField] private Texture2D windNoiseTexture;
    private readonly int _windNoiseTextureID = Shader.PropertyToID("_WindNoiseTexture");

    [SerializeField] private float worldPositionScale = 1;
    private readonly int _windPositionScaleID = Shader.PropertyToID("_WindPosMult");

    [SerializeField] private float windTimeScale = 1;
    private readonly int _windTimeScaleID = Shader.PropertyToID("_WindTimeMult");
    
    [SerializeField] private float windTextureScale = 1;
    private readonly int _windTextureScaleID = Shader.PropertyToID("_WindTexMult");
    
    [SerializeField] private Vector3 windStrength = Vector3.one;
    private readonly int _windStrengthID = Shader.PropertyToID("_WindStrength");
    
    private void OnEnable()
    {
        UpdateWindProperties();
    }

    private void OnValidate()
    {
        UpdateWindProperties();
    }

    private void UpdateWindProperties()
    {
        if (windNoiseTexture)
        {
            Shader.SetGlobalTexture(_windNoiseTextureID, windNoiseTexture);   
        }
        Shader.SetGlobalFloat(_windPositionScaleID, worldPositionScale);
        Shader.SetGlobalFloat(_windTimeScaleID, windTimeScale);
        Shader.SetGlobalFloat(_windTextureScaleID, windTextureScale);
        Shader.SetGlobalVector(_windStrengthID, windStrength);
    }
    
}
