using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 10.0f;
    public float turnSpeed = 25.0f;

    public Camera mainCamera;

    public Vector3 targetPoint;

    public Rigidbody rb;
    private bool PauseGame = false;

    public Animator Animator;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 10);
        Gizmos.DrawSphere(targetPoint, 0.5f);
    }

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var forward = transform.forward;
        var right = transform.right;
        var rbVelocity = rb.velocity;
        var forwardVelocity = Vector3.Project(rbVelocity, forward).magnitude;
        var rightVelocity = Vector3.Project(rbVelocity, right).magnitude;
        
        Animator.SetFloat("InputX", Input.GetAxis("Horizontal"));
        Animator.SetFloat("InputY", Input.GetAxis("Vertical"));
    }

    private void FixedUpdate()
    {
        if (PauseGame)
        {
            transform.LookAt(new Vector3(targetPoint.x, transform.position.y, targetPoint.z));
            rb.velocity = Vector3.zero;
            return;
        }
        // wasd movement

        var desiredVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // forward is always the camera's forward
        var forward = mainCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();
        // snap the forward vector to the grid with 45 degree increments
        // forward = SnapToGrid(forward, 45);


        // turn the desired velocity to the camera's forward
        desiredVelocity = Quaternion.LookRotation(forward) * desiredVelocity;

        // desiredVelocity = transform.TransformDirection(desiredVelocity);
        desiredVelocity *= speed;
        rb.velocity = desiredVelocity;

        // face in direction of mouse hit

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }

        // rotate to face mouse hit, rotate around y axis only
        transform.LookAt(new Vector3(targetPoint.x, transform.position.y, targetPoint.z));
        
        // rotate to face rb velocity, rotate around y axis only
        // transform.LookAt(new Vector3(desiredVelocity.x, transform.position.y, desiredVelocity.z));
    }

    private Vector3 SnapToGrid(Vector3 forward, int i)
    {
        // snaps a given vector to given angle increments
        // get the forward vector's angle in signed degrees
        float angle = Vector3.SignedAngle(Vector3.forward, forward, Vector3.up);
        // snap the angle to the grid
        angle = Mathf.Round(angle / i) * i;
        // convert the angle back to a vector
        return Quaternion.Euler(0, angle, 0) * Vector3.forward;
    }

    public void HandlePause(bool pauseGame)
    {
        PauseGame = pauseGame;
    }
}