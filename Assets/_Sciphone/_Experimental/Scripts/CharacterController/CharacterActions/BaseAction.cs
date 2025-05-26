using System.Linq.Expressions;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

[Serializable]
public abstract class BaseAction : CharacterAction
{
    [HideInInspector] public BaseController controller;
}
[Serializable]
public class Idle : BaseAction
{
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                !character.PerformingAction<Walk>() &&
                !character.PerformingAction<Run>() &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Fall>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (!CanPerform)
        {
            IsBeingPerformed = false;
            return;
        }

        if (character.moveInput.sqrMagnitude == 0f)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime);
            controller.HandleGroundMovement();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Walk : BaseAction
{
    public bool forceWalk;
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Walk += OnWalkInput;
    }

    private void OnWalkInput(bool walkPressed, InputDevice device)
    {
        if (device is Keyboard)
        {
            forceWalk = walkPressed;
        }
    }

    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (!CanPerform)
        {
            IsBeingPerformed = false;
            return;
        }
        if (character.moveInput.sqrMagnitude > 0f && !character.PerformingAction<Run>())
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime);
            controller.HandleGroundMovement();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Run : BaseAction
{
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                !((Walk)character.GetAction<Walk>()).forceWalk &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (!CanPerform)
        {
            IsBeingPerformed = false;
            return;
        }
        if (character.moveInput.sqrMagnitude > 0.99f)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime);
            controller.HandleGroundMovement();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Sprint : BaseAction
{
    public bool sprintCached;
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Sprint += OnSprintInput;
        IsBeingPerformed_OnValueChanged += Sprint_IsBeingPerformed_OnValueChanged;
    }

    private void Sprint_IsBeingPerformed_OnValueChanged(bool value)
    {
        character.GetAction<Fall>().EvaluateStatus();

        if (character.PerformingAction<Jump>() || character.PerformingAction<Fall>() ||
            character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
        {
            sprintCached = true;
        }
    }

    private void OnSprintInput(bool sprintPressed, InputDevice device)
    {
        if (!CanPerform || character.moveInput.sqrMagnitude == 0f) return;
        if (device is Gamepad && sprintPressed)
        {
            IsBeingPerformed = true;
        }
        if (device is Keyboard)
        {
            IsBeingPerformed = sprintPressed;
            if (sprintCached && !sprintPressed)
                sprintCached = false;
        }
    }

    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (!CanPerform)
        {
            IsBeingPerformed = false;
            return;
        }
        if (sprintCached)
        {
            IsBeingPerformed = true;
            sprintCached = false;
        }
        if (character.moveInput.sqrMagnitude == 0f)
        {
            IsBeingPerformed = false;
            sprintCached = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime);
            controller.HandleGroundMovement();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Crouch : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Crouch += CrouchPressed;
    }

    private void CrouchPressed()
    {
        if (!CanPerform) return;
        IsBeingPerformed = !IsBeingPerformed;
    }

    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (!CanPerform)
        {
            IsBeingPerformed = false;
            return;
        }
    }
}
[Serializable]
public class Jump : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Jump += OnJumpInput;
    }
    private void OnJumpInput(bool jumpPressed)
    {
        if (!CanPerform) return;
        if (jumpPressed)
        {
            IsBeingPerformed = true;
            controller.InitiateJump();
            controller.CalculateJumpDirection();
        }
    }

    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.isGrounded &&
                baseController.jumpDurationCounter <= 0f);
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.CanPerformAction<ClimbOverLow>() &&
                !character.CanPerformAction<ClimbOverHigh>() &&
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (IsBeingPerformed && character.isGrounded && character.rb.linearVelocity.y < 0f)
        {
            controller.jumpDurationCounter = 0f;
        }

        if (controller.jumpDurationCounter > 0f)
        {
            controller.jumpDurationCounter -= Time.deltaTime;
        }
        else
        {
            controller.jumpDurationCounter = 0f;
        }

        if (controller.jumpDurationCounter > 0f)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.fixedDeltaTime);
            controller.HandleAirMovement(Time.fixedDeltaTime);
        }
    }
}
[Serializable]
public class AirJump : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Jump += OnJumpInput;
    }
    private void OnJumpInput(bool jumpPressed)
    {
        if (!CanPerform) return;
        if (jumpPressed)
        {
            IsBeingPerformed = true;
            controller.InitiateJump();
            controller.CalculateJumpDirection();
        }
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.PerformingAction<Fall>() &&
                baseController.currentAirJumpCount > 0 &&
                baseController.airJumpDurationCounter <= 0f);
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.CanPerformAction<ClimbOverLow>() &&
                !character.CanPerformAction<ClimbOverHigh>() &&
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (character.isGrounded)
        {
            controller.airJumpDurationCounter = 0f;
            controller.currentAirJumpCount = controller.airJumpCount;
        }
        if (controller.airJumpDurationCounter > 0f)
        {
            controller.airJumpDurationCounter -= Time.deltaTime;
        }
        else
        {
            controller.airJumpDurationCounter = 0f;
        }

        if (controller.airJumpDurationCounter > 0f)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.fixedDeltaTime);
            controller.HandleAirMovement(Time.fixedDeltaTime);
        }
    }
}
[Serializable]
public class Fall : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.isGrounded &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>() &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        IsBeingPerformed = CanPerform;
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.CalculateJumpDirection();
            controller.HandleRotation(Time.fixedDeltaTime);
            controller.HandleAirMovement(Time.fixedDeltaTime);
        }
    }
}