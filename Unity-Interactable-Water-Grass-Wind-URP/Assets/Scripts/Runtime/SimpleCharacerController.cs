using System;
using System.Collections;
using System.Collections.Generic;
using GIRS.Utility;
using UnityEngine;
using URPShaderTestBed.Water.Scripts;

public class SimpleCharacerController : MonoBehaviour
{
    CharacterController characterController;

    public LayerMask WaterMask;
    public WaterSimulationManager simManager;

    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    private Vector3 characterFaceDriection = Vector3.forward;
    private Vector3 moveDirection = Vector3.zero;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (characterController.isGrounded)
        {
            // We are grounded, so recalculate
            // move direction directly from axes

            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
            moveDirection *= speed;

            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
            
            characterFaceDriection = Vector3.Slerp(characterFaceDriection, moveDirection, 10f * Time.deltaTime);
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);
        
        // Animation
        this.transform.forward = characterFaceDriection;
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("Good");
        if (characterController.velocity.magnitude > 0.5f)
        {
            if (simManager)
            {
                simManager.RegisterCollision(this.transform.position);
            }   
        }
    }
}
