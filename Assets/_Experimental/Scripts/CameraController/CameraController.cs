using System;
using System.Collections;
using UnityEngine;
using Sciphone;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif


public class CameraController : MonoBehaviour
{
    [Range(0.001f, 3f)] public float timeScale = 1f;

    public Transform cameraTransform;
    public Transform playerTransform;
    [HideInInspector] public Transform refTransform;
    public LayerMask cameraCollisionLayer;

    [SerializeField] internal InputReader inputReader;

    [SerializeReference, Polymorphic] public ICameraMode[] cameraModes;
    public ICameraMode currentCameraMode;

    private void Awake()
    {
        refTransform = new GameObject("CameraReference").transform;
        refTransform.SetParent(transform);
    }

    private void Start()
    {
        foreach (var cameraMode in cameraModes)
        {
            cameraMode.Initialize(this);
        }
        currentCameraMode = cameraModes[0];
        StartCoroutine(currentCameraMode.SetCameraTransform(this));
    }

    private void LateUpdate()
    {
        currentCameraMode.HandlePivotPosition(this, Time.deltaTime * timeScale);
        currentCameraMode.HandlePivotRotation(this, Time.deltaTime * timeScale);
        currentCameraMode.HandleCamera(this, Time.deltaTime * timeScale);
    }

    [Button(nameof(ResetCameraTransform))]
    public void ResetCameraTransform()
    {
        StartCoroutine(currentCameraMode.SetCameraTransform(this));
    }
}

public interface ICameraMode
{
    public void Initialize(CameraController controller);
    public void HandlePivotPosition(CameraController controller, float dt);
    public void HandlePivotRotation(CameraController controller, float dt);
    public void HandleCamera(CameraController controller, float dt);
    public IEnumerator SetCameraTransform(CameraController controller); 
}
[Serializable]
public abstract class BaseCameraMode : ICameraMode
{
    public string name = "CameraMode";
    public float cameraRadius = 0.3f;

    [HideInInspector] public Transform pivotTransform;
    [HideInInspector] public Transform camTransform;
    [HideInInspector] public Transform followTransform;
    [HideInInspector] public Transform refTransform;
    [HideInInspector] public LayerMask collisionLayer;

    public float switchTime = 0.5f;

    public virtual void Initialize(CameraController controller)
    {
        pivotTransform = controller.transform;
        camTransform = controller.cameraTransform;
        followTransform = controller.playerTransform;
        refTransform = controller.refTransform;
        collisionLayer = controller.cameraCollisionLayer;
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


