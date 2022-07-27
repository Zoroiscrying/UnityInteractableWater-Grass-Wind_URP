using System;
using UnityEngine;

namespace URPShaderTestBed.Water.Scripts
{
    public class RegisterWaterSimulationTarget : MonoBehaviour
    {
        private void Start()
        {
            WaterSimulationManager.WaterSimTarget = this.transform;
        }
    }
}