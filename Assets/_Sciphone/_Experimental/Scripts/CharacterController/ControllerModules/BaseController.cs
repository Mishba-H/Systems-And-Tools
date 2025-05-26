using System;
using UnityEngine;
using Sciphone;

[Serializable]
public class BaseController : MonoBehaviour, IControllerModule
{
    public Character character {  get; set; }

    #region MOVEMENT_PARAMETERS
    [TabGroup("Movement"), Disable] public float targetSpeed;
    [TabGroup("Movement")] public float baseSpeed = 1f;
    [TabGroup("Movement")] public float walkSpeedMultiplier = 2f;
    [TabGroup("Movement")] public float runSpeedMultiplier = 4f;
    [TabGroup("Movement")] public float sprintSpeedMultiplier = 8f;
    [TabGroup("Movement")] public float crouchSpeedMultiplier = 0.6f;
    [TabGroup("Movement")] public float rotateSpeed = 15f;
    #endregion

    #region JUMP_PARAMETERS
    [TabGroup("Jump")] public float gravityValue = 15f;
    [TabGroup("Jump")] public float terminalVelocity = -50f;
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
    private Vector3 jumpDir;
    #endregion

    private float speedFactor;

    private void Start()
    {
        foreach (var action in character.actions)
        {
            action.IsBeingPerformed_OnValueChanged += CalculateTargetSpeed;
        }

        character.animMachine.OnActiveStateChanged += CalculateSpeedFactor;
    }

    public void CalculateJumpDirection()
    {
        if (character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            jumpDir = new Vector3(jumpVelocity.x, 0f, jumpVelocity.z);
            if (jumpDir == Vector3.zero)
                jumpDir = transform.forward;
        }
        else if (character.PerformingAction<Fall>())
        {
            jumpDir = transform.forward;
        }
    }
    public void HandleRotation(float dt)
    {
        if (character.PerformingAction<Sprint>())
        {
            //Player faces along the move direction
            var targetForward = character.moveDir == Vector3.zero ? transform.forward : character.moveDir;
            targetForward = new Vector3(targetForward.x, 0f, targetForward.z);
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * dt);
        }
        else if (character.PerformingAction<Walk>() || character.PerformingAction<Run>())
        {
            //Player always faces in the direction of the dynamic forward
            var targetForward = new Vector3(character.cameraOrientation.forward.x, 0f, character.cameraOrientation.forward.z);
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * dt);
        }
        else if (character.PerformingAction<Jump>() || character.PerformingAction<AirJump>())
        {
            var targetForward = jumpDir;
            transform.forward = Vector3.Slerp(transform.forward, targetForward, rotateSpeed * dt);
        }
    }
    public void CalculateTargetSpeed(bool _)
    {
        if (character.PerformingAction <Idle>())
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
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty prop))
        {
            var curves = (RootMotionData)prop.Value;
            var totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;
            var totalZDisp = curves.rootTZ.Evaluate(totalTime) - curves.rootTZ.Evaluate(0f);
            speedFactor = targetSpeed / (totalZDisp / totalTime);
        }
    }
    public void SnapToGround()
    {
        transform.position = new Vector3(transform.position.x, character.groundHit.point.y, transform.position.z);
    }
    public void HandleGroundMovement()
    {
        if (speedFactor == 0f)
        {
            character.rb.linearVelocity = Vector3.zero;
            return;
        }
        if (character.moveDir == Vector3.zero)
        {
            character.rb.linearVelocity = Quaternion.LookRotation(transform.forward, Vector3.up) * 
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
        else
        {
            character.rb.linearVelocity = Quaternion.LookRotation(character.moveDir, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
    }
    public void InitiateJump()
    {
        if (character.PerformingAction<AirJump>())
        {
            currentAirJumpCount--;
            CalculateJumpVelocity(airJumpHeight, airJumpDistance);
            airJumpDurationCounter = timeOfFlight * 0.5f;
        }
        else
        {
            CalculateJumpVelocity(jumpHeight, jumpDistance);
            jumpDurationCounter = timeOfFlight * 0.5f;
        }
        character.rb.linearVelocity = jumpVelocity;
    }
    public void CalculateJumpVelocity(float jumpHeight, float jumpDistance)
    {
        float gravityValue = this.gravityValue * (float)Math.Pow(character.timeScale, 2);
        float jumpVelocityY = Mathf.Sqrt(2 * gravityValue * jumpHeight);
        timeOfFlight = gravityValue == 0f ? 0f : 2 * jumpVelocityY / gravityValue;
        float jumpVelocityXZ = timeOfFlight == 0f ? 0f : jumpDistance / timeOfFlight;
        jumpVelocity = new Vector3(character.moveDir.x * jumpVelocityXZ, jumpVelocityY, character.moveDir.z * jumpVelocityXZ);
    }
    public void HandleAirMovement(float dt)
    {
        float gravityValue = this.gravityValue * (float)Math.Pow(character.timeScale, 2);
        if (character.rb.linearVelocity.y > terminalVelocity)
            character.rb.AddForce(gravityValue * dt * Vector3.down, ForceMode.VelocityChange);
        else
            character.rb.linearVelocity = new Vector3(character.rb.linearVelocity.x, character.timeScale * terminalVelocity, character.rb.linearVelocity.z);
    }
}
