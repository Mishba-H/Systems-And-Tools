using System;
using System.Linq.Expressions;
using UnityEngine;

[Serializable]
public abstract class ParkourAction : CharacterAction
{
    [HideInInspector] public ParkourController controller;
}
[Serializable]
public class ClimbOverLow : ParkourAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Jump += OnJumpPressed;
    }
    private void OnJumpPressed(bool jumpPressed)
    {
        if (CanPerform && jumpPressed)
        {
            IsBeingPerformed = true;
            controller.InitiateClimb();
        }
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
            var parkourExpression = (Expression<Func<bool>>)(()=> 
                parkourController.climbLowAvailable &&
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (MathF.Abs(character.animMachine.activeState.GetNormalizedTime() - 1f) < 0.01f)
        {
            IsBeingPerformed = false;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleClimb();
        }
    }
}
[Serializable]
public class ClimbOverHigh : ParkourAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputReader.instance.Jump += OnJumpPressed;
    }
    private void OnJumpPressed(bool jumpPressed)
    {
        if (CanPerform && jumpPressed)
        {
            IsBeingPerformed = true;
            controller.InitiateClimb();
        }
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
                parkourController.climbHighAvailable &&
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (MathF.Abs(character.animMachine.activeState.GetNormalizedTime() - 1f) < 0.01f)
        {
            IsBeingPerformed = false;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleClimb();
        }
    }
}