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
                character.characterMover.isGrounded &&
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
}

[Serializable]
public class Walk : BaseAction
{
    private bool walk;

    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.WalkCommand += CharacterCommand_WalkCommand;
    }
    private void CharacterCommand_WalkCommand(bool obj)
    {
        walk = obj;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.isGrounded &&
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
}

[Serializable]
public class Run : BaseAction
{
    private Vector3 moveDir;
    private bool run;

    public override void OnEnable()
    {
        base.OnEnable();

        character.characterCommand.RunCommand += CharacterCommand_RunCommand;
    }
    private void CharacterCommand_RunCommand(bool obj)
    {
        run = obj;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.isGrounded &&
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
}

[Serializable]
public class Sprint : BaseAction
{
    private bool sprint;

    public override void OnEnable()
    {
        base.OnEnable();

        character.characterCommand.SprintCommand += CharacterCommand_SprintCommand;
    }
    private void CharacterCommand_SprintCommand(bool obj)
    {
        sprint = obj;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.isGrounded &&
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
}

[Serializable]
public class Crouch : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.CrouchCommand += CharacterCommand_CrouchCommand;
    }
    private void CharacterCommand_CrouchCommand(bool crouch)
    {
        if (!CanPerform)
        {
            IsBeingPerformed = false;
        }
        IsBeingPerformed = crouch;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.isGrounded &&
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
        character.characterCommand.JumpCommand += OnJumpInput;
    }
    private void OnJumpInput()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
        controller.InitiateJump();
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;

        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                character.characterMover.isGrounded &&
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

        if (IsBeingPerformed && character.characterMover.isGrounded && 
            character.transform.InverseTransformDirection(character.characterMover.worldVelocity).y < 0f)
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
}

[Serializable]
public class AirJump : BaseAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.JumpCommand += OnJumpInput;
    }
    private void OnJumpInput()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
        controller.InitiateJump();
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

        if (character.characterMover.isGrounded)
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
                !character.characterMover.isGrounded &&
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
}