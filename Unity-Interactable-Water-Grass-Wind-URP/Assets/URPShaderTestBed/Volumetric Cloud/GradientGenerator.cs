using System;
using UnityEngine;

namespace URPShaderTestBed.Volumetric_Cloud
{
    [ExecuteInEditMode]
    public class GradientGenerator : MonoBehaviour
    {
        public Material material;
        public string textureProperty;
        public bool realtimeGeneration;
        public Gradient lutGradient;
        public Vector2Int lutTextureSize;
        public Texture2D lutTexture;

        private void Update()
        {
            if (realtimeGeneration)
            {
                GenerateTexture();
            }
        }

        public void GenerateTexture()
        {
            lutTexture = new Texture2D(lutTextureSize.x, lutTextureSize.y) {wrapMode = TextureWrapMode.Clamp};

            for (var x = 0; x < lutTextureSize.x ; x++)
            {
                var color = lutGradient.Evaluate(x / (float) lutTextureSize.x);
                for (var y = 0; y < lutTextureSize.y; y++)
                {
                    lutTexture.SetPixel(x,y,color);
                }
            }
            
            lutTexture.Apply();
            material.SetTexture(textureProperty, lutTexture);
        }
    }
}