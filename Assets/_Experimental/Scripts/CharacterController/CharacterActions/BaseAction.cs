using System.Linq.Expressions;
using System;
using UnityEngine;

[Serializable]
public abstract class BaseAction : CharacterAction
{
    [HideInInspector] public BaseController controller;
}

[Serializable]
public class Idle : BaseAction
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
                !character.PerformingAction<Walk>() &&
                !character.PerformingAction<Run>() &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.CalculateScaleFactor();
            controller.HandleMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchIdle", "Base");
            character.characterMover.ApplyCapsulePreset("Short");
        }
        else
        {
            character.characterAnimator.ChangeAnimationState("Idle", "Base");
            character.characterMover.ApplyCapsulePreset("Large");
        }
    }
}

[Serializable]
public class Walk : BaseAction
{
    private bool walk;

    private void CharacterCommand_WalkCommand(bool obj)
    {
        walk = obj;
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.WalkCommand += CharacterCommand_WalkCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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
        if (walk)
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
            controller.CalculateScaleFactor();
            controller.HandleMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchMoveSlow", "Base");
            character.characterMover.ApplyCapsulePreset("Short");
        }
        else
        {
            character.characterAnimator.ChangeAnimationState("Walk", "Base");
            character.characterMover.ApplyCapsulePreset("Small");
        }
    }
}

[Serializable]
public class Run : BaseAction
{
    private bool run;

    private void CharacterCommand_RunCommand(bool obj)
    {
        run = obj;
    }
    public override void OnEnable()
    {
        base.OnEnable();

        character.characterCommand.RunCommand += CharacterCommand_RunCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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
        if (run)
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
            controller.CalculateScaleFactor();
            controller.HandleMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchMoveFast", "Base");
            character.characterMover.ApplyCapsulePreset("Short");
        }
        else
        {
            character.characterAnimator.ChangeAnimationState("Run", "Base");
            character.characterMover.ApplyCapsulePreset("Medium");
        }
    }
}

[Serializable]
public class Sprint : BaseAction
{
    private bool sprint;

    private void CharacterCommand_SprintCommand(bool obj)
    {
        sprint = obj;
    }
    public override void OnEnable()
    {
        base.OnEnable();

        character.characterCommand.SprintCommand += CharacterCommand_SprintCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.IsGrounded &&
                (!character.PerformingAction<Crouch>() || baseController.canEndCrouch) &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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
        if (sprint)
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
            controller.CalculateScaleFactor();
            controller.HandleMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (controller.movementMode == BaseController.MovementMode.EightWay)
            character.characterMover.SetFaceDir(controller.worldMoveDir);

        character.characterAnimator.ChangeAnimationState("Sprint", "Base");
        character.characterMover.ApplyCapsulePreset("Large");
    }
}

[Serializable]
public class Crouch : BaseAction
{
    private void CharacterCommand_CrouchCommand(bool crouch)
    {
        if (CanPerform && crouch)
        {
            IsBeingPerformed = true;
        }
        if (IsBeingPerformed && !crouch && controller.canEndCrouch)
        {
            IsBeingPerformed = false;
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.CrouchCommand += CharacterCommand_CrouchCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.IsGrounded &&
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Idle>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchIdle", "Base");
        }
        else if (character.PerformingAction<Walk>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchMoveSlow", "Base");
        }
        else if (character.PerformingAction<Run>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchMoveFast", "Base");
        }
        character.characterMover.ApplyCapsulePreset("Short");
    }
    public override void OnStop()
    {
        if (character.PerformingAction<Idle>())
        {
            character.characterAnimator.ChangeAnimationState("Idle", "Base");
            character.characterMover.ApplyCapsulePreset("Large");
        }
        else if (character.PerformingAction<Walk>())
        {
            character.characterAnimator.ChangeAnimationState("Walk", "Base");
            character.characterMover.ApplyCapsulePreset("Small");
        }
        else if (character.PerformingAction<Run>())
        {
            character.characterAnimator.ChangeAnimationState("Run", "Base");
            character.characterMover.ApplyCapsulePreset("Medium");
        }
    }
}

[Serializable]
public class Jump : BaseAction
{
    private void OnJumpCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.JumpCommand += OnJumpCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.IsGrounded &&
                (!character.PerformingAction<Crouch>() || baseController.canEndCrouch) &&
                baseController.jumpDurationCounter <= 0f);
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
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

        if (IsBeingPerformed && character.characterMover.IsGrounded &&
            character.transform.InverseTransformDirection(character.characterMover.GetWorldVelocity()).y <= 0f)
        {
            controller.jumpDurationCounter = 0f;
        }

        if (controller.jumpDurationCounter > 0f)
        {
            controller.jumpDurationCounter -= Time.deltaTime * character.timeScale;
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
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        controller.HandlePhysicsSimulation();
        controller.InitiateJump();
        character.characterMover.IsGrounded = false;
        character.EvaluateAndUpdateAllActions();

        character.characterAnimator.ChangeAnimationState("Jump", "Base");
        character.characterMover.ApplyCapsulePreset("Medium");
    }
    public override void OnStop()
    {
        controller.HandlePhysicsSimulation();
        if (character.TryGetAction<Fall>(out var fallAction))
        {
            fallAction.EvaluateStatus();
        }
    }
}

[Serializable]
public class AirJump : BaseAction
{
    private void OnJumpCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.JumpCommand += OnJumpCommand;
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
                !character.PerformingAction<ParkourAction>());
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

        if (character.characterMover.IsGrounded)
        {
            controller.airJumpDurationCounter = 0f;
            controller.currentAirJumpCount = controller.airJumpCount;
        }
        if (controller.airJumpDurationCounter > 0f)
        {
            controller.airJumpDurationCounter -= Time.deltaTime * character.timeScale;
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
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        controller.HandlePhysicsSimulation();
        controller.InitiateJump();
        character.characterAnimator.ChangeAnimationState("AirJump", "Base");
        character.characterMover.ApplyCapsulePreset("Medium");
    }
    public override void OnStop()
    {
        controller.HandlePhysicsSimulation();

        if (character.TryGetAction<Fall>(out var fallAction))
        {
            fallAction.EvaluateStatus();
        }
    }
}

[Serializable]
public class Fall : BaseAction
{
    private void CharacterMover_OnIsGroundedValueChanged(bool obj)
    {
        if (obj && CanPerform)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        character.characterMover.OnIsGroundedValueChanged += CharacterMover_OnIsGroundedValueChanged;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                true);
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

        IsBeingPerformed = !character.characterMover.IsGrounded;
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        controller.HandlePhysicsSimulation();
        if (character.characterAnimator.currentStateName == "AirJump" || character.characterAnimator.currentStateName == "Jump")
        {
            return;
        }
        else
        {
            character.characterAnimator.ChangeAnimationState("Fall", "Base");
        }
        character.characterMover.ApplyCapsulePreset("Medium");
    }
    public override void OnStop()
    {
        controller.HandlePhysicsSimulation();
    }
}