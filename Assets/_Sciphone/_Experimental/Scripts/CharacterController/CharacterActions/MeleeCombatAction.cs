using System;
using System.Linq.Expressions;
using UnityEngine;
using Sciphone.ComboGraph;

[Serializable]
public abstract class MeleeCombatAction : CharacterAction
{
    [HideInInspector] public MeleeCombatController controller;
}
[Serializable]
public class Evade : MeleeCombatAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.DodgeCommand += OnDodgeCommand;
    }
    private void OnDodgeCommand()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
        controller.evadeInputTime = Time.time;
        controller.InitiateDodge();

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
                !character.PerformingAction<ClimbOverLow>() &&
                !character.PerformingAction<ClimbOverHigh>());
            condition = CombineExpressions(condition, parkourExpression);
        }
        var meleeCombatController = character.GetControllerModule<MeleeCombatController>();
        if (meleeCombatController != null)
        {
            var meleeCombatExpression = (Expression<Func<bool>>)(() =>
                Time.time - meleeCombatController.lastEvadeTime > meleeCombatController.dodgeInterval &&
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
            controller.lastEvadeTime = Time.time;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.fixedDeltaTime);
            controller.HandleDodgeMotion();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Roll : MeleeCombatAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        character.characterCommand.DodgeCommand += OnDodgeInput;
    }
    private void OnDodgeInput()
    {
        if (!CanPerform) return;

        IsBeingPerformed = true;
        controller.rollInputTime = Time.time;
        controller.InitiateDodge();

    }
    public override void CompileCondition()
    {
        Expression<Func<bool>> condition = () => true;
        var baseController = character.GetControllerModule<BaseController>();
        if (baseController != null)
        {
            var baseExpression = (Expression<Func<bool>>)(() =>
                !character.PerformingAction<Roll>() &&
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
                Time.time - meleeCombatController.lastRollTime > meleeCombatController.dodgeInterval &&
                ((character.PerformingAction<Evade>() && Time.time - controller.evadeInputTime > controller.rollWindowTime) ||
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
            controller.lastRollTime = Time.time;
        }
    }
    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleRotation(Time.fixedDeltaTime);
            controller.HandleDodgeMotion();
            controller.SnapToGround();
        }
    }
}
[Serializable]
public class Attack : MeleeCombatAction
{
    public override void OnEnable()
    {
        base.OnEnable();
        InputProcessor.instance.OnProcessInput += OnRecieveProcessedInput;
    }

    private void OnRecieveProcessedInput(InputSequenceType sequenceType)
    {
        if (!CanPerform) return;

        switch (sequenceType)
        {
            case InputSequenceType.AttackTap:
                if (character.PerformingAction<Sprint>())
                    controller.cachedAttack = AttackType.SprintLightAttack;
                else if (character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
                    controller.cachedAttack = AttackType.DodgeAttack;
                else
                    controller.cachedAttack = AttackType.LightAttack;
                break;
            case InputSequenceType.AltAttackTap:
                if (character.PerformingAction<Sprint>())
                    controller.cachedAttack = AttackType.SprintHeavyAttack;
                else if (character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
                    controller.cachedAttack = AttackType.DodgeAttack;
                else
                    controller.cachedAttack = AttackType.HeavyAttack;
                break;
            case InputSequenceType.AttackHold:
                controller.cachedAttack = AttackType.LightHoldAttack;
                break;
            case InputSequenceType.AltAttackHold:
                controller.cachedAttack = AttackType.HeavyHoldAttack;
                break;
            case InputSequenceType.BackFrontAttack:
                controller.cachedAttack = AttackType.BackFrontLightAttack;
                break;
            case InputSequenceType.BackFrontAltAttack:
                controller.cachedAttack = AttackType.BackFrontHeavyAttack;
                break;
            case InputSequenceType.FrontFrontAttack:
                controller.cachedAttack = AttackType.FrontFrontLightAttack;
                break;
            case InputSequenceType.FrontFrontAltAttack:
                controller.cachedAttack = AttackType.FrontFrontHeavyAttack;
                break;
        }
        controller.attackInputTime = Time.time;
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
        }

        if (controller.cachedAttack != AttackType.None && Time.time - controller.attackInputTime < controller.attackCacheDuration && controller.readyToAttack)
        {
            IsBeingPerformed = controller.SelectAttack(controller.cachedAttack);
            if (IsBeingPerformed) controller.readyToAttack = false;
            controller.cachedAttack = AttackType.None;
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
        controller.CalculateAttackDir(Time.deltaTime);

        if (IsBeingPerformed)
        {
            controller.lastAttackTime = Time.time;
        }
        if (Time.time - controller.lastAttackTime > controller.comboEndTime)
        {
            controller.attacksPerformed.Clear();
        }
    }

    public override void FixedUpdate()
    {
        if (IsBeingPerformed)
        {
            controller.HandleAttackMotion();
            controller.HandleRotation(Time.fixedDeltaTime);
        }
    }
}
[Serializable]
public class Block : MeleeCombatAction
{

}