using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace URPShaderTestBed.Grass
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Geo Grass Point Mesh", menuName = "Tools/GrassPainter/GeoGrass_PointCloudMeshData")]
    public class GrassMeshPointData : ScriptableObject
    {
        [SerializeField]
        public Vector3[] positions;
        [SerializeField]
        public Color[] colors;
        [SerializeField]
        public int[] indices;
        [SerializeField]
        public Vector3[] normals;
        [SerializeField]
        public Vector2[] length;
        [SerializeField] public int indexCount;
        //private bool _serialized = false;
        //public bool Serialized => _serialized;

        public void ClearData()
        {
            //_serialized = true;
            indexCount = 0;
            positions = new Vector3[0];
            indices = new int[0];
            colors = new Color[0];
            normals = new Vector3[0];
            length = new Vector2[0];
        }
    }
}