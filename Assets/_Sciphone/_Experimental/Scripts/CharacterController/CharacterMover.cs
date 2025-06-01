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

    public int maxBounces = 3;
    public float sweepDistance = 1.5f;

    public bool simulatePhysics;
    public float simulationStep = 1/60f;
    public float accumulatedTime;
    public Vector3 worldVelocity;

    public Vector3 gravityDirection = Vector3.down;
    public float gravityMagnitude = 10f;
    public float terminalVelocity = -50f;

    public float maxStepHeight;
    public float minStepWidth;

    [SerializeField] private CapsuleOrientation capsuleOrientation = CapsuleOrientation.Y;
    public float skinWidth = 0.015f;
    public Vector3 capsuleOffset;
    public float capsuleRadius;
    public float capsuleSize;

    public float criticalSlopeAngle = 75f;
    public float criticalWallAngle = 30f;

    public bool isGrounded;
    public RaycastHit groundHit;
    public LayerMask groundLayer;
    public int sides = 8;
    public int noOfLayers = 3;
    public float groundCheckerHeight = 1f;
    public float groundCheckerDepth = 0.3f;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.OnAllActionEvaluate += Character_OnAllActionEvaluate;
        minStepWidth = maxStepHeight / MathF.Tan(criticalSlopeAngle * MathF.PI / 180);
    }

    private void Character_OnAllActionEvaluate()
    {
        isGrounded = CheckGround(transform.position, out groundHit);
        if (simulatePhysics)
        {
            accumulatedTime += Time.deltaTime * timeScale;
            while (accumulatedTime > simulationStep)
            {
                accumulatedTime -= simulationStep;
                SimulatePhysics();
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
        groundCheckerDepth = GetGroundCheckerDepth();

        if (Physics.Raycast(pos + transform.up * groundCheckerHeight,
            -transform.up, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
        {
            if (Vector3.Angle(groundHit.normal, transform.up) > criticalSlopeAngle)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        for (int i = 1; i < noOfLayers; i++)
        {
            float angleStep = 360f / sides;
            var radius = (capsuleRadius - skinWidth) * i / (noOfLayers - 1);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
                Vector3 rayStart = pos + transform.up * groundCheckerHeight + direction * radius;
                if (Physics.Raycast(rayStart, -transform.up, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
                {
                    if (Vector3.Angle(groundHit.normal, transform.up) > criticalSlopeAngle)
                    {
                        return false;
                    }
                    else
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

        if (Physics.CapsuleCast(point1, point2, capsuleRadius - skinWidth, sweepDir.normalized, out capsuleHit, sweepDistance))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector3 CollideAndSlide(Vector3 moveVector, Vector3 initialMoveVector, Vector3 pos, float distance, bool gravityPass, int depth)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        if (gravityPass)
        {
            if (CapsuleSweep(pos, moveVector, distance, out RaycastHit hit))
            {
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                Vector3 distToSurface = (hit.distance - skinWidth) * moveVector.normalized;
                if (distToSurface.magnitude <= skinWidth)
                {
                    if (groundAngle < criticalSlopeAngle)
                    {
                        isGrounded = true;
                    }
                    distToSurface = Vector3.zero;
                }

                Vector3 leftOverMovement = distance * moveVector.normalized - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;
                leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hit.normal).normalized;
                if (groundAngle < criticalSlopeAngle)
                {
                    return distToSurface;
                }

                return distToSurface + CollideAndSlide(leftOverMovement, initialMoveVector, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
            }
        }
        else
        {
            // Sweep along move vector and find collisions
            if (CapsuleSweep(pos, moveVector, distance, out RaycastHit hit))
            {
                Vector3 distToSurface = (hit.distance - skinWidth) * moveVector.normalized;
                if (distToSurface.magnitude <= skinWidth)
                {
                    distToSurface = Vector3.zero;
                }

                Vector3 leftOverMovement = distance * moveVector.normalized - distToSurface;
                float leftOverMagnitude = leftOverMovement.magnitude;

                Vector3 hitNormal = hit.normal;
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                if (groundAngle >= criticalSlopeAngle)
                {
                    hitNormal = Vector3.ProjectOnPlane(hit.normal, transform.up).normalized;

                    // Check for movement along stairs going up
                    // Sweep Up and check for collisions
                    if (!CapsuleSweep(pos + distToSurface, transform.up, maxStepHeight, out _))
                    {
                        // Sweep Forward and check for collisions
                        if (!CapsuleSweep(pos + distToSurface + transform.up * maxStepHeight, -hitNormal, leftOverMagnitude, out RaycastHit stepHit))
                        {
                            // Move up if no collisions
                            return distToSurface + transform.up * maxStepHeight;
                        }
                        else if (stepHit.distance - skinWidth > minStepWidth)
                        {
                            Physics.Raycast(pos + distToSurface + transform.up * maxStepHeight + moveVector * (stepHit.distance - skinWidth),
                                -transform.up, out RaycastHit rayHit, maxStepHeight);
                            return distToSurface + transform.up * (maxStepHeight - rayHit.distance);
                        }
                        /*if (!CapsuleSweep(pos + distToSurface + transform.up * maxStepHeight, -hitNormal, leftOverMagnitude, out RaycastHit stepHit) ||
                            stepHit.distance - skinWidth > minStepWidth)
                        {
                            return distToSurface + transform.up * maxStepHeight + CollideAndSlide(moveVector, initialMoveVector,
                                pos + distToSurface + transform.up * maxStepHeight, leftOverMagnitude, gravityPass, depth + 1);
                        }*/
                    }

                    // Treat ground as a vertical wall
                    float wallAngle = Vector3.Angle(moveVector, -hitNormal);
                    if (wallAngle <= criticalWallAngle)
                    {
                        return distToSurface;
                    }
                    else
                    {
                        // Slide along the wall surface
                        leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hitNormal).normalized;

                        float scale = 1 - Vector3.Dot(hitNormal, initialMoveVector.normalized);
                        leftOverMagnitude *= scale;

                        return distToSurface + CollideAndSlide(leftOverMovement, initialMoveVector, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                    }
                }
                else
                {
                    // Check for movement along slopes going up
                    leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hitNormal).normalized;

                    float scale = 1 - Vector3.Dot(hitNormal, initialMoveVector.normalized);
                    leftOverMagnitude *= scale;

                    return distToSurface + CollideAndSlide(leftOverMovement, initialMoveVector, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
                }

            }
            // No collisions found along move vector
            else
            {
                // Check for movement along slopes going down


                // Check for movement along stairs going down
                if (CapsuleSweep(pos + moveVector.normalized * minStepWidth,
                    -transform.up, sweepDistance, out RaycastHit downHit) &&
                    downHit.distance <= maxStepHeight)
                {
                    return minStepWidth * moveVector.normalized + (downHit.distance - skinWidth) * -transform.up + CollideAndSlide(moveVector, moveVector,
                        pos + minStepWidth * moveVector - transform.up * (downHit.distance - skinWidth),
                        distance - minStepWidth, gravityPass, depth + 1);
                }
            }
        }

        return moveVector.normalized * distance;
    }

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

    private void SimulatePhysics()
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

    public void SetPhysicsSimulation(bool value)
    {
        simulatePhysics = value;
    }

    public void SetGravity(Vector3 gravity)
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
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position + transform.up * 1f + worldVelocity, 0.25f);
            Gizmos.DrawRay(transform.position + transform.up * 1f, worldVelocity);
        }
    }
}
