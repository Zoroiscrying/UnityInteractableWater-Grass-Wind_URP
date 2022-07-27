using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterRippleCamera : MonoBehaviour {

    private Camera cam;
    public MeshRenderer waterPlane;

    private void Awake() {
        cam = GetComponent<Camera>();
    }

    private void Update() {
        waterPlane.sharedMaterial.SetVector("_CamPosition", transform.position);
        waterPlane.sharedMaterial.SetFloat("_OrthographicCamSize", cam.orthographicSize);
    }
}
