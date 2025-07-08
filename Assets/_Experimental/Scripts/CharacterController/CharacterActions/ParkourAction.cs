using System;
using System.Linq.Expressions;
using UnityEngine;
using static ParkourController;

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
            controller.HandleParkourMovement(character.transform.up, -controller.climbNormal, Time.deltaTime * character.timeScale);
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

        character.characterMover.SetWorldVelocity(Vector3.zero);
        character.characterMover.SetGravitySimulation(false);

        controller.CalculateScaleFactor();
        controller.CalculateStartingDistanceFromAnchor();
        controller.SetInitialTransform(controller.climbHit.point, controller.climbNormal, controller.climbHeight, controller.startingDistFromWall);
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
            controller.HandleParkourMovement(character.transform.up, -controller.fenceNormal, Time.deltaTime * character.timeScale);
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

        character.characterMover.SetWorldVelocity(Vector3.zero);
        character.characterMover.SetGravitySimulation(false);

        controller.CalculateScaleFactor();
        controller.CalculateStartingDistanceFromAnchor();
        controller.SetInitialTransform(controller.fenceHit.point, controller.fenceNormal, controller.fenceHeight, controller.startingDistFromWall);
    }
}

[Serializable]
public class LadderTraverse : ParkourAction
{
    private void CharacterCommand_ParkourUpCommand()
    {
        if (!IsBeingPerformed && CanPerform && controller.ladderAscendAvailable)
        {
            IsBeingPerformed = true;
        }
    }
    private void CharacterCommand_ParkourDownCommand()
    {
        if (!IsBeingPerformed && CanPerform && controller.ladderDescendAvailable)
        {
            IsBeingPerformed = true;
        }
        else if (IsBeingPerformed && controller.ladderState != LadderTraverseStates.ClimbUpStart && controller.ladderState != LadderTraverseStates.ClimbDownStart &&
            controller.ladderState != LadderTraverseStates.ClimbUpEnd && controller.ladderState != LadderTraverseStates.ClimbDownEnd)
        {
            IsBeingPerformed = false;
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.ParkourUpCommand += CharacterCommand_ParkourUpCommand;
        character.characterCommand.ParkourDownCommand += CharacterCommand_ParkourDownCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                (!character.PerformingAction<Crouch>() || baseController.canEndCrouch) &&
                !character.PerformingAction<Fall>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<AirJump>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                (controller.ladderAscendAvailable || controller.ladderDescendAvailable) &&
                !character.PerformingAction<ClimbOverFromGround>() &&
                !character.PerformingAction<VaultOverFence>());
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
            if (controller.autoAscendLadder && controller.ladderAscendAvailable)
            {
                IsBeingPerformed = true;
            }
            if (controller.autoDescendLadder && controller.ladderDescendAvailable)
            {
                IsBeingPerformed = true;
            }
        }

        if (controller.ladderState == LadderTraverseStates.ClimbUpEnd || controller.ladderState == LadderTraverseStates.ClimbDownEnd)
        {
            if (Mathf.Abs(character.animMachine.rootState.NormalizedTime() - 1f) < 0.01f)
            {
                IsBeingPerformed = false;
                controller.ChangeLadderState(LadderTraverseStates.None);
            }
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.RunLadderStateMachine();
        }
    }
    public override void OnPerform()
    {
        character.characterMover.ApplyCapsulePreset("Zero");

        if (controller.ladderAscendAvailable)
        {
            controller.ChangeLadderState(LadderTraverseStates.ClimbUpStart);
        }
        else if (controller.ladderDescendAvailable)
        {
            controller.ChangeLadderState(LadderTraverseStates.ClimbDownStart);
        }
    }
    public override void OnStop()
    {
        character.characterMover.SetWorldVelocity(Vector2.zero);
        controller.ChangeLadderState(LadderTraverseStates.None);
    }
}