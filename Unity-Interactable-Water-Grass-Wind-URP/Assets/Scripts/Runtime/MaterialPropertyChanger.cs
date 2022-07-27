using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MaterialPropertyChanger : MonoBehaviour
{
    public Color OverrideCol = Color.white;
    public string PropertyName = "_SurfaceColor";

    private MaterialPropertyBlock _materialPropertyBlock;
    private Renderer _renderer;
    
    // Start is called before the first frame update
    void Start()
    {
        _renderer = this.GetComponent<Renderer>();
        _materialPropertyBlock = new MaterialPropertyBlock();
        if (_renderer)
        {
            if (_materialPropertyBlock != null)
            {
                _renderer.GetPropertyBlock(_materialPropertyBlock, 0);
                _materialPropertyBlock.SetColor(PropertyName, OverrideCol);
                _renderer.SetPropertyBlock(_materialPropertyBlock, 0);
            }
        }
    }

    private void OnValidate()
    {
        if (_renderer)
        {
            if (_materialPropertyBlock != null)
            {
                _renderer.GetPropertyBlock(_materialPropertyBlock, 0);
                _materialPropertyBlock.SetColor(PropertyName, OverrideCol);
                _renderer.SetPropertyBlock(_materialPropertyBlock, 0);      
            }
        }
    }
}
