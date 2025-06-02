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
    [SerializeField] private CapsuleOrientation capsuleOrientation = CapsuleOrientation.Y;
    public float skinWidth = 0.015f;
    public Vector3 capsuleOffset;
    public float capsuleRadius;
    public float capsuleSize;

    public int maxBounces = 3;
    public float sweepDistance = 1.5f;

    public float criticalSlopeAngle = 75f;
    public float criticalWallAngle = 30f;

    public float maxStepHeight;
    [Disable] public float minStepWidth;
    #endregion

    #region GRAVITY_SETTINGS    
    public bool simulateGravity;
    public float simulationStep = 1 / 60f;
    public float accumulatedTime;
    public Vector3 worldVelocity;

    public Vector3 gravityDirection = Vector3.down;
    public float gravityMagnitude = 10f;
    public float terminalVelocity = -50f;
    #endregion

    #region CHECKER_SETTINGS
    public bool isGrounded;
    public RaycastHit groundHit;
    public LayerMask groundLayer;
    public int sides = 8;
    public int noOfLayers = 3;
    public float groundCheckerRadius = 0.3f;
    public float groundCheckerHeight = 1f;

    public RaycastHit stairHit;
    public int stairCheckerCount;
    #endregion

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.CharacterUpdateEvent += OnCharacterUpdate;
        minStepWidth = maxStepHeight / MathF.Tan(criticalSlopeAngle * MathF.PI / 180);
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
                    if (groundAngle <= criticalSlopeAngle && transform.InverseTransformDirection(worldVelocity).y < 0)
                    {
                        isGrounded = true;
                        CheckGround(transform.position, out groundHit);
                    }
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
            // Sweep along move vector and find collisions
            if (CapsuleSweep(pos, moveDir, distance, out RaycastHit hit))
            {
                Vector3 distToSurface = (hit.distance - skinWidth) * moveDir;
                if (distToSurface.magnitude <= skinWidth)
                {
                    distToSurface = Vector3.zero;
                }

                Vector3 leftOverMovement = distance * moveDir - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;

                Vector3 hitNormal = hit.normal;
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                if (groundAngle >= criticalSlopeAngle)
                {
                    hitNormal = Vector3.ProjectOnPlane(hit.normal, transform.up).normalized;

                    if (!CapsuleSweep(pos + distToSurface, transform.up, maxStepHeight, out _))
                    {
                        // Sweep Forward and check for collisions
                        if (!CapsuleSweep(pos + distToSurface + transform.up * maxStepHeight, moveDir, leftOverMagnitude, out _))
                        {
                            Physics.Raycast(pos + distToSurface + transform.up * maxStepHeight + moveDir * minStepWidth,
                                -transform.up, out RaycastHit rayHit, maxStepHeight);

                            return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir,
                                pos + distToSurface + transform.up * (maxStepHeight - rayHit.distance),
                                leftOverMagnitude, gravityPass, depth + 1);
                        }
                        else if (CapsuleSweep(pos + distToSurface + transform.up * maxStepHeight, moveDir, leftOverMagnitude, out RaycastHit stepHit) &&
                            (stepHit.distance - skinWidth) * Mathf.Cos(Vector3.Angle(moveDir, -hitNormal) * Mathf.Deg2Rad) > minStepWidth)
                        {
                            Physics.Raycast(pos + distToSurface + transform.up * maxStepHeight + moveDir * (stepHit.distance - skinWidth),
                                -transform.up, out RaycastHit rayHit, maxStepHeight);

                            return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir,
                                pos + distToSurface + transform.up * (maxStepHeight - rayHit.distance),
                                leftOverMagnitude, gravityPass, depth + 1);
                        }
                    }

                    // Treat ground as a vertical wall
                    float wallAngle = Vector3.Angle(Vector3.ProjectOnPlane(moveDir, transform.up), -hitNormal);
                    if (wallAngle <= criticalWallAngle)
                    {
                        // Dont slide along wall surface
                        return distToSurface;
                    }
                    else
                    {
                        // Slide along the wall surface
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, transform.up);
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hitNormal).normalized;

                        float scale = 1 - Vector3.Dot(hitNormal, initialMoveDir.normalized);
                        leftOverMagnitude *= scale;

                        return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                    }
                }
                else
                {
                    // Check for movement along slopes
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hitNormal).normalized;
                    float scale = 1 - Vector3.Dot(hitNormal, initialMoveDir.normalized);
                    leftOverMagnitude *= scale;

                    return distToSurface + CollideAndSlide(leftOverMovement, initialMoveDir, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                }
            }
            // No collisions found along move vector
            else
            {
                // Check for movement along slopes
                if (depth == 0 && isGrounded && Vector3.Angle(groundHit.normal, transform.up) <= criticalSlopeAngle)
                {
                    Vector3.ProjectOnPlane(moveDir, -groundHit.normal);
                    return CollideAndSlide(moveDir, initialMoveDir, pos, distance, gravityPass, depth + 1);
                }

                // Check for movement along stairs going down 
                // cannot be done because the capsule dimensions
                // will interfere with the slope settings
            }
        }

        return moveDir * distance;
    }

    public void SnapToGround()
    {
        Vector3 toGround = groundHit.point - transform.position;
        toGround = Vector3.Project(toGround, transform.up);
        transform.position += toGround;

        /*Vector3 snapPoint = Vector3.zero;
        if (Physics.Raycast(transform.position + transform.up * groundCheckerHeight + worldVelocity.normalized * GetSnappingRadius(),
            -transform.up, out RaycastHit snapHit, groundCheckerHeight + groundCheckerDepth) &&
            Vector3.Angle(snapHit.normal, transform.up) <= criticalSlopeAngle)
        {
            snapPoint = snapHit.point;
        }
        else
        {
            snapPoint = groundHit.point;
        }

        Vector3 toGround = snapPoint - transform.position;
        toGround = Vector3.Project(toGround, transform.up);
        transform.position += toGround;*/
    }

    /*public float GetSnappingRadius()
    {
        if (character.PerformingAction<Idle>())
        {
            return 0f;
        }
        else if (character.PerformingAction<Walk>() || character.PerformingAction<Run>() || character.PerformingAction<Sprint>()
            || character.PerformingAction<Evade>() || character.PerformingAction<Roll>() || character.PerformingAction<Attack>())
        {
            return minStepWidth;
        }
        else if (character.PerformingAction<Fall>() || character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            return 0f;
        }
        else
        {
            return 0f;
        }
    }*/

    public Vector3 ProcessCollideAndSlide(Vector3 moveVector, bool gravityPass)
    {
        Vector3 collideAndSlideVector = CollideAndSlide(moveVector, moveVector, transform.position, sweepDistance, gravityPass, 0);
        if (collideAndSlideVector.magnitude < moveVector.magnitude)
        {
            transform.position += collideAndSlideVector;
            return collideAndSlideVector;
        }
        else
        {
            transform.position += collideAndSlideVector.normalized * moveVector.magnitude;
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
            //Gizmos.color = Color.black;
            //Gizmos.DrawWireSphere(transform.position + transform.up * 0.9f + worldVelocity, 0.15f);
            //Gizmos.DrawRay(transform.position + transform.up * 0.9f, worldVelocity);

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


// NOTE : Collide And Slide Algorithm
// capsule sweep cannot detect stairs properly under all situations so it has to be discarded
// instead use parallel rays which go vertically up from character base to max step height with adjustable interval to detect stairs