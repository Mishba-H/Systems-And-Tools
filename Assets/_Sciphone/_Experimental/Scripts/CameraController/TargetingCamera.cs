using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

[Serializable]
public class TargetingCamera : BaseCameraMode
{
    public float pivotHeight = 1.5f;
    public Vector3 posOffset = Vector3.back * 3.5f;
    public float topClampAngle = 75f;
    public float bottomClampAngle = 75f;

    public Vector3 smoothTime = 2f * Vector3.one;
    private Transform refTransform;
    private Vector3 vel = Vector3.zero;

    public Transform lookTarget;
    public float targetCamSpeed;
    public Vector3 velo;

    public void SetLookTArget(Transform targetTransform)
    {
        lookTarget = targetTransform;
    }

    public override void Initialize(CameraController controller)
    {
        base.Initialize(controller);
        refTransform = new GameObject("CameraReference").transform;
        refTransform.SetParent(controller.transform);
    }
    public override void HandlePivotPosition(CameraController controller, float dt)
    {
        //Update position of camera holder(pivot)
        refTransform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(controller.cameraTransform.forward, Vector3.up), Vector3.up);

        var targetPosition = followTarget.position
            + pivotHeight * Vector3.up;

        Vector3 localOffset = refTransform.InverseTransformDirection(controller.transform.position - targetPosition);

        localOffset.x = Mathf.SmoothDamp(localOffset.x, 0, ref vel.x, smoothTime.x * dt);
        localOffset.y = Mathf.SmoothDamp(localOffset.y, 0, ref vel.y, smoothTime.y * dt);
        localOffset.z = Mathf.SmoothDamp(localOffset.z, 0, ref vel.z, smoothTime.z * dt);

        controller.transform.position = targetPosition + refTransform.TransformDirection(localOffset);
    }
    public override void HandlePivotRotation(CameraController controller, float dt)
    {
        var targetFwd = lookTarget.position - controller.transform.position;
        controller.transform.forward = Vector3.SmoothDamp(controller.transform.forward, targetFwd, ref velo, targetCamSpeed * dt);
    }
    public override void HandleCamera(CameraController controller, float dt)
    {
        //Check for camera collision and move the camera
        var dist = posOffset.magnitude;
        var dir = (controller.transform.rotation * posOffset).normalized;
        if (Physics.SphereCast(controller.transform.position, cameraRadius, dir, out RaycastHit cameraHit, dist, collisionLayer))
        {
            var cameraCurrentPos = cameraTransform.position;
            var cameraTargetPos = controller.transform.position + dir * cameraHit.distance;
            cameraTransform.position = Vector3.Lerp(cameraCurrentPos, cameraTargetPos, cameraSpeed * dt);
        }
        else
        {
            var cameraCurrentPos = cameraTransform.localPosition;
            var cameraTargetPos = posOffset;
            cameraTransform.localPosition = Vector3.Lerp(cameraCurrentPos, cameraTargetPos, cameraSpeed * dt);
        }

        var targetFwd = lookTarget.position - cameraTransform.position;
        cameraTransform.forward = Vector3.SmoothDamp(cameraTransform.forward, targetFwd, ref velo, targetCamSpeed * dt);
    }
    public override IEnumerator SetCameraTransform(CameraController controller)
    {
        Vector3 currentPos = controller.cameraTransform.localPosition;
        Vector3 targetPos = posOffset;

        var remainingTime = switchTime;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            controller.cameraTransform.localPosition = Vector3.Lerp(currentPos, targetPos, 1 - remainingTime / switchTime);
            yield return null;
        }
    }
}