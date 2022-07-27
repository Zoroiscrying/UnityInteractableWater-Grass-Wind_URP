using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class TestDrawMesh : MonoBehaviour
{
    public Mesh mesh;
    public Material mat;
    private Camera _sceneCam;
    public bool shadowCasting, shadowReceive, useLightProbes;

    private void OnEnable()
    {
        _sceneCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        var matrix = Matrix4x4.TRS(
            transform.localPosition, 
            transform.rotation,
            transform.localScale);
        
        Graphics.DrawMesh(mesh, matrix, mat, 
            0, Camera.current, 0, null, shadowCasting, shadowReceive);
    }
}
