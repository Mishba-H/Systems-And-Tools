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
                !character.PerformingAction<Evade>() &&
                !character.PerformingAction<Roll>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() => 
                parkourController.climbPointAvailable &&
                !character.PerformingAction<ClimbOverFromGround>());
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

        if (MathF.Abs(character.animMachine.activeState.NormalizedTime() - 1f) < 0.01f)
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.HandleClimb(Time.deltaTime * character.timeScale);
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

        controller.InitiateClimb();
    }
}