using System;
using Sciphone;
using UnityEngine;

#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class CharacterMover : MonoBehaviour
{
    public event Action<bool> OnIsGroundedValueChanged;

    enum CapsuleOrientation
    {
        X,
        Y,
        Z
    }

    [HideInInspector] public Character character;

    #region ALGORITH_SETTINGS
    [TabGroup("Algorithm Settings")] public int maxDepth = 3;
    [TabGroup("Algorithm Settings")] public int noOfSpheres = 5;
    [TabGroup("Algorithm Settings")] public float sweepDistance = 1.5f;

    [TabGroup("Algorithm Settings")][SerializeField] private CapsuleOrientation capsuleOrientation = CapsuleOrientation.Y;
    [TabGroup("Algorithm Settings")] public float skinWidth = 0.015f;
    [TabGroup("Algorithm Settings")] public Vector3 capsuleOffset;
    [TabGroup("Algorithm Settings")] public float capsuleRadius;
    [TabGroup("Algorithm Settings")] public float capsuleSize;

    [TabGroup("Algorithm Settings")] public float criticalSlopeAngle = 75f;
    [TabGroup("Algorithm Settings")] public float criticalWallAngle = 30f;

    [TabGroup("Algorithm Settings")] public float maxStepHeight;
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

    [TabGroup("Checker Settings")] public bool isGrounded;
    [TabGroup("Checker Settings")] public RaycastHit groundHit;
    [TabGroup("Checker Settings")] public LayerMask groundLayer;
    [TabGroup("Checker Settings")] public int sides = 8;
    [TabGroup("Checker Settings")] public int noOfLayers = 3;
    [TabGroup("Checker Settings")] public float groundCheckerRadius = 0.3f;

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

    public Vector3 worldVelocity;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.DetectionLoop += Character_DetectionLoop;
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

    private void Character_DetectionLoop()
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




        /*if (collideAndSlideVector.magnitude < moveVector.magnitude)
        {
            transform.position += collideAndSlideVector;
            if (!gravityPass)
            {
                SnapToGround();
            }
            return collideAndSlideVector;
        }
        else
        {
            transform.position += collideAndSlideVector.normalized * moveVector.magnitude;
            if (!gravityPass)
            {
                SnapToGround();
            }
            return collideAndSlideVector.normalized * moveVector.magnitude;
        }*/
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
            if (depth == 0 && CheckGround(pos, out RaycastHit groundHit))
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

            if (Physics.Raycast(pos + transform.up * skinWidth, moveDir, out RaycastHit lowerStepHit, 
                checkDistance + skinWidth, groundLayer, QueryTriggerInteraction.Ignore) &&
                Vector3.Angle(lowerStepHit.normal, transform.up) > criticalSlopeAngle &&
                Vector3.ProjectOnPlane(lowerStepHit.distance * moveDir, transform.up).magnitude <= capsuleRadius)
            {
                Vector3 wallNormal = Vector3.ProjectOnPlane(lowerStepHit.normal, transform.up).normalized;
                float wallAngle = Vector3.Angle(Vector3.ProjectOnPlane(moveDir, transform.up), -wallNormal);
                if (wallAngle <= criticalWallAngle)
                {
                    //Debug.Log(depth + "will stop against wall");
                    return lowerStepHit.normal;
                }
                else
                {
                    //Debug.Log(depth + "will move along wall");
                    Vector3 leftOverMovement = checkDistance * moveDir;
                    float leftOverMagnitude = leftOverMovement.magnitude;
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, wallNormal);
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, transform.up);
                    return lowerStepHit.normal + CollideAndSlide(leftOverMovement, initialMoveDir, pos, leftOverMagnitude, gravityPass, depth + 1);
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

        if (Physics.Raycast(pos + transform.up * (maxStepHeight + skinWidth),
            -transform.up, out groundHit, maxStepHeight + skinWidth + groundCheckerDepth, groundLayer, QueryTriggerInteraction.Ignore))
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
                Vector3 rayStart = pos + transform.up * (maxStepHeight + skinWidth) + direction * radius;
                if (Physics.Raycast(rayStart, -transform.up, out groundHit, maxStepHeight + skinWidth + groundCheckerDepth, groundLayer, QueryTriggerInteraction.Ignore))
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
        if (Physics.Raycast(pos + transform.up * skinWidth, moveDir, out lowerStepHit, distance, groundLayer, QueryTriggerInteraction.Ignore) &&
            Vector3.Angle(lowerStepHit.normal, transform.up) > criticalSlopeAngle)
        {
            bool allRaysHit = true;
            float seperation = (upwardsRoom + skinWidth) / stepCheckerCount;
            int notHitIndex = -1;
            Vector3 lowerStepNormal = Vector3.ProjectOnPlane(lowerStepHit.normal, transform.up);
            for (int i = 0; i <= stepCheckerCount; i++)
            {
                if (!(Physics.Raycast(lowerStepHit.point + i * seperation * transform.up + lowerStepNormal * skinWidth, -lowerStepNormal, out upperStepHit,
                    distance - lowerStepHit.distance + skinWidth, groundLayer, QueryTriggerInteraction.Ignore) &&
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
                    groundLayer, QueryTriggerInteraction.Ignore) &&
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
                    -transform.up, out RaycastHit stepSurfaceHit, maxStepHeight + skinWidth * 2f, groundLayer, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(stepSurfaceHit.normal, transform.up) <= criticalSlopeAngle)
                {
                    stepHeight = Vector3.Dot(stepSurfaceHit.point - lowerStepHit.point, transform.up);
                    stepWidth = stepHeight / Mathf.Tan(criticalSlopeAngle * Mathf.Deg2Rad); // Min Required width to be a step
                    if (!Physics.Raycast(stepSurfaceHit.point, -lowerStepNormal, stepWidth - skinWidth, groundLayer, QueryTriggerInteraction.Ignore))
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
                groundLayer, QueryTriggerInteraction.Ignore) &&
                sphereHit.distance < capsuleHit.distance)
            {
                capsuleHit = sphereHit;
                Vector3 centerAtHit = center + sphereHit.distance * sweepDir;
                if (Physics.Raycast(centerAtHit, -sphereHit.normal, out surfaceHit, capsuleRadius, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    returnValue = true;
                }
            }
        }

        return returnValue;
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

    public void SnapToGround()
    {
        if (CheckGround(transform.position, out RaycastHit groundHit))
        {
            Vector3 toGround = groundHit.point - transform.position;
            toGround = Vector3.Project(toGround, transform.up);
            transform.position += toGround;
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

    public void SetFaceDir(Vector3 faceDir)
    {
        faceDir = Vector3.ProjectOnPlane(faceDir, transform.up).normalized;
        if (faceDir != Vector3.zero)
        {
            transform.forward = faceDir;
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