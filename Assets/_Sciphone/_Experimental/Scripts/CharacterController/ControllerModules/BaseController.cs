using System;
using UnityEngine;
using Sciphone;
using UnityEditor.PackageManager;
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
    [TabGroup("Movement")] public float rotateSpeed = 240;
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
    private Vector3 jumpVelocity;
    private float timeOfFlight;
    #endregion


    List<EightWayBlendState> states;
    public float error = 0.1f;
    public float blendSpeed = 0.5f;

    private void Start()
    {
        character.characterCommand.ChangeMovementModeCommand += CharacterCommand_ChangeMovementModeCommand;
        character.characterCommand.FaceDirCommand += CharacterCommand_FaceDirCommand;
        character.characterCommand.MoveDirCommand += CharacterCommand_MoveDirCommand;

        character.OnAllActionEvaluate += CalculateSpeedFactor;
        character.OnAllActionEvaluate += HandlePhysicsSimulation;
        character.OnAnimationMachineUpdate += Character_OnAnimationMachineUpdate;
        foreach (var action in character.actions)
        {
            if (action is Idle || action is Walk || action is Run || action is Sprint || action is Crouch)
                action.IsBeingPerformed_OnValueChanged += CalculateTargetSpeed;
        }

        states = new List<EightWayBlendState>();
        states.Add(character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Walk") as EightWayBlendState);
        states.Add(character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Run") as EightWayBlendState);
        states.Add(character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("CrouchMove") as EightWayBlendState);
    }

    private void CharacterCommand_FaceDirCommand(Vector3 obj)
    {
        targetForward = obj;
    }

    private void CharacterCommand_MoveDirCommand(Vector3 dir)
    {
        worldMoveDir = Vector3.ProjectOnPlane(dir, transform.up).normalized;
    }

    public void CalculateTargetSpeed(bool value)
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

    private void CalculateSpeedFactor()
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

                scaleFactor = new Vector3(1f, 0f, targetSpeed / (totalZDisp/totalTime));
            }
        }
    }
    
    private void CharacterCommand_ChangeMovementModeCommand(MovementMode obj)
    {
        movementMode = obj;
    }

    private void Character_OnAnimationMachineUpdate(float dt)
    {
        HandleMovement(dt);
        HandleRotation(dt);
        HandleBlendParameters(dt);
    }

    public Vector3 RotateTowards(Vector3 targetDirection, float dt)
    {
        if (targetDirection == Vector3.zero)
            return transform.forward;
        targetDirection.Normalize();

        return Vector3.RotateTowards(transform.forward, targetDirection, rotateSpeed * dt * Mathf.Deg2Rad, 0f);
    }

    private void HandleMovement(float dt)
    {
        if (character.PerformingAction<Idle>() || character.PerformingAction<Walk>() || character.PerformingAction<Run>() ||
            character.PerformingAction<Sprint>() || character.PerformingAction<Crouch>())
        {
            Vector3 moveAmount = Vector3.zero;
            if (movementMode == MovementMode.Forward)
            {
                Vector3 up = transform.up;
                Vector3 forward = transform.forward;
                Vector3 right = Vector3.Cross(up, forward).normalized;

                Vector3 rootDeltaPosition = character.animMachine.rootDeltaPosition;
                Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                    rootDeltaPosition.z * scaleFactor.z);

                Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

                moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPostition, false);
            }
            else if (movementMode == MovementMode.EightWay)
            {
                Vector3 up = transform.up;
                Vector3 forward = worldMoveDir;
                Vector3 right = Vector3.Cross(up, forward).normalized;

                Vector3 rootDeltaPosition = character.animMachine.rootDeltaPosition;
                Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
                    rootDeltaPosition.z * scaleFactor.z);

                Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

                moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPostition, false);
            }
            character.characterMover.SetWorldVelocity(moveAmount / dt);
        }
    }

    private void HandleRotation(float dt)
    {
        if (character.PerformingAction<Idle>() || character.PerformingAction<Walk>() || character.PerformingAction<Run>() || character.PerformingAction<Crouch>())
        {
            if (movementMode == MovementMode.Forward)
            {
                character.characterMover.SetFaceDir(RotateTowards(worldMoveDir, dt));
            }
            else if (movementMode == MovementMode.EightWay)
            {
                character.characterMover.SetFaceDir(RotateTowards(targetForward, dt));
            }
        }
        else if (character.PerformingAction<Sprint>())
        {
            character.characterMover.SetFaceDir(RotateTowards(worldMoveDir, dt));
        }
        else if (character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            character.characterMover.SetFaceDir(jumpVelocity);
        }
        else if (character.PerformingAction<Fall>())
        {
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

    private void HandlePhysicsSimulation()
    {
        if (character.PerformingAction<Fall>() || character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            character.characterMover.SetPhysicsSimulation(true);
        }
        else
        {
            character.characterMover.SetPhysicsSimulation(false);
        }
    }

    private void HandleBlendParameters(float dt)
    {
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);

        if (movementMode == MovementMode.Forward)
        {
            foreach (var state in states)
            {
                state.blendX = 0f;
                state.blendY = 1f;
            }
        }
        else if (movementMode == MovementMode.EightWay)
        {
            foreach (var state in states)
            {
                state.blendX = Mathf.Abs(localMoveDir.x - state.blendX) < error ?
                    localMoveDir.x : Mathf.MoveTowards(state.blendX, localMoveDir.x, blendSpeed * dt);
                state.blendY = Mathf.Abs(localMoveDir.z - state.blendY) < error ?
                    localMoveDir.z : Mathf.MoveTowards(state.blendY, localMoveDir.z, blendSpeed * dt);
            }
        }

    }
}
