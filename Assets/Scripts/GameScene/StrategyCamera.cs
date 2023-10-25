using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class StrategyCamera : MonoBehaviour
{
    public bool doFollowTransform;
    public Transform followTarget;

    public bool doKeyboardMovement = false;
    public bool doMouseMovement = true;
    
  
    public Transform cameraTransform;

    public Vector2 zoomMinMaxLimits = new Vector2(5, 20);
    public Vector2 camAngleYMinMaxLimits = new Vector2(0, 90);

    public float normalSpeed = 0.5f;
    public float fastSpeed = 3f;

    public float movementSpeed;
    public float zoomSpeed = 10f;
    public float movementTime;
    public float rotationAmount;
    [FormerlySerializedAs("zoomAmount")] public Vector3 zoomVector;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;


    public Vector3 rotateStartPosition;
    public Vector3 rotateCurrentPosition;

    private bool PauseGame = false;
    
    // Start is called before the first frame update
    void Start()
    {
        // set the camera's rotation to look directly at the parent object
        cameraTransform.LookAt(transform.position);

        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (followTarget != null && !PauseGame)
        {
            transform.position = followTarget.position;
            newPosition = transform.position;
        }


        RecalculateZoom();

        if (doMouseMovement)
        {
            HandleMouseInput();
        }

        if (doKeyboardMovement || PauseGame)
        {
            HandleMovementInput();
        }

        ApplyMovement();

        // if (Input.GetKeyDown(KeyCode.Escape))
        // {
        //     followTarget = null;
        // }
    }

    private void RecalculateZoom()
    {
        // the zoom vector is always the local position of the camera, normalised then multiplied by the zoom amount
        zoomVector = cameraTransform.localPosition.normalized * (zoomSpeed * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        // get the new zoom magnitude
        float newZoomMagnitude = Mathf.Clamp(newZoom.magnitude, zoomMinMaxLimits.x, zoomMinMaxLimits.y);
        // set the new zoom vector to the new zoom magnitude
        var currentZoomMagnitude = cameraTransform.localPosition.magnitude;
        newZoom = newZoomMagnitude * cameraTransform.localPosition.normalized;

        // clamp the camera 
        // z rotation should be 0 and x rotation should be clamped between the min and max values
        newRotation =
            Quaternion.Euler(
                newRotation.eulerAngles.x,
                newRotation.eulerAngles.y, 0);
        
        // todo: clamp the camera rotation in the up/down direction

        if (PauseGame||(doFollowTransform && followTarget != null))
        {
            transform.position = Vector3.Lerp(transform.position, newPosition, movementTime * Time.deltaTime);
        }


        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, movementTime * Time.deltaTime);
        cameraTransform.localPosition =
            Vector3.Lerp(cameraTransform.localPosition, newZoom, movementTime * Time.deltaTime);
    }

    private void HandleMouseInput()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomVector;
        }


        


        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;

            Vector3 difference = rotateStartPosition - rotateCurrentPosition;

            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));

            // the y difference is used to rotate the camera up/down around the x axis
            newRotation *= Quaternion.Euler(Vector3.right * (difference.y / 5f));
        }
    }

    void HandleMovementInput()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementSpeed = fastSpeed;
        }
        else
        {
            movementSpeed = normalSpeed;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += transform.forward * movementSpeed;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition -= transform.forward * movementSpeed;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition -= transform.right * movementSpeed;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += transform.right * movementSpeed;
        }

        // if (Input.GetKey(KeyCode.Q))
        // {
        //     newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        // }
        //
        // if (Input.GetKey(KeyCode.E))
        // {
        //     newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        // }
        //
        // if (Input.GetKey(KeyCode.R))
        // {
        //     newZoom += zoomVector;
        // }
        //
        // if (Input.GetKey(KeyCode.F))
        // {
        //     newZoom -= zoomVector;
        // }
        
        newPosition.y = transform.position.y;
    }

    public void HandlePause(bool pauseGame)
    {
        PauseGame = pauseGame;


    }
}