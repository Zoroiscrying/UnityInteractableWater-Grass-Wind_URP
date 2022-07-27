using UnityEngine;

namespace URPShaderTestBed.Grass
{
    [CreateAssetMenu(fileName = "NewGrassPaintBrushSetting", menuName = "Tools/GrassPainter/BrushSetting")]
    public class GrassPaintBrushSetting : ScriptableObject
    {
        [Min(0f)]
        public float GrassWidth = 1f;
    
        [Min(0f)]
        public float GrassHeight = 1f;
    
        
        /// <summary>
        /// Set the object's default values.
        /// </summary>
        public void SetDefaultValues()
        {
            GrassWidth = 0.5f;
            GrassHeight = 0.5f;
        }
        
        /// <summary>
        /// Deep copy this
        /// </summary>
        /// <returns>A new object with the same values as this</returns>
        internal GrassPaintBrushSetting DeepCopy()
        {
            GrassPaintBrushSetting copy = ScriptableObject.CreateInstance<GrassPaintBrushSetting>();
            this.CopyTo(copy);
            return copy;
        }

        /// <summary>
        /// Copy all properties to target
        /// </summary>
        /// <param name="target"></param>
        public void CopyTo(GrassPaintBrushSetting target)
        {
            target.name 						= this.name;
            target.GrassWidth					= this.GrassWidth;
            target.GrassHeight					= this.GrassHeight;
            //target._radius							= this._radius;
            //target._falloff							= this._falloff;
            //target._strength						= this._strength;
            //target._curve							= new AnimationCurve(this._curve.keys);
            //target.allowNonNormalizedFalloff		= this.allowNonNormalizedFalloff;
        }
    
    }
}
