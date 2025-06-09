using Sciphone;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

[Serializable]
public class BasicFollow : BaseCameraMode
{
    public float pivotHeight = 1.5f;
    public Vector3 posOffset = Vector3.back * 5f;
    public Vector3 rotOffset = Vector3.right * 25f;
    public float topClampAngle = 75f;
    public float bottomClampAngle = 75f;

    public Vector3 smoothTime = 2f * Vector3.one;
    private Transform refTransform;
    private Vector3 vel = Vector3.zero;

    public bool horizontalRecentering = true;
    public float recenterTime = 50f;
    public float recenterDelay = 3f;
    private float recenterTimer = 0f;
    private Vector3 recenterVel = Vector3.zero;
    private Vector3 followTargetPrevPos;

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
        if (lookInput == Vector2.zero && moveInput != Vector2.zero)
        {
            recenterTimer += dt;
        }
        else if (lookInput != Vector2.zero)
        {
            recenterTimer = 0f;
            recenterVel = Vector3.zero;
        }

        if (horizontalRecentering && recenterTimer > recenterDelay)
        {
            // Horizontal Recentering
            var targetFwd = followTarget.position - followTargetPrevPos;
            if (Vector3.Angle(targetFwd, cameraTransform.forward.With(y: 0)) > 90)
            {
                var right = Vector3.Project(targetFwd, cameraTransform.right.With(y: 0));
                var front = -(targetFwd - right);
                targetFwd = right + front;
            }
            targetFwd = targetFwd.With(y: 0).normalized;
            var dampedFwd = Vector3.SmoothDamp(controller.transform.forward, targetFwd, ref recenterVel, recenterTime * dt);
            verticalRot = Quaternion.LookRotation(dampedFwd).eulerAngles.x;
            if (verticalRot > 180)
                verticalRot -= 360f;
            horizontalRot = Quaternion.LookRotation(dampedFwd).eulerAngles.y;
        }
        followTargetPrevPos = followTarget.position;

        verticalRot = Mathf.Clamp(verticalRot, -topClampAngle - rotOffset.x, bottomClampAngle - rotOffset.x);

        //Apply rotations to pivot
        controller.transform.rotation = Quaternion.Euler(verticalRot, horizontalRot, 0);
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
    }
    public override IEnumerator SetCameraTransform(CameraController controller)
    {
        Vector3 currentPos = cameraTransform.localPosition;
        Vector3 targetPos = posOffset;

        Quaternion currentRot = cameraTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(rotOffset);

        var remainingTime = switchTime;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            cameraTransform.localPosition = Vector3.Lerp(currentPos, targetPos, 1 - remainingTime / switchTime);
            cameraTransform.localRotation = Quaternion.Lerp(currentRot, targetRot, 1 - remainingTime / switchTime);
            yield return null;
        }
    }
}