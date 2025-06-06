using System.Linq.Expressions;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
                character.characterMover.IsGrounded &&
                !character.PerformingAction<Walk>() &&
                !character.PerformingAction<Run>() &&
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

        IsBeingPerformed = CanPerform;
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
            character.characterAnimator.ChangeAnimationState("CrouchIdle", "Base");
        else
            character.characterAnimator.ChangeAnimationState("Idle", "Base");
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
                character.characterMover.IsGrounded &&
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
        if (walk)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
            character.characterAnimator.ChangeAnimationState("CrouchMove", "Base");
        else
            character.characterAnimator.ChangeAnimationState("Walk", "Base");
    }
    public override void OnStop()
    {
        if (character.TryGetAction<Idle>(out var idleAction))
        {
            idleAction.EvaluateStatus();
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
                character.characterMover.IsGrounded &&
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
        if (run)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void OnPerform()
    {
        if (character.PerformingAction<Crouch>())
            character.characterAnimator.ChangeAnimationState("CrouchMove", "Base");
        else
            character.characterAnimator.ChangeAnimationState("Run", "Base");
    }
    public override void OnStop()
    {
        if (character.TryGetAction<Idle>(out var idleAction))
        {
            idleAction.EvaluateStatus();
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
        if (sprint)
        {
            IsBeingPerformed = true;
        }
        else
        {
            IsBeingPerformed = false;
        }
    }
    public override void OnPerform()
    {
        if (controller.movementMode == BaseController.MovementMode.EightWay)
            character.characterMover.SetFaceDir(controller.worldMoveDir);

        character.characterAnimator.ChangeAnimationState("Sprint", "Base");
    }
    public override void OnStop()
    {
        if (character.TryGetAction<Idle>(out var idleAction))
        {
            idleAction.EvaluateStatus();
        }
    }
}

[Serializable]
public class Crouch : BaseAction
{
    private void CharacterCommand_CrouchCommand(bool crouch)
    {
        if (!CanPerform)
        {
            IsBeingPerformed = false;
        }
        IsBeingPerformed = crouch;
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
    public override void OnPerform()
    {
        if (character.PerformingAction<Idle>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchIdle", "Base");
        }
        else if (character.PerformingAction<Walk>() || character.PerformingAction<Run>())
        {
            character.characterAnimator.ChangeAnimationState("CrouchMove", "Base");
        }
    }
    public override void OnStop()
    {
        if (character.PerformingAction<Idle>())
        {
            character.characterAnimator.ChangeAnimationState("Idle", "Base");
        }
        else if (character.PerformingAction<Walk>())
        {
            character.characterAnimator.ChangeAnimationState("Walk", "Base");
        }
        else if (character.PerformingAction<Run>())
        {
            character.characterAnimator.ChangeAnimationState("Run", "Base");
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
    public override void OnPerform()
    {
        controller.InitiateJump();
        character.characterMover.IsGrounded = false;
        character.EvaluateAllActions();

        character.characterAnimator.ChangeAnimationState("Jump", "Base");
    }
}

[Serializable]
public class AirJump : BaseAction
{
    private void OnJumpCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
        controller.InitiateJump();
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
    public override void OnPerform()
    {
        character.characterAnimator.ChangeAnimationState("AirJump", "Base");
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
                !character.characterMover.IsGrounded &&
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
                true);
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        IsBeingPerformed = CanPerform;
    }
    public override void OnPerform()
    {
        if (character.characterAnimator.currentStateName == "AirJump" || character.characterAnimator.currentStateName == "Jump")
        {
            return;
        }
        else
        {
            character.characterAnimator.ChangeAnimationState("Fall", "Base");
        }
    }
}