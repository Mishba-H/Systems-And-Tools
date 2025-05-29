using System;
using Nomnom.RaycastVisualization;
using Sciphone;
using UnityEngine;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class CharacterMover : MonoBehaviour
{
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
    public float maxStepWidth;

    [SerializeField] private CapsuleOrientation capsuleOrientation = CapsuleOrientation.Y;
    public Vector3 offset;
    public float radius;
    public float size;
    public float skinWidth = 0.015f;

    public float criticalSlopeAngle = 75f;
    public float criticalWallAngle = 30f;


    public MovementMode movementMode = MovementMode.Forward;
    public Vector3 worldMoveDir;
    public Vector3 faceDir;
    public float rotateSpeed;

    public bool isGrounded;
    public RaycastHit groundHit;
    public LayerMask groundLayer;
    public int sides = 8;
    public int noOfLayers = 3;
    public float groundCheckerHeight = 1f;
    public float groundCheckerDepth = 0.3f;

    public Vector3 rootDeltaPosition;
    public Quaternion rootDeltaRotation;
    public Vector3 scaleFactor;

    public enum MovementMode
    {
        Forward,
        EightWay
    }

    enum CapsuleOrientation
    {
        X,
        Y,
        Z
    }

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.animMachine.OnGraphEvaluate += AnimMachine_OnGraphEvaluate;
        character.characterCommand.ChangeMovementModeCommand += CharacterCommand_ChangeMovementModeCommand;
    }

    private void CharacterCommand_ChangeMovementModeCommand(MovementMode obj)
    {
        movementMode = obj;
    }

    private void Update()
    {
        isGrounded = CheckGround(out groundHit);
    }

    private void AnimMachine_OnGraphEvaluate(float dt)
    {
        rootDeltaPosition = character.animMachine.rootDeltaPosition;
        rootDeltaRotation = character.animMachine.rootDeltaRotation;

        HandleMovement(dt);

        if (simulatePhysics)
        {
            accumulatedTime += dt;
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
            return 1f;
        }
        else if (character.PerformingAction<Fall>() || character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            return 0.1f;
        }
        else
        {
            return 0f;
        }
    }

    public bool CheckGround(out RaycastHit groundHit)
    {
        groundCheckerDepth = GetGroundCheckerDepth();

        if (Physics.Raycast(transform.position + transform.up * groundCheckerHeight,
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
            var radius = this.radius * i / (noOfLayers - 1);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
                Vector3 rayStart = transform.position + transform.up * groundCheckerHeight + direction * radius;
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

    public bool CapsuleSweep(Vector3 sweepDir, float sweepDistance, out RaycastHit capsuleHit)
    {
        Vector3 localOffset = transform.TransformDirection(offset);

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

        Vector3 point1 = transform.position + localOffset + capsuleDir * (size / 2f - radius);
        Vector3 point2 = transform.position + localOffset - capsuleDir * (size / 2f - radius);

        using (VisualLifetime.Create(1f))
        {
            if (Physics.CapsuleCast(point1, point2, radius - skinWidth, sweepDir.normalized, out capsuleHit, sweepDistance))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public Vector3 CollideAndSlide(Vector3 moveVector, Vector3 initialMoveVector, Vector3 pos, float distance, bool gravityPass, int depth)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        if (CapsuleSweep(moveVector, distance, out RaycastHit hit))
        {
            Vector3 distToSurface = (hit.distance - skinWidth) * moveVector.normalized;
            if (distToSurface.magnitude <= skinWidth)
            {
                distToSurface = Vector3.zero;
            }

            Vector3 leftOverMovement = distance * moveVector.normalized - distToSurface;
            float leftOverMagnitude = leftOverMovement.magnitude;
            leftOverMovement = Vector3.ProjectOnPlane(leftOverMovement, hit.normal).normalized;

            if (gravityPass)
            {
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                if (groundAngle < criticalSlopeAngle)
                {
                    return distToSurface;
                }
            }
            else if (!gravityPass)
            {
                Vector3 hitNormal = hit.normal;
                float groundAngle = Vector3.Angle(hit.normal, transform.up);
                if (groundAngle >= criticalSlopeAngle)
                {
                    hitNormal = Vector3.ProjectOnPlane(hit.normal, transform.up).normalized;
                }
                float wallAngle = Vector3.Angle(moveVector, -hitNormal);
                if (wallAngle <= criticalWallAngle)
                {
                    return distToSurface;
                }
                else
                {
                    float scale = 1 - Vector3.Dot(hitNormal, initialMoveVector.normalized);
                    leftOverMagnitude *= scale;
                }
            }

            return distToSurface + CollideAndSlide(leftOverMovement, initialMoveVector, pos + distToSurface, leftOverMagnitude, gravityPass, depth + 1);
        }

        return moveVector;
    }

    public void ProcessCollideAndSlide(Vector3 moveVector, bool gravityPass)
    {
        Vector3 collideAndSlideVector = CollideAndSlide(moveVector, moveVector, transform.position, sweepDistance, gravityPass, 0);
        if (collideAndSlideVector.magnitude < moveVector.magnitude)
        {
            transform.position += collideAndSlideVector;
        }
        else
        {
            transform.position += collideAndSlideVector.normalized * moveVector.magnitude;
        }
    }

    public void RotateTowards(Vector3 targetDirection, float dt)
    {
        if (targetDirection == Vector3.zero)
            return;

        targetDirection.Normalize();

        Vector3 currentDirection = transform.forward;

        float angle = Vector3.Angle(currentDirection, targetDirection);

        float maxAngle = rotateSpeed * dt;

        if (angle <= maxAngle)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }
        else
        {
            Vector3 newDirection = Vector3.RotateTowards(currentDirection, targetDirection, maxAngle * Mathf.Deg2Rad, 0f);
            transform.rotation = Quaternion.LookRotation(newDirection);
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

        ProcessCollideAndSlide(worldVelocity * simulationStep, true);
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

    public void SetScaleFactor(Vector3 factor)
    {
        scaleFactor = factor;
    }

    public void SetFaceDir(Vector3 faceDir)
    {
        this.faceDir = faceDir;
    }

    public void SetMoveDir(Vector3 moveDir)
    {
        worldMoveDir = Vector3.ProjectOnPlane(moveDir, transform.up).normalized;
    }

    private void HandleMovement(float dt)
    {
        if (movementMode == MovementMode.Forward)
        {
            Vector3 up = transform.up;
            Vector3 forward = transform.forward;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                rootDeltaPosition.z * scaleFactor.z);

            Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

            RotateTowards(worldMoveDir, dt);
            ProcessCollideAndSlide(worldDeltaPostition, false);
        }
        else if (movementMode == MovementMode.EightWay)
        {
            Vector3 up = transform.up;
            Vector3 forward = worldMoveDir;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                rootDeltaPosition.z * scaleFactor.z);

            Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

            RotateTowards(faceDir, dt);
            ProcessCollideAndSlide(worldDeltaPostition, false);
        }
    }

    private void SnapToGround()
    {
        if (isGrounded)
        {
            transform.position = new Vector3(transform.position.x, groundHit.point.y, transform.position.z);
        }
    }
}
