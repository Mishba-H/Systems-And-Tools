using System;
using System.Linq.Expressions;
using UnityEngine;
using Sciphone.ComboGraph;
using System.Linq;

[Serializable]
public abstract class MeleeCombatAction : CharacterAction
{
    [HideInInspector] public MeleeCombatController controller;
}
[Serializable]
public class Evade : MeleeCombatAction
{
    private void OnDodgeCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.DodgeCommand += OnDodgeCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Sprint>() &&
                !character.PerformingAction<Jump>() &&
                !character.PerformingAction<Fall>());
            condition = CombineExpressions(condition, baseExpression);
        }
        var parkourController = character.GetControllerModule<ParkourController>();
        if (parkourController != null)
        {
            var parkourExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<ParkourAction>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                character.time - meleeCombatController.evadeStopTime > meleeCombatController.dodgeInterval &&
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

        if (IsBeingPerformed && (MathF.Abs(character.animMachine.activeState.GetNormalizedTime() - 1f) < 0.01f))
        {
            IsBeingPerformed = false;
        }
        if (character.PerformingAction<Roll>() || character.PerformingAction<Fall>() || character.PerformingAction<Attack>())
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.CalculateScaleFactor();
            controller.HandleDodgeMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        controller.evadePerformTime = character.time;
        controller.InitiateDodge();

        character.characterAnimator.ChangeAnimationState("Evade", "Base");
    }
    public override void OnStop()
    {
        controller.evadeStopTime = character.time;
    }
}
[Serializable]
public class Roll : MeleeCombatAction
{
    private void OnDodgeCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;

    }
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.DodgeCommand += OnDodgeCommand;
    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Jump>() &&
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
                character.time - meleeCombatController.rollStopTime > meleeCombatController.dodgeInterval &&
                ((character.PerformingAction<Evade>() && character.time - controller.evadePerformTime > controller.rollWindowTime) ||
                character.PerformingAction<Sprint>()) &&
                !character.PerformingAction<Attack>());
            condition = CombineExpressions(condition, meleeCombatExpression);
        }

        this.condition = condition.Compile();
    }
    public override void EvaluateStatus()
    {
        base.EvaluateStatus();

        if (IsBeingPerformed && (MathF.Abs(character.animMachine.activeState.GetNormalizedTime() - 1f) < 0.01f))
        {
            IsBeingPerformed = false;
        }
        if (character.PerformingAction<Fall>() || character.PerformingAction<Attack>())
        {
            IsBeingPerformed = false;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.CalculateScaleFactor();
            controller.HandleDodgeMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }
    }
    public override void OnPerform()
    {
        if (character.TryGetAction<Evade>(out var evadeAction))
        {
            evadeAction.EvaluateStatus();
        }
        controller.rollPerformTime = character.time;
        controller.InitiateDodge();

        character.characterAnimator.ChangeAnimationState("Roll", "Base");
    }
    public override void OnStop()
    {
        controller.rollStopTime = character.time;
    }
}
[Serializable]
public class Attack : MeleeCombatAction
{
    private void OnAttackCommand(AttackType attackType)
    {
        if (!CanPerform) return;
        
        controller.cachedAttack = attackType;
        controller.attackCommandTime = character.time;
        controller.attackDir = controller.worldMoveDir == Vector3.zero ? controller.transform.forward : controller.worldMoveDir;
    }
    private void Controller_OnAttackSelected()
    {
        controller.readyToAttack = false;
        controller.cachedAttack = AttackType.None;

        character.characterAnimator.ChangeAnimationState(controller.attacksPerformed.Last().attackName, "GreatSword");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.AttackCommand += OnAttackCommand;
        controller.OnAttackSelected += Controller_OnAttackSelected;
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
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
            !character.PerformingAction<Evade>());
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

        if (controller.readyToAttack && controller.cachedAttack != AttackType.None && 
            character.time - controller.attackCommandTime < controller.attackCacheDuration)
        {
            IsBeingPerformed = controller.TrySelectAttack(controller.cachedAttack);
        }

        if (IsBeingPerformed && character.animMachine.activeState.GetNormalizedTime() >= 1f)
        {
            IsBeingPerformed = false;
        }
        if (!IsBeingPerformed)
        {
            controller.readyToAttack = true;
        }
    }
    public override void Update()
    {
        if (IsBeingPerformed)
        {
            controller.CalculateScaleFactor();
            controller.HandleAttackMotion(Time.deltaTime * character.timeScale);
            controller.HandleRotation(Time.deltaTime * character.timeScale);
        }

        if (!IsBeingPerformed && character.time - controller.lastAttackTime > controller.comboEndTime)
        {
            controller.readyToAttack = true;
            controller.attacksPerformed.Clear();
        }
    }
    public override void OnPerform()
    {
        character.EvaluateAndUpdateAllActions();
    }
    public override void OnStop()
    {
        controller.lastAttackTime = character.time;
        character.EvaluateAndUpdateAllActions();
    }
}
[Serializable]
public class Block : MeleeCombatAction
{

}