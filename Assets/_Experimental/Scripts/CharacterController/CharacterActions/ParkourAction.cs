using System;
using System.Linq.Expressions;
using UnityEngine;

[Serializable]
public abstract class ParkourAction : CharacterAction
{
    [HideInInspector] public ParkourController controller;
}
[Serializable]
public class ClimbOverFromGround : ParkourAction
{
    private void CharacterCommand_ParkourUpCommand()
    {
        if (CanPerform)
        {
            IsBeingPerformed = true;
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.ParkourUpCommand += CharacterCommand_ParkourUpCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                true);
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() => 
                parkourController.climbAvailable &&
                !character.PerformingAction<ParkourAction>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<MeleeCombatAction>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (CanPerform)
        {
            if (controller.climbHeight > controller.climbLowRange.x && controller.climbHeight <= controller.climbLowRange.y 
                && controller.autoClimbOverFromGroundLow)
            {
                IsBeingPerformed = true;
            }
            else if (controller.climbHeight > controller.climbMediumRange.x && controller.climbHeight <= controller.climbMediumRange.y 
                && controller.autoClimbOverFromGroundMedium)
            {
                IsBeingPerformed = true;
            }
            else if (controller.climbHeight > controller.climbHighRange.x && controller.climbHeight <= controller.climbHighRange.y
                && controller.autoClimbOverFromGroundHigh)
            {
                IsBeingPerformed = true;
            }
        }

        if (MathF.Abs(character.animMachine.rootState.NormalizedTime() - 1f) < 0.01f)
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleParkourMovement(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (controller.climbHeight > controller.climbLowRange.x && controller.climbHeight <= controller.climbLowRange.y)
        {
            character.characterAnimator.ChangeAnimationState("ClimbOverFromGroundLow", "Parkour");
        }
        else if (controller.climbHeight > controller.climbMediumRange.x && controller.climbHeight <= controller.climbMediumRange.y)
        {
            character.characterAnimator.ChangeAnimationState("ClimbOverFromGroundMedium", "Parkour");
        }
        else if (controller.climbHeight > controller.climbHighRange.x && controller.climbHeight <= controller.climbHighRange.y)
        {
            character.characterAnimator.ChangeAnimationState("ClimbOverFromGroundHigh", "Parkour");
        }

        character.characterMover.ApplyCapsulePreset("Zero");

        controller.CalculateScaleFactor(controller.climbHeight);
        controller.CalculateStartingDistanceFromWall();
        controller.SetInitialTransform(controller.climbHit, controller.climbHeight);
    }
}

[Serializable]
public class VaultOverFence : ParkourAction
{
    private void CharacterCommand_ParkourUpCommand()
    {
        if (CanPerform)
        {
            IsBeingPerformed = true;
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.ParkourUpCommand += CharacterCommand_ParkourUpCommand;
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
                controller.fenceAvalilable &&
                !character.PerformingAction<ParkourAction>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<MeleeCombatAction>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (CanPerform)
        {
            if (controller.fenceHeight > controller.vaultFenceLowRange.x && controller.fenceHeight <= controller.vaultFenceLowRange.y
                && controller.autoVaultOverFenceLow)
            {
                IsBeingPerformed = true;
            }
            else if (controller.fenceHeight > controller.vaultFenceMediumRange.x && controller.fenceHeight <= controller.vaultFenceMediumRange.y
                && controller.autoVaultOverFenceMedium)
            {
                IsBeingPerformed = true;
            }
            else if (controller.fenceHeight > controller.vaultFenceHighRange.x && controller.fenceHeight <= controller.vaultFenceHighRange.y
                && controller.autoVaultOverFenceHigh)
            {
                IsBeingPerformed = true;
            }
        }
        if (MathF.Abs(character.animMachine.rootState.NormalizedTime() - 1f) < 0.01f)
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleParkourMovement(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (controller.fenceHeight > controller.vaultFenceLowRange.x && controller.fenceHeight <= controller.vaultFenceLowRange.y)
        {
            character.characterAnimator.ChangeAnimationState("VaultOverFenceLow", "Parkour");
        }
        else if (controller.fenceHeight > controller.vaultFenceMediumRange.x && controller.fenceHeight <= controller.vaultFenceMediumRange.y)
        {
            character.characterAnimator.ChangeAnimationState("VaultOverFenceMedium", "Parkour");
        }
        else if (controller.fenceHeight > controller.vaultFenceHighRange.x && controller.fenceHeight <= controller.vaultFenceHighRange.y)
        {
            character.characterAnimator.ChangeAnimationState("VaultOverFenceHigh", "Parkour");
        }

        character.characterMover.ApplyCapsulePreset("Zero");

        controller.CalculateScaleFactor(controller.fenceHeight);
        controller.CalculateStartingDistanceFromWall();
        controller.SetInitialTransform(controller.fenceHit, controller.fenceHeight);
    }
}