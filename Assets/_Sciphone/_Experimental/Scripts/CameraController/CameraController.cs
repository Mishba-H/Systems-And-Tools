using System;
using System.Collections;
using UnityEngine;
using Sciphone;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif


public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    public Transform cameraTransform;
    public Transform playerTransform;
    public LayerMask collisionLayer;

    [SerializeReference, Polymorphic] public BaseCameraMode[] cameraModes;
    public ICameraMode currentCameraMode;

    private void Start()
    {
        foreach (var cameraMode in cameraModes)
        {
            cameraMode.Initialize(this);
        }
        currentCameraMode = cameraModes[0];
    }

    private void LateUpdate()
    {
        currentCameraMode.HandleLookInput(this, Time.deltaTime);
        currentCameraMode.HandlePivotRotation(this, Time.deltaTime);
        currentCameraMode.HandlePivotPosition(this, Time.deltaTime);
        currentCameraMode.HandleCamera(this, Time.deltaTime);
    }

    private void FixedUpdate()
    {
    }

    [Button(nameof(SetCameraTransform))]
    public void SetCameraTransform()
    {
        StartCoroutine(currentCameraMode.SetCameraTransform(this));
    }
}

public interface ICameraMode
{
    public void Initialize(CameraController controller);
    public void HandleLookInput(CameraController controller, float dt);
    public void HandlePivotPosition(CameraController controller, float dt); // Adjusts position for camera pivot/ holder
    public void HandlePivotRotation(CameraController controller, float dt); // Adjusts rotations for camera pivot/ holder
    public void HandleCamera(CameraController controller, float dt); // Moves the camera in case of collisions
    public IEnumerator SetCameraTransform(CameraController controller); 
}
[Serializable]
public abstract class BaseCameraMode : ICameraMode
{
    public string name = "CameraMode";
    [HideInInspector] public Transform cameraTransform;
    [HideInInspector] public Transform followTarget;
    [HideInInspector] public LayerMask collisionLayer;
    public float cameraRadius = 0.3f;
    public float cameraSpeed = 15f;

    [HideInInspector] public Vector2 lookInput;
    [HideInInspector] public InputDevice device;
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public float verticalRot;
    [HideInInspector] public float horizontalRot;
    public float mouseSensitivity = 10f;
    public float gamepadSensitivity = 10f;
    public float touchscreenSensitivity = 10f;

    public float switchTime = 0.5f;

    private void OnLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        device = context.control.device;
    }
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public virtual void Initialize(CameraController controller)
    {
        InputReader.instance.Subscribe("Look", OnLookInput);
        InputReader.instance.Subscribe("Move", OnMoveInput);

        cameraTransform = controller.cameraTransform;
        followTarget = controller.playerTransform;
        collisionLayer = controller.collisionLayer;

        controller.StartCoroutine(SetCameraTransform(controller));
    }

    public void HandleLookInput(CameraController controller, float dt)
    {
        if (device is Mouse)
        {
            verticalRot -= lookInput.y * mouseSensitivity * 0.01f;
            horizontalRot += lookInput.x * mouseSensitivity * 0.01f;
        }
        else if (device is Gamepad)
        {
            verticalRot -= lookInput.y * dt * gamepadSensitivity * 10;
            horizontalRot += lookInput.x * dt * gamepadSensitivity * 10;
        }
        else if (device is Touchscreen)
        {
            verticalRot -= lookInput.y * touchscreenSensitivity * 0.01f;
            horizontalRot += lookInput.x * touchscreenSensitivity * 0.01f;
        }
    }
    public virtual void HandleVirtualTarget(CameraController controller, float dt)
    {
        throw new NotImplementedException();
    }
    public virtual void HandlePivotPosition(CameraController controller, float dt)
    {
        throw new NotImplementedException();
    }
    public virtual void HandlePivotRotation(CameraController controller, float dt)
    {
        throw new NotImplementedException();
    }
    public virtual void HandleCamera(CameraController controller, float dt)
    {
        throw new NotImplementedException();
    }
    public virtual IEnumerator SetCameraTransform(CameraController controller)
    {
        throw new NotImplementedException();
    }
}


