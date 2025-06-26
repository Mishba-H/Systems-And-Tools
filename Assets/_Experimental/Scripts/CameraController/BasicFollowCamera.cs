using Sciphone;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

[Serializable]
public class BasicFollow : BaseCameraMode
{
    public float pivotHeight = 1.5f;
    public Vector3 camPosOffset = Vector3.back * 5f;
    public Vector3 camRotOffset = Vector3.right * 25f;
    public float camSpeed = 15f;

    public Vector2 pitchRange = new Vector2(75f, 75f); // Looking up/down
    public Vector2 yawRange = new Vector2(0f, 360f); // Looking side to side

    [HideInInspector] public float pitchAngle;
    [HideInInspector] public float yawAngle;

    public Vector3 smoothTime = 2f * Vector3.one;
    private Vector3 vel = Vector3.zero;

    public bool verticalRecentering = true;
    public bool horizontalRecentering = true;
    public float recenterTime = 50f;
    public float recenterDelay = 3f;
    private float recenterTimer = 0f;
    private Vector3 recenterVel = Vector3.zero;
    private Vector3 followTargetPrevPos;

    [HideInInspector] public Vector2 lookInput;
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public InputDevice device;
    public float mouseSensitivity = 10f;
    public float gamepadSensitivity = 10f;
    public float touchscreenSensitivity = 10f;

    private void OnLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        device = context.control.device;
    }
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public override void Initialize(CameraController controller)
    {
        base.Initialize(controller);

        controller.inputReader.Subscribe("Look", OnLookInput);
        controller.inputReader.Subscribe("Move", OnMoveInput);
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
        HandleLookInput(dt / controller.timeScale);

        Vector3 refUp = followTransform.up;
        Vector3 worldForward = Vector3.forward;
        if (Mathf.Abs(Vector3.Dot(refUp, worldForward)) == 1f)
            worldForward = Vector3.right;
        Vector3 refRight = Vector3.Normalize(Vector3.Cross(refUp, worldForward));

        //Debug.DrawRay(pivotTransform.position, refUp, Color.green);
        //Debug.DrawRay(pivotTransform.position, refRight, Color.red);
        //Debug.DrawRay(pivotTransform.position, refForward, Color.blue);

        HandleRecentering(dt, refUp, refRight);

        Quaternion pitchRotation = Quaternion.AngleAxis(pitchAngle, refRight);
        Quaternion yawRotation = Quaternion.AngleAxis(yawAngle, refUp);
        Quaternion targetRotation = yawRotation * pitchRotation;

        pivotTransform.rotation = targetRotation;
        pivotTransform.rotation = Quaternion.LookRotation(pivotTransform.forward, followTransform.up);
    }
    public void HandleLookInput(float dt)
    {
        if (device is Mouse)
        {
            pitchAngle -= lookInput.y * mouseSensitivity * 0.01f;
            yawAngle += lookInput.x * mouseSensitivity * 0.01f;
        }
        else if (device is Gamepad)
        {
            pitchAngle -= lookInput.y * dt * gamepadSensitivity * 10;
            yawAngle += lookInput.x * dt * gamepadSensitivity * 10;
        }
        else if (device is Touchscreen)
        {
            pitchAngle -= lookInput.y * touchscreenSensitivity * 0.01f;
            yawAngle += lookInput.x * touchscreenSensitivity * 0.01f;
        }

        pitchAngle = Mathf.Clamp(pitchAngle, -pitchRange.x, pitchRange.y);
        if (pitchAngle >= 180f) pitchAngle -= 360f;
        else if (pitchAngle <= -180f) pitchAngle += 360f;

        yawAngle = Mathf.Clamp(yawAngle, -yawRange.x, yawRange.y);
        if (yawAngle >= 180f) yawAngle -= 360f;
        else if (yawAngle <= -180f) yawAngle += 360f;
    }
    public void HandleRecentering(float dt, Vector3 refUp, Vector3 refRight)
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

        if (recenterTimer > recenterDelay)
        {
            var centeredForward = (followTransform.position - followTargetPrevPos);
            centeredForward = Vector3.ProjectOnPlane(centeredForward, followTransform.up).normalized;

            var camForwardOnPlane = Vector3.ProjectOnPlane(camTransform.forward, followTransform.up).normalized;
            if (Vector3.Angle(centeredForward, camForwardOnPlane) > 90f)
            {
                var front = Vector3.Project(centeredForward, camForwardOnPlane);
                var right = centeredForward - front;
                centeredForward = right - front;
            }
            var dampedForward = Vector3.SmoothDamp(pivotTransform.forward, centeredForward, ref recenterVel, recenterTime * dt);

            if (horizontalRecentering || verticalRecentering)
            {
                Vector3 refForward = Vector3.Cross(refRight, refUp);
                Quaternion targetRot = Quaternion.LookRotation(dampedForward, refUp);

                Quaternion referenceRotation = Quaternion.LookRotation(refForward, refUp);
                Quaternion localRot = Quaternion.Inverse(referenceRotation) * targetRot;
                Vector3 euler = localRot.eulerAngles;

                if (horizontalRecentering)
                {
                    yawAngle = Extensions.NormalizeAngle(euler.y);
                }

                if (verticalRecentering)
                {
                    pitchAngle = Extensions.NormalizeAngle(euler.x);
                }
            }
        }
        followTargetPrevPos = followTransform.position;
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
    }
    public override IEnumerator SetCameraTransform(CameraController controller)
    {
        Vector3 currentPos = camTransform.localPosition;
        Vector3 targetPos = camPosOffset;

        Quaternion currentRot = camTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(camRotOffset);

        var remainingTime = switchTime;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            camTransform.localPosition = Vector3.Lerp(currentPos, targetPos, 1 - remainingTime / switchTime);
            camTransform.localRotation = Quaternion.Lerp(currentRot, targetRot, 1 - remainingTime / switchTime);
            yield return null;
        }
    }
}