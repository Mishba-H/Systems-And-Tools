using System;
using UnityEngine;
using Sciphone;
using System.Collections.Generic;

[Serializable]
public class BaseController : MonoBehaviour, IControllerModule
{
    public enum MovementMode
    {
        Forward,
        EightWay
    }

    public Character character {  get; set; }

    #region MOVEMENT_PARAMETERS
    [TabGroup("Movement")] public MovementMode movementMode = MovementMode.Forward;
    [TabGroup("Movement"), Disable] public float targetSpeed;
    [TabGroup("Movement")] public float baseSpeed = 1f;
    [TabGroup("Movement")] public float walkSpeedMultiplier = 2f;
    [TabGroup("Movement")] public float runSpeedMultiplier = 4f;
    [TabGroup("Movement")] public float sprintSpeedMultiplier = 8f;
    [TabGroup("Movement")] public float crouchSpeedMultiplier = 0.6f;
    [TabGroup("Movement")] public float groundRotateSpeed = 480;
    [TabGroup("Movement")] public Vector3 worldMoveDir;
    [TabGroup("Movement")] public Vector3 targetForward;
    [TabGroup("Movement")] public Vector3 scaleFactor;
    #endregion

    #region JUMP_PARAMETERS
    [TabGroup("Jump")] public float jumpHeight;
    [TabGroup("Jump")] public float jumpDistance;
    [TabGroup("Jump"), Disable] public float jumpDurationCounter;
    [TabGroup("Jump")] public int airJumpCount;
    [TabGroup("Jump")] public float airJumpHeight;
    [TabGroup("Jump")] public float airJumpDistance;
    [TabGroup("Jump"), Disable] public int currentAirJumpCount;
    [TabGroup("Jump"), Disable] public float airJumpDurationCounter;
    [TabGroup("Jump")] public float airRotateSpeed = 1000;
    private Vector3 jumpVelocity;
    private float timeOfFlight;
    #endregion

    List<EightWayBlendState> movementStates;
    public float error = 0.1f;
    public float blendSpeed = 0.5f;

    private float moveRatio;
    private bool recalculateScaleFactor;

    private void Start()
    {
        character.characterCommand.ChangeMovementModeCommand += CharacterCommand_ChangeMovementModeCommand;
        character.characterCommand.FaceDirCommand += CharacterCommand_FaceDirCommand;
        character.characterCommand.MoveDirCommand += CharacterCommand_MoveDirCommand;

        character.UpdateLoop += OnCharacterUpdate;
        foreach (var action in character.actions)
        {
            if (action is Idle || action is Walk || action is Run || action is Sprint || action is Crouch)
            {
                action.IsBeingPerformed_OnValueChanged += (bool value) =>
                {
                    if (value) recalculateScaleFactor = true;
                };
            }
        }

        movementStates = new List<EightWayBlendState>
        {
            character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Walk") as EightWayBlendState,
            character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Run") as EightWayBlendState,
            character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("CrouchMove") as EightWayBlendState
        };
    }

    private void CharacterCommand_ChangeMovementModeCommand(MovementMode obj)
    {
        movementMode = obj;
    }

    private void CharacterCommand_FaceDirCommand(Vector3 obj)
    {
        targetForward = obj;
    }

    private void CharacterCommand_MoveDirCommand(Vector3 dir)
    {
        worldMoveDir = Vector3.ProjectOnPlane(dir, transform.up).normalized;
    }

    private void OnCharacterUpdate()
    {
        HandleAnimationParameters(Time.deltaTime * character.timeScale);
    }

    public void CalculateScaleFactor()
    {
        if (recalculateScaleFactor)
        {
            if (character.PerformingAction<Idle>())
            {
                scaleFactor = new Vector3(1f, 0f, 1f);
            }
            else if (character.PerformingAction<Walk>() || character.PerformingAction<Run>() ||
                character.PerformingAction<Sprint>() || character.PerformingAction<Crouch>())
            {
                if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out var prop))
                {
                    var curves = (RootMotionData)prop.Value;
                    var totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;
                    var totalZDisp = curves.rootTZ.Evaluate(totalTime) - curves.rootTZ.Evaluate(0f);

                    scaleFactor = new Vector3(1f, 0f, targetSpeed / (totalZDisp / totalTime));
                }
            }
            CalculateTargetSpeed();
        }
    }

    public void CalculateTargetSpeed()
    {
        if (character.PerformingAction<Idle>())
        {
            targetSpeed = 0f;
        }
        else if (character.PerformingAction<Walk>())
        {
            targetSpeed = baseSpeed * walkSpeedMultiplier;
        }
        else if (character.PerformingAction<Run>())
        {
            targetSpeed = baseSpeed * runSpeedMultiplier;
        }
        else if (character.PerformingAction<Sprint>())
        {
            targetSpeed = baseSpeed * sprintSpeedMultiplier;
        }

        if (character.PerformingAction<Crouch>())
        {
            targetSpeed *= crouchSpeedMultiplier;
        }
    }

    public void HandleMotion(float dt)
    {
        Vector3 moveAmount = Vector3.zero;
        if (movementMode == MovementMode.Forward || character.PerformingAction<Sprint>())
        {
            Vector3 up = transform.up;
            Vector3 forward = transform.forward;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
            Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                rootDeltaPosition.z * scaleFactor.z);

            Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPostition);
            moveRatio = worldDeltaPostition == Vector3.zero ? 0 : moveAmount.sqrMagnitude / worldDeltaPostition.sqrMagnitude;
        }
        else if (movementMode == MovementMode.EightWay)
        {
            Vector3 up = transform.up;
            Vector3 forward = worldMoveDir;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
            Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                rootDeltaPosition.z * scaleFactor.z);

            Vector3 worldDeltaPosition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);
            moveRatio = worldDeltaPosition == Vector3.zero ? 0 : moveAmount.sqrMagnitude / worldDeltaPosition.sqrMagnitude;
        }
        character.characterMover.SetWorldVelocity(moveAmount / dt);
    }

    public void HandleRotation(float dt)
    {
        if (character.PerformingAction<Idle>() || character.PerformingAction<Walk>() || character.PerformingAction<Run>() ||
            character.PerformingAction<Sprint>() || character.PerformingAction<Crouch>())
        {
            if (movementMode == MovementMode.Forward || character.PerformingAction<Sprint>())
            {
                character.characterMover.SetFaceDir(Vector3.RotateTowards(transform.forward, worldMoveDir, groundRotateSpeed * dt * Mathf.Deg2Rad, 0f));
            }
            else if (movementMode == MovementMode.EightWay)
            {
                character.characterMover.SetFaceDir(Vector3.RotateTowards(transform.forward, targetForward, groundRotateSpeed * dt * Mathf.Deg2Rad, 0f));
            }
        }
        else if (character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            character.characterMover.SetFaceDir(jumpVelocity);
        }
        else if (character.PerformingAction<Fall>())
        {
            character.characterMover.SetFaceDir(Vector3.RotateTowards(transform.forward, character.characterMover.GetWorldVelocity(), airRotateSpeed * dt * Mathf.Deg2Rad, 0f));
        }
    }
    public void HandlePhysicsSimulation()
    {
        if (character.PerformingAction<Fall>() || character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            character.characterMover.SetGravitySimulation(true);
        }
        else
        {
            character.characterMover.SetGravitySimulation(false);
        }
    }

    public void InitiateJump()
    {
        if (character.PerformingAction<Jump>())
        {
            CalculateJumpVelocity(jumpHeight, jumpDistance);
            jumpDurationCounter = timeOfFlight * 0.5f;
        }
        else if (character.PerformingAction<AirJump>())
        {
            currentAirJumpCount--;
            CalculateJumpVelocity(airJumpHeight, airJumpDistance);
            airJumpDurationCounter = timeOfFlight * 0.5f;
        }
        character.characterMover.SetWorldVelocity(jumpVelocity);
    }

    public void CalculateJumpVelocity(float jumpHeight, float jumpDistance)
    {
        float gravityValue = character.characterMover.gravityMagnitude;
        float jumpVelocityY = Mathf.Sqrt(2 * gravityValue * jumpHeight);
        timeOfFlight = 2 * jumpVelocityY / gravityValue;
        float jumpVelocityXZ = jumpDistance / timeOfFlight;
        Vector3 localJumpVelocity = new Vector3(0f, jumpVelocityY, jumpVelocityXZ);

        Vector3 up = transform.up;
        Vector3 forward = worldMoveDir;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        jumpVelocity = right * localJumpVelocity.x + up * localJumpVelocity.y + forward * localJumpVelocity.z;
    }

    private void HandleAnimationParameters(float dt)
    {
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir) * moveRatio;

        if (movementMode == MovementMode.Forward)
        {
            foreach (var state in movementStates)
            {
                state.blendX = 0f;
                state.blendY = Mathf.MoveTowards(state.blendY, moveRatio, blendSpeed * dt);
            }
        }
        else if (movementMode == MovementMode.EightWay)
        {
            foreach (var state in movementStates)
            {
                state.blendX = Mathf.Abs(localMoveDir.x - state.blendX) < error ?
                    localMoveDir.x : Mathf.MoveTowards(state.blendX, localMoveDir.x, blendSpeed * dt);
                state.blendY = Mathf.Abs(localMoveDir.z - state.blendY) < error ?
                    localMoveDir.z : Mathf.MoveTowards(state.blendY, localMoveDir.z, blendSpeed * dt);
            }
        }
    }
}
