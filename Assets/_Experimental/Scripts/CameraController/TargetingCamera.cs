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
    public Vector3 camPosOffset = Vector3.back * 3.5f;
    public float camSpeed = 15f;

    public Vector3 smoothTime = 2f * Vector3.one;
    private Vector3 vel = Vector3.zero;

    public Transform lookTarget;
    public float targetCamSpeed;
    public Vector3 velo;

    private Vector3 smoothedForward;

    public void SetLookTarget(Transform targetTransform)
    {
        lookTarget = targetTransform;
    }
    public override void Initialize(CameraController controller)
    {
        base.Initialize(controller);
    }
    public override void HandlePivotPosition(CameraController controller, float dt)
    {
        var camForwardOnPlane = Vector3.ProjectOnPlane(camTransform.forward, followTransform.up);
        camForwardOnPlane = camForwardOnPlane == Vector3.zero ? Vector3.ProjectOnPlane(pivotTransform.forward, followTransform.up) : camForwardOnPlane;
        refTransform.rotation = Quaternion.LookRotation(camForwardOnPlane, followTransform.up);

        var targetPosition = followTransform.position + pivotHeight * followTransform.up;

        Vector3 localOffset = refTransform.InverseTransformDirection(controller.transform.position - targetPosition);

        localOffset.x = Mathf.SmoothDamp(localOffset.x, 0, ref vel.x, smoothTime.x * dt);
        localOffset.y = Mathf.SmoothDamp(localOffset.y, 0, ref vel.y, smoothTime.y * dt);
        localOffset.z = Mathf.SmoothDamp(localOffset.z, 0, ref vel.z, smoothTime.z * dt);

        controller.transform.position = targetPosition + refTransform.TransformDirection(localOffset);
    }
    public override void HandlePivotRotation(CameraController controller, float dt)
    {
        Vector3 targetForward = lookTarget.position - controller.transform.position;
        targetForward = Vector3.ProjectOnPlane(targetForward, followTransform.up).normalized;
        smoothedForward = Vector3.SmoothDamp(smoothedForward, targetForward, ref velo, targetCamSpeed * dt);
        pivotTransform.rotation = Quaternion.LookRotation(smoothedForward, followTransform.up);
    }
    public override void HandleCamera(CameraController controller, float dt)
    {
        //Check for camera collision and move the camera
        var dist = camPosOffset.magnitude;
        var dir = (pivotTransform.rotation * camPosOffset).normalized;
        if (Physics.SphereCast(pivotTransform.position, cameraRadius, dir, out RaycastHit cameraHit, dist, collisionLayer))
        {
            var cameraCurrentPos = camTransform.position;
            var cameraTargetPos = pivotTransform.position + dir * cameraHit.distance;
            camTransform.position = Vector3.Lerp(cameraCurrentPos, cameraTargetPos, camSpeed * dt);
        }
        else
        {
            var cameraCurrentPos = camTransform.localPosition;
            var cameraTargetPos = camPosOffset;
            camTransform.localPosition = Vector3.Lerp(cameraCurrentPos, cameraTargetPos, camSpeed * dt);
        }

        var targetFwd = lookTarget.position - camTransform.position;
        camTransform.rotation = Quaternion.LookRotation(targetFwd, followTransform.up);
    }
    public override IEnumerator SetCameraTransform(CameraController controller)
    {
        Vector3 currentPos = controller.cameraTransform.localPosition;
        Vector3 targetPos = camPosOffset;

        var remainingTime = switchTime;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            controller.cameraTransform.localPosition = Vector3.Lerp(currentPos, targetPos, 1 - remainingTime / switchTime);
            yield return null;
        }
    }
}