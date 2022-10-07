using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform camera;
    public float speed = 6f;
    public float smoothTime = 0.1f;

    private bool _isMoving;
    private Vector3 _moveDirection;
    private float _turnSmoothVelocity;
    
    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude >= 0.1)
        {
            _isMoving = true;
            _moveDirection = direction;
        }
        else
        {
            _isMoving = false;
        }
    }

    private void FixedUpdate()
    {
        if (!_isMoving) return;
        
        var targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg + camera.eulerAngles.y;
        var angle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, 
            targetAngle, 
            ref _turnSmoothVelocity, 
            smoothTime
        );
            
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        var moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        controller.Move(moveDirection.normalized * (speed * Time.deltaTime));
    }
}
