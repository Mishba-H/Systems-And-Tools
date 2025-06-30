using System;
using System.Collections;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using static UnityEditor.PlayerSettings;

//#if UNITY_EDITOR
//using Physics = Nomnom.RaycastVisualization.VisualPhysics;
//#else
//using Physics = UnityEngine.Physics;
//#endif

public class CharacterMover : MonoBehaviour
{
    public event Action<bool> OnIsGroundedValueChanged;

    public enum CapsuleOrientation
    {
        X,
        Y,
        Z
    }

    [Serializable]
    public class CapsulePreset
    {
        public string presetName;
        public CapsuleOrientation capsuleOrientation;
        public Vector3 capsuleOffset;
        public float capsuleRadius;
        public float capsuleSize;
    }

    [HideInInspector] public Character character;

    [SerializeReference, Polymorphic] public List<CapsulePreset> capsulePresets;

    #region ALGORITH_SETTINGS
    [TabGroup("Algorithm Settings")] public int maxDepth = 3;
    [TabGroup("Algorithm Settings")] public int noOfSpheres = 5;
    [TabGroup("Algorithm Settings")] public float sweepDistance = 1.5f;

    [TabGroup("Algorithm Settings")] public CapsuleOrientation capsuleOrientation = CapsuleOrientation.Y;
    [TabGroup("Algorithm Settings")] public Vector3 capsuleOffset;
    [TabGroup("Algorithm Settings")] public float capsuleRadius;
    [TabGroup("Algorithm Settings")] public float capsuleSize;

    [TabGroup("Algorithm Settings")] public float skinWidth = 0.015f;

    [TabGroup("Algorithm Settings")] public float criticalSlopeAngle = 75f;
    [TabGroup("Algorithm Settings")] public float criticalWallAngle = 30f;

    [TabGroup("Algorithm Settings")] public float maxStepHeight;
    [TabGroup("Algorithm Settings")] public AnimationCurve pushSpeedCurve;
    private Collider[] overlappingColliders;
    private Collider myCollider;
    #endregion

    #region GRAVITY_SETTINGS    
    [TabGroup("Gravity Settings")] public bool simulateGravity;
    [TabGroup("Gravity Settings")] public float simulationStep = 1 / 60f;
    [TabGroup("Gravity Settings")] public float accumulatedTime;

    [TabGroup("Gravity Settings")] public Vector3 gravityDirection = Vector3.down;
    [TabGroup("Gravity Settings")] public float gravityMagnitude = 10f;
    [TabGroup("Gravity Settings")] public float terminalVelocity = -50f;
    private Vector3 previousPosition;
    private Vector3 currentPosition;
    #endregion

    #region CHECKER_SETTINGS
    [TabGroup("Checker Settings")] public LayerMask collisionLayer;
    [TabGroup("Checker Settings")][SerializeReference] private bool isGrounded;
    [TabGroup("Checker Settings")] public RaycastHit groundHit;
    [TabGroup("Checker Settings")] public int sides = 8;
    [TabGroup("Checker Settings")] public int noOfLayers = 3;
    [TabGroup("Checker Settings")] public float groundCheckerRadius = 0.3f;
    [TabGroup("Checker Settings")] public float groundCheckerHeight = 1f;

    [TabGroup("Checker Settings")] public int stepCheckerCount;

    public bool IsGrounded
    {
        get => isGrounded;
        set
        {
            if (IsGrounded != value)
            {
                isGrounded = value;
                OnIsGroundedValueChanged?.Invoke(isGrounded);
            }
        }
    }
    #endregion

    #region TARGET_MATCHING
    [TabGroup("Target Mathcing")] public Vector3 targetPosition;
    [TabGroup("Target Mathcing")] public Vector3 targetForward;
    [TabGroup("Target Mathcing")] public float lerpTime;
    private Coroutine lerpToTargetCoroutine;
    #endregion

    public Vector3 worldVelocity;

    private void Awake()
    {
        character = GetComponent<Character>();
        myCollider = GetComponent<Collider>();
        overlappingColliders = new Collider[10];
    }

    private void Start()
    {
        character.PreUpdateLoop += Character_PreUpdateLoop;
        character.UpdateLoop += Character_UpdateLoop;
    }

    //public bool testGravityPass;
    //Vector3 testWithGravityPass;
    //Vector3 testWithoutGravityPass;
    //private void Update()
    //{
    //    if (testGravityPass)
    //    {
    //        testWithGravityPass = CollideAndSlide(worldVelocity, worldVelocity, transform.position, sweepDistance, true, 0);
    //    }
    //    else
    //    {
    //        testWithoutGravityPass = CollideAndSlide(transform.forward, transform.forward, transform.position, sweepDistance, false, 0);
    //    }
    //}

    private void Character_PreUpdateLoop()
    {
        IsGrounded = CheckGround(transform.position, out groundHit);
    }

    private void Character_UpdateLoop()
    {
        if (simulateGravity)
        {
            accumulatedTime += Time.deltaTime * character.timeScale;
            while (accumulatedTime > simulationStep)
            {
                accumulatedTime -= simulationStep;
                SimulateGravity();
            }

            // Interpolation for appearance of smooth movement
            float alpha = accumulatedTime / simulationStep;
            transform.position = Vector3.Lerp(previousPosition, currentPosition, alpha);
        }

        ResolveOverlaps(Time.deltaTime * character.timeScale);
    }

    private void SimulateGravity()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        if (localVelocity.y <= terminalVelocity)
        {
            localVelocity = localVelocity.With(y: terminalVelocity);
            worldVelocity = transform.TransformDirection(localVelocity);
        }
        else
        {
            worldVelocity += simulationStep * gravityMagnitude * gravityDirection;
        }

        worldVelocity = ProcessCollideAndSlide(worldVelocity * simulationStep) / simulationStep;
    }

    private void ResolveOverlaps(float dt)
    {
        var myCapsule = myCollider as CapsuleCollider;
        if (myCapsule.radius == 0f || myCapsule.height == 0f) return;

        Vector3 localOffset = transform.TransformDirection(capsuleOffset);
        Vector3 capsuleDir = Vector3.zero;
        switch (capsuleOrientation)
        {
            case CapsuleOrientation.X:
                capsuleDir = transform.TransformDirection(Vector3.right);
                break;
            case CapsuleOrientation.Y:
                capsuleDir = transform.TransformDirection(Vector3.up);
                break;
            case CapsuleOrientation.Z:
                capsuleDir = transform.TransformDirection(Vector3.forward);
                break;
        }

        Vector3 point1 = transform.position + localOffset + capsuleDir * (capsuleSize / 2f - capsuleRadius);
        Vector3 point2 = transform.position + localOffset - capsuleDir * (capsuleSize / 2f - capsuleRadius);
        int count = Physics.OverlapCapsuleNonAlloc(point1, point2, capsuleRadius, overlappingColliders, collisionLayer);
        //Debug.Log(count);

        Vector3 resultantPushDir = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider other = overlappingColliders[i];

            if (other == null || other == myCollider)
                continue;

            bool overlapped = Physics.ComputePenetration(myCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation, out Vector3 pushDir, out float distance);

            if (overlapped && distance > 0f)
            {
                //Debug.DrawRay(transform.position, pushDir.normalized, Color.yellow);
                if (CheckStep(transform.position, Vector3.ProjectOnPlane(-pushDir, transform.up), capsuleRadius, out _, out _, out _))
                {
                    //Debug.Log("Ignoring because of step");
                    continue;
                }

                if (CheckGroundOnCollider(other))
                {
                    //Debug.Log("Ignoring because of walkable slope");
                    continue;
                }

                resultantPushDir += pushDir * distance;
            }
        }

        //if (resultantPushDir != Vector3.zero)
        //{
        //    Debug.Log("Overlapping");
        //}
        //else
        //{
        //    Debug.Log("Not Overlapping");
        //}
        float t = Mathf.Clamp01(resultantPushDir.magnitude / capsuleRadius);
        float pushSpeed = pushSpeedCurve.Evaluate(t);
        transform.position += pushSpeed * dt * resultantPushDir.normalized;
    }

    private bool CheckGroundOnCollider(Collider other)
    {
        for (int i = 1; i < noOfLayers; i++)
        {
            float angleStep = 360f / sides;
            var radius = capsuleRadius * i / (noOfLayers - 1);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
                Vector3 rayStart = transform.position + transform.up * (groundCheckerHeight + skinWidth) + direction * radius;
                if (other.Raycast(new Ray(rayStart, -transform.up), out groundHit, groundCheckerHeight + skinWidth))
                {
                    if (Vector3.Angle(groundHit.normal, transform.up) <= criticalSlopeAngle)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void TargetMatching(Vector3 targetPosition, Vector3 targetForward, bool initiateLerp)
    {
        this.targetPosition = targetPosition;
        this.targetForward = targetForward;
        if (initiateLerp && lerpToTargetCoroutine == null)
        {
            lerpToTargetCoroutine = StartCoroutine(LerpToTarget());
        }
        if (lerpToTargetCoroutine == null)
        {
            transform.position = targetPosition;
        }
    }

    private IEnumerator LerpToTarget()
    {
        float elapsedTime = 0f;
        while (elapsedTime <= lerpTime)
        {
            elapsedTime += Time.deltaTime * character.timeScale;
            transform.position = Vector3.Lerp(transform.position, targetPosition, elapsedTime / lerpTime);
            transform.forward = Vector3.Slerp(transform.forward, targetForward, elapsedTime / lerpTime);
            yield return null;
        }
        lerpToTargetCoroutine = null;
    }

    public Vector3 ProcessCollideAndSlide(Vector3 moveVector)
    {
        Vector3 collideAndSlideVector = CollideAndSlide(moveVector, moveVector, transform.position, sweepDistance, simulateGravity, 0);
        if (collideAndSlideVector.magnitude > moveVector.magnitude)
        {
            collideAndSlideVector = collideAndSlideVector.normalized * moveVector.magnitude;
        }

        if (simulateGravity)
        {
            previousPosition = currentPosition;
            currentPosition = previousPosition + collideAndSlideVector;
        }
        else
        {
            transform.position += collideAndSlideVector;
            SnapToGround();
        }

        return collideAndSlideVector;
    }

    public Vector3 CollideAndSlide(Vector3 moveDir, Vector3 initialMoveDir, Vector3 pos, float checkDistance, bool gravityPass, int depth)
    {
        moveDir.Normalize();
        initialMoveDir.Normalize();

        if (depth >= maxDepth)
        {
            return Vector3.zero;
        }

        if (gravityPass)
        {
            if (ApproximatedCapsuleSweep(pos, moveDir, checkDistance, out RaycastHit capsuleHit, out RaycastHit surfaceHit))
            {
                Vector3 distToSurface = (capsuleHit.distance - skinWidth) * moveDir;
                if (distToSurface.magnitude <= skinWidth)
                {
                    if (Vector3.Angle(surfaceHit.normal, transform.up) <= criticalSlopeAngle)
                    {
                        //Debug.Log(depth + "force grounding");
                        IsGrounded = true;
                        CheckGround(transform.position, out groundHit);
                        SnapToGround();
                        character.EvaluateAndUpdateAllActions();
                        return distToSurface;
                    }
                }
                else if (Vector3.Angle(capsuleHit.normal, transform.up) <= criticalSlopeAngle)
                {
                    //Debug.Log(depth + "will hit ground");
                    return distToSurface;
                }

                //Debug.Log(depth + "sliding along vel");
                Vector3 leftOverMovement = checkDistance * moveDir - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;
                leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, capsuleHit.normal).normalized;

                return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
            }
            else
            {
                return moveDir * checkDistance;
            }
        }
        else
        {
            if (depth == 0)
            {
                moveDir = Vector3.ProjectOnPlane(moveDir, groundHit.normal).normalized; 
            }

            if (checkDistance >= capsuleRadius)
            {
                if (CheckStep(pos, moveDir, checkDistance + capsuleRadius, out float stepHeight, out float stepWidth, out RaycastHit lowerStairHit))
                {
                    //Debug.Log(depth + "Can move up step");
                    return lowerStairHit.distance * moveDir +
                        CollideAndSlide(moveDir, initialMoveDir, lowerStairHit.point + transform.up * (stepHeight + skinWidth),
                        checkDistance - lowerStairHit.distance, gravityPass, depth + 1);
                }
            }

            if (ApproximatedCapsuleSweep(pos, moveDir, checkDistance, out RaycastHit capsuleHit, out RaycastHit surfaceHit))
            {
                Vector3 distToSurface = (capsuleHit.distance - skinWidth) * moveDir;
                if (distToSurface.magnitude <= skinWidth)
                {
                    distToSurface = Vector3.zero;
                }

                Vector3 leftOverMovement = checkDistance * moveDir - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;

                Vector3 flatCapsuleHitDir = Vector3.ProjectOnPlane(-capsuleHit.normal, transform.up).normalized;
                if (CheckStep(pos + distToSurface, flatCapsuleHitDir, capsuleRadius + skinWidth, out float stepHeight, out float stepWidth, out RaycastHit lowerStairHit))
                {
                    //Debug.Log(depth + "move up step after close capsule sweep");
                    return distToSurface +
                        CollideAndSlide(moveDir, initialMoveDir, pos + distToSurface + transform.up * (stepHeight + skinWidth), leftOverMagnitude, gravityPass, depth + 1);
                }
                else if (Vector3.Angle(surfaceHit.normal, transform.up) > criticalSlopeAngle)
                {
                    Vector3 wallNormal = Vector3.ProjectOnPlane(surfaceHit.normal, transform.up).normalized;
                    float wallAngle = Vector3.Angle(Vector3.ProjectOnPlane(moveDir, transform.up), -wallNormal);
                    if (wallAngle <= criticalWallAngle)
                    {
                        //Debug.Log(depth + "will stop against wall");
                        return distToSurface;
                    }
                    else
                    {
                        //Debug.Log(depth + "will move along wall");
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, wallNormal);
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, transform.up);
                        return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                    }
                }
                else
                {
                    //Debug.Log(depth + "will move along ground");
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, capsuleHit.normal);
                    return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                }
            }
            return moveDir * checkDistance;
        }
    }

    public bool CheckGround(Vector3 pos, out RaycastHit groundHit)
    {
        var groundCheckerDepth = GetGroundCheckerDepth();

        if (Physics.Raycast(pos + transform.up * (groundCheckerHeight + skinWidth),
            -transform.up, out groundHit, groundCheckerHeight + skinWidth + groundCheckerDepth, collisionLayer, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(groundHit.normal, transform.up) <= criticalSlopeAngle)
            {
                return true;
            }
        }
        for (int i = 1; i < noOfLayers; i++)
        {
            float angleStep = 360f / sides;
            var radius = groundCheckerRadius * i / (noOfLayers - 1);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
                Vector3 rayStart = pos + transform.up * (groundCheckerHeight + skinWidth) + direction * radius;
                if (Physics.Raycast(rayStart, -transform.up, out groundHit, groundCheckerHeight + skinWidth + groundCheckerDepth, collisionLayer, 
                    QueryTriggerInteraction.Ignore))
                {
                    if (Vector3.Angle(groundHit.normal, transform.up) <= criticalSlopeAngle)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public float GetGroundCheckerDepth()
    {
        if (character.PerformingAction<Idle>() || character.PerformingAction<Walk>() || character.PerformingAction<Run>() || character.PerformingAction<Sprint>()
            || character.PerformingAction<Evade>() || character.PerformingAction<Roll>() || character.PerformingAction<Attack>())
        {
            return maxStepHeight;
        }
        else if (character.PerformingAction<Fall>() || character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            return 0f;
        }
        else
        {
            return maxStepHeight;
        }
    }

    public bool CheckStep(Vector3 pos, Vector3 moveDir, float distance, out float stepHeight, out float stepWidth, out RaycastHit lowerStepHit)
    {
        stepHeight = 0f;
        stepWidth = 0f;

        float upwardsRoom;
        if (ApproximatedCapsuleSweep(pos, transform.up, maxStepHeight, out RaycastHit capsuleHit, out _))
            upwardsRoom = capsuleHit.distance - skinWidth;
        else
            upwardsRoom = maxStepHeight - skinWidth;

        RaycastHit upperStepHit = new RaycastHit();
        if (Physics.Raycast(pos + transform.up * skinWidth, moveDir, out lowerStepHit, distance, collisionLayer, QueryTriggerInteraction.Ignore) &&
            Vector3.Angle(lowerStepHit.normal, transform.up) > criticalSlopeAngle)
        {
            bool allRaysHit = true;
            float seperation = (upwardsRoom + skinWidth) / stepCheckerCount;
            int notHitIndex = -1;
            Vector3 lowerStepNormal = Vector3.ProjectOnPlane(lowerStepHit.normal, transform.up);
            for (int i = 0; i <= stepCheckerCount; i++)
            {
                if (!(Physics.Raycast(lowerStepHit.point + i * seperation * transform.up + lowerStepNormal * skinWidth, -lowerStepNormal, out upperStepHit,
                    distance - lowerStepHit.distance + skinWidth, collisionLayer, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(upperStepHit.normal, transform.up) > criticalSlopeAngle))
                {
                    allRaysHit = false;
                    notHitIndex = i;
                    break;
                }
            }

            if (allRaysHit)
            {
                if (Physics.Raycast(upperStepHit.point + lowerStepNormal * skinWidth, -transform.up, out RaycastHit stepSurfaceHit, maxStepHeight + skinWidth, 
                    collisionLayer, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(stepSurfaceHit.normal, transform.up) <= criticalSlopeAngle)
                {
                    stepHeight = Vector3.Dot(stepSurfaceHit.point - lowerStepHit.point, transform.up);
                    stepWidth = Vector3.Dot(stepSurfaceHit.point - lowerStepHit.point, -lowerStepNormal);
                    float stepAngle = Mathf.Atan(stepHeight / stepWidth) * Mathf.Rad2Deg;
                    if (stepAngle < criticalSlopeAngle && stepWidth > 0f)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Physics.Raycast(lowerStepHit.point + notHitIndex * seperation * transform.up - lowerStepNormal * skinWidth,
                    -transform.up, out RaycastHit stepSurfaceHit, maxStepHeight + skinWidth * 2f, collisionLayer, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(stepSurfaceHit.normal, transform.up) <= criticalSlopeAngle)
                {
                    stepHeight = Vector3.Dot(stepSurfaceHit.point - lowerStepHit.point, transform.up);
                    stepWidth = stepHeight / Mathf.Tan(criticalSlopeAngle * Mathf.Deg2Rad); // Min Required width to be a step
                    if (!Physics.Raycast(stepSurfaceHit.point, -lowerStepNormal, stepWidth - skinWidth, collisionLayer, QueryTriggerInteraction.Ignore))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool ApproximatedCapsuleSweep(Vector3 startPos, Vector3 sweepDir, float sweepDistance, out RaycastHit capsuleHit, out RaycastHit surfaceHit)
    {
        capsuleHit = new();
        capsuleHit.distance = float.MaxValue;
        surfaceHit = new();

        sweepDir.Normalize();

        bool returnValue = false;

        Vector3 localOffset = transform.TransformDirection(capsuleOffset);
        Vector3 capsuleDir = Vector3.zero;
        switch (capsuleOrientation)
        {
            case CapsuleOrientation.X:
                capsuleDir = transform.TransformDirection(Vector3.right);
                break;
            case CapsuleOrientation.Y:
                capsuleDir = transform.TransformDirection(Vector3.up);
                break;
            case CapsuleOrientation.Z:
                capsuleDir = transform.TransformDirection(Vector3.forward);
                break;
        }

        Vector3 point1 = startPos + localOffset + capsuleDir * (capsuleSize / 2f - capsuleRadius);
        Vector3 point2 = startPos + localOffset - capsuleDir * (capsuleSize / 2f - capsuleRadius);
        Vector3 point1to2 = point2 - point1;
        float seperation = point1to2.magnitude / (noOfSpheres - 1f);
        for (int i = 0; i < noOfSpheres; i++)
        {
            Vector3 center = point1 + i * seperation * point1to2.normalized;
            if (Physics.SphereCast(center, capsuleRadius - skinWidth, sweepDir, out RaycastHit sphereHit, sweepDistance,
                collisionLayer, QueryTriggerInteraction.Ignore) &&
                sphereHit.distance < capsuleHit.distance)
            {
                capsuleHit = sphereHit;
                Vector3 centerAtHit = center + sphereHit.distance * sweepDir;
                if (Physics.Raycast(centerAtHit, -sphereHit.normal, out surfaceHit, capsuleRadius, collisionLayer, QueryTriggerInteraction.Ignore))
                {
                    returnValue = true;
                }
            }
        }

        return returnValue;
    }

    public void SnapToGround()
    {
        if (CheckGround(transform.position, out RaycastHit groundHit))
        {
            Vector3 toGround = groundHit.point - transform.position;
            toGround = Vector3.Project(toGround, transform.up);
            transform.position += toGround;
        }
    }

    public void ApplyCapsulePreset(string presetName)
    {
        foreach (var preset in capsulePresets)
        {
            if (preset.presetName == presetName)
            {
                capsuleOrientation = preset.capsuleOrientation;
                capsuleOffset = preset.capsuleOffset;
                capsuleRadius = preset.capsuleRadius;
                capsuleSize = preset.capsuleSize;

                var collider = myCollider as CapsuleCollider;
                switch (preset.capsuleOrientation)
                {
                    case CapsuleOrientation.X: collider.direction = 0; break;
                    case CapsuleOrientation.Y: collider.direction = 1; break;
                    case CapsuleOrientation.Z: collider.direction = 2; break;
                }
                collider.center = preset.capsuleOffset;
                collider.radius = preset.capsuleRadius;
                collider.height = preset.capsuleSize;
            }
        }
    }

    public void SetGravitySimulation(bool value)
    {
        if (value != simulateGravity)
        {
            simulateGravity = value;
            if (value)
            {
                currentPosition = transform.position;
                previousPosition = currentPosition;
            }
            else
            {
                previousPosition = currentPosition;
                currentPosition = transform.position;
            }
        }
    }

    public void SetGravityVector(Vector3 gravity)
    {
        gravityDirection = gravity.normalized;
        gravityMagnitude = gravity.magnitude;
        transform.up = gravityDirection;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void SetFaceDir(Vector3 faceDir)
    {   
        faceDir = Vector3.ProjectOnPlane(faceDir, transform.up).normalized;
        if (faceDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(faceDir, transform.up);
        }
    }

    public void SetWorldVelocity(Vector3 val)
    {
        worldVelocity = val;
    }

    public Vector3 GetWorldVelocity()
    {
        return worldVelocity;
    }

    //private void OnDrawGizmos()
    //{
    //    if (Application.isPlaying)
    //    {
    //        Gizmos.color = Color.black;
    //        Gizmos.DrawSphere(transform.position + transform.up * capsuleOffset.y + transform.forward * sweepDistance, 0.1f);
    //        Gizmos.DrawRay(transform.position + transform.up * capsuleOffset.y, transform.forward * sweepDistance);

    //        Gizmos.color = Color.blue;
    //        if (testGravityPass)
    //        {
    //            Gizmos.DrawWireSphere(transform.position + transform.up * capsuleOffset.y + testWithGravityPass, 0.15f);
    //            Gizmos.DrawRay(transform.position + transform.up * 0.9f, testWithGravityPass);
    //        }
    //        else
    //        {
    //            Gizmos.DrawWireSphere(transform.position + transform.up * capsuleOffset.y + testWithoutGravityPass, 0.15f);
    //            Gizmos.DrawRay(transform.position + transform.up * 0.9f, testWithoutGravityPass);
    //        }
    //    }
    //}
}