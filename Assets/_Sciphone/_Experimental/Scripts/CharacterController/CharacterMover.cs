using System;
using Nomnom.RaycastVisualization;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem.HID;

#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class CharacterMover : MonoBehaviour
{
    enum CapsuleOrientation
    {
        X,
        Y,
        Z
    }

    [HideInInspector] public Character character;

    [Range(0f, 3f)] public float timeScale;

    #region ALGORITH_SETTINGS
    [TabGroup("Algorithm Settings")] public int maxBounces = 3;
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
    [TabGroup("Gravity Settings")] public Vector3 worldVelocity;

    [TabGroup("Gravity Settings")] public Vector3 gravityDirection = Vector3.down;
    [TabGroup("Gravity Settings")] public float gravityMagnitude = 10f;
    [TabGroup("Gravity Settings")] public float terminalVelocity = -50f;
    #endregion

    #region CHECKER_SETTINGS
    [TabGroup("Checker Settings")] public bool isGrounded;
    [TabGroup("Checker Settings")] public RaycastHit groundHit;
    [TabGroup("Checker Settings")] public LayerMask groundLayer;
    [TabGroup("Checker Settings")] public int sides = 8;
    [TabGroup("Checker Settings")] public int noOfLayers = 3;
    [TabGroup("Checker Settings")] public float groundCheckerRadius = 0.3f;
    [TabGroup("Checker Settings")] public float groundCheckerHeight = 1f;

    [TabGroup("Checker Settings")] public RaycastHit stairHit;
    [TabGroup("Checker Settings")] public int stairCheckerCount;
    #endregion

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.CharacterUpdateEvent += OnCharacterUpdate;
    }

    public bool testGravityPass;
    Vector3 testWithGravityPass;
    Vector3 testWithoutGravityPass;
    private void Update()
    {
        if (testGravityPass)
        {
            testWithGravityPass = CollideAndSlide(worldVelocity, worldVelocity, transform.position, sweepDistance, true, 0);
        }
        else
        {
            testWithoutGravityPass = CollideAndSlide(transform.forward, transform.forward, transform.position, sweepDistance, false, 0);
        }
    }

    private void OnCharacterUpdate()
    {
        isGrounded = CheckGround(transform.position, out groundHit);

        if (simulateGravity)
        {
            accumulatedTime += Time.deltaTime * timeScale;
            while (accumulatedTime > simulationStep)
            {
                accumulatedTime -= simulationStep;
                SimulateGravity();
            }
        }
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
            return 0f;
        }
    }

    public bool CheckGround(Vector3 pos, out RaycastHit groundHit)
    {
        var groundCheckerDepth = GetGroundCheckerDepth();

        if (Physics.Raycast(pos + transform.up * groundCheckerHeight,
            -transform.up, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
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
                Vector3 rayStart = pos + transform.up * groundCheckerHeight + direction * radius;
                if (Physics.Raycast(rayStart, -transform.up, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
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

    public bool CheckStairs(Vector3 pos, Vector3 moveDir, out RaycastHit stairHit, float distance)
    {
        float seperation = maxStepHeight / stairCheckerCount;
        for (int i = 0; i < stairCheckerCount + 1; i++)
        {
            if (Physics.Raycast(pos + i * seperation * transform.up, moveDir, out stairHit, distance) &&
                Vector3.Angle(stairHit.normal, transform.up) > criticalSlopeAngle)
            {
                return true;
            }
        }
        stairHit = new RaycastHit();
        return false;
    }

    public bool CapsuleSweep(Vector3 startPos, Vector3 sweepDir, float sweepDistance, out RaycastHit capsuleHit)
    {
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

        return Physics.CapsuleCast(point1, point2, capsuleRadius - skinWidth, sweepDir.normalized, out capsuleHit, sweepDistance);
    }

    public Vector3 CollideAndSlide(Vector3 moveDir, Vector3 initialMoveDir, Vector3 pos, float distance, bool gravityPass, int depth)
    {
        moveDir.Normalize();
        initialMoveDir.Normalize();

        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        if (gravityPass)
        {
            if (CapsuleSweep(pos, moveDir, distance, out RaycastHit hit))
            {
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                Vector3 distToSurface = (hit.distance - skinWidth) * moveDir;
                if (distToSurface.magnitude <= skinWidth)
                {
                    distToSurface = Vector3.zero;
                }
                if (groundAngle <= criticalSlopeAngle)
                {
                    return distToSurface;
                }

                Vector3 leftOverMovement = distance * moveDir - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;
                leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hit.normal).normalized;

                return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
            }
        }
        else
        {
            if (depth == 0 && CheckGround(pos, out RaycastHit groundHit))
            {
                moveDir = Vector3.ProjectOnPlane(moveDir, groundHit.normal);
            }

            if (CheckStairs(pos, Vector3.ProjectOnPlane(moveDir, transform.up), out RaycastHit lowerStairHit, distance + capsuleRadius))
            {
                float upwardsRoom;
                if (CapsuleSweep(pos, transform.up, maxStepHeight, out RaycastHit upHit))
                    upwardsRoom = upHit.distance - skinWidth;
                else
                    upwardsRoom = maxStepHeight;

                if (upwardsRoom > 0f)
                {
                    if (CheckStairs(pos + transform.up * upwardsRoom, Vector3.ProjectOnPlane(moveDir, transform.up),
                        out RaycastHit upperStairHit, distance + capsuleRadius))
                    {
                        float stepWidth = (upperStairHit.distance - lowerStairHit.distance) *
                                Mathf.Cos(Vector3.Angle(-upperStairHit.normal, Vector3.ProjectOnPlane(moveDir, transform.up)) * Mathf.Deg2Rad);
                        if (Physics.Raycast(pos + transform.up * upwardsRoom +
                            upperStairHit.distance * Vector3.ProjectOnPlane(moveDir, transform.up),
                            -transform.up, out RaycastHit stairHeightHit, maxStepHeight))
                        {
                            float stepHeight = upwardsRoom - stairHeightHit.distance;
                            float stepAngle = Mathf.Atan(stepHeight / stepWidth) * Mathf.Rad2Deg;
                            if (stepAngle <= criticalSlopeAngle && stepWidth > 0f)
                            {
                                return upperStairHit.distance * moveDir +
                                    CollideAndSlide(moveDir, initialMoveDir, pos + transform.up * stepHeight + lowerStairHit.distance * moveDir,
                                    distance - upperStairHit.distance, gravityPass, depth + 1);
                            }
                            //Debug.Log($"step height : {stepHeight}, step width : {stepWidth}, step angle : {Mathf.Atan(stepHeight / stepWidth) * Mathf.Rad2Deg}");
                        }
                    }
                    else
                    {
                        if (Physics.Raycast(pos + transform.up * upwardsRoom + Vector3.ProjectOnPlane(moveDir, transform.up) * (lowerStairHit.distance + skinWidth),
                            -transform.up, out RaycastHit stairHeightHit, maxStepHeight))
                        {
                            float stepHeight = upwardsRoom - stairHeightHit.distance;

                            return lowerStairHit.distance * moveDir +
                                CollideAndSlide(moveDir, initialMoveDir, pos + transform.up * stepHeight + lowerStairHit.distance * moveDir,
                                distance - lowerStairHit.distance, gravityPass, depth + 1);
                        }
                    }
                }
            }

            if (CapsuleSweep(pos, moveDir, distance, out RaycastHit capsuleHit))
            {
                Vector3 distToSurface = (capsuleHit.distance - skinWidth) * moveDir;
                if (distToSurface.magnitude <= skinWidth)
                {
                    distToSurface = Vector3.zero;
                }

                Vector3 leftOverMovement = distance * moveDir - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;

                Vector3 flatCapsuleHitDir = Vector3.ProjectOnPlane(-capsuleHit.normal, transform.up).normalized;
                if (CheckStairs(pos + distToSurface, flatCapsuleHitDir, out lowerStairHit, capsuleRadius))
                {
                    float upwardsRoom;
                    if (CapsuleSweep(pos, transform.up, maxStepHeight, out RaycastHit upHit))
                        upwardsRoom = upHit.distance - skinWidth;
                    else
                        upwardsRoom = maxStepHeight;

                    if (upwardsRoom > 0f)
                    {
                        if (CheckStairs(pos + distToSurface + transform.up * upwardsRoom, flatCapsuleHitDir, out RaycastHit upperStairHit, 
                            capsuleRadius + maxStepHeight / Mathf.Tan(criticalSlopeAngle * Mathf.Deg2Rad)))
                        {
                            float stepWidth = upperStairHit.distance - lowerStairHit.distance;
                            if (Physics.Raycast(pos + transform.up * upwardsRoom +
                                upperStairHit.distance * flatCapsuleHitDir,
                                -transform.up, out RaycastHit stairHeightHit, maxStepHeight))
                            {
                                float stepHeight = upwardsRoom - stairHeightHit.distance;   
                                float stepAngle = Mathf.Atan(stepHeight / stepWidth) * Mathf.Rad2Deg;
                                if (stepAngle <= criticalSlopeAngle && stepWidth > 0f)
                                {
                                    return distToSurface +
                                        CollideAndSlide(moveDir, initialMoveDir, pos + transform.up * stepHeight + lowerStairHit.distance * moveDir,
                                        leftOverMagnitude, gravityPass, depth + 1);
                                }
                            }
                        }
                        else
                        {
                            if (Physics.Raycast(pos + distToSurface + transform.up * upwardsRoom + (lowerStairHit.distance + skinWidth) * flatCapsuleHitDir,
                                -transform.up, out RaycastHit stairHeightHit, maxStepHeight))
                            {
                                float stepHeight = upwardsRoom - stairHeightHit.distance;

                                return distToSurface +
                                    CollideAndSlide(moveDir, initialMoveDir, pos + transform.up * stepHeight + lowerStairHit.distance * moveDir,
                                        leftOverMagnitude, gravityPass, depth + 1);
                            }
                        }
                    }
                }

                Physics.Raycast(pos, moveDir, out RaycastHit groundWallHit, distance + capsuleRadius);
                float groundWallAngle = Vector3.Angle(groundWallHit.normal, transform.up);
                Debug.Log($"groundWallAngle : {groundWallAngle}");
                if (groundWallAngle >= criticalSlopeAngle)
                {
                    Vector3 hitNormal = Vector3.ProjectOnPlane(capsuleHit.normal, transform.up).normalized;
                    float wallAngle = Vector3.Angle(Vector3.ProjectOnPlane(moveDir, transform.up), -hitNormal);
                    if (wallAngle <= criticalWallAngle)
                    {
                        return distToSurface;
                    }
                    else
                    {
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hitNormal);
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, transform.up);

                        return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                    }
                }
                else
                {
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, capsuleHit.normal);
                    return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                }
            }
        }

        return moveDir * distance;
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

    public Vector3 ProcessCollideAndSlide(Vector3 moveVector, bool gravityPass)
    {
        Vector3 collideAndSlideVector = CollideAndSlide(moveVector, moveVector, transform.position, sweepDistance, gravityPass, 0);
        if (collideAndSlideVector.magnitude < moveVector.magnitude)
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
        }
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

        worldVelocity = ProcessCollideAndSlide(worldVelocity * simulationStep, true) / simulationStep;
    }

    public void SetGravitySimulation(bool value)
    {
        simulateGravity = value;
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

    public void SetWorldVelocity(Vector3 vel)
    {
        worldVelocity = vel;
    }

    public Vector3 GetWorldVelocity()
    {
        return worldVelocity;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            if (testGravityPass)
            {
                Gizmos.DrawWireSphere(transform.position + transform.up * 0.9f + testWithGravityPass, 0.15f);
                Gizmos.DrawRay(transform.position + transform.up * 0.9f, testWithGravityPass);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position + transform.up * 0.9f + testWithoutGravityPass, 0.15f);
                Gizmos.DrawRay(transform.position + transform.up * 0.9f, testWithoutGravityPass);
            }
        }
    }
}