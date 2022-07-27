using System;
using System.Collections;
using System.Collections.Generic;
using GIRS.Utility;
using UnityEngine;
using URPShaderTestBed.Water.Scripts;

public class WaterRippleRegister : MonoBehaviour
{
    [SerializeField] private WaterSimulationManager waterSimulationManager;
    [SerializeField] private bool playerIsMoving;
    [SerializeField] private Vector3 playerPositionWS;
    private LayerMask _playerMask;

    private void Awake()
    {
        _playerMask = LayerMask.GetMask("Player");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && _playerMask.Contains(other.gameObject.layer))
        {
            if (playerIsMoving)
            {
                //Debug.Log(other.gameObject.name);
                waterSimulationManager.RegisterCollision(playerPositionWS);   
            }
        }
    }
}
