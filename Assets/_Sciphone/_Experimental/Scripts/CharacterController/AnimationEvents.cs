using System;

[Serializable]
public class ChangeAnimationState : TriggerEvent
{
    public string newState;
    public string layerName;
    public override void TriggerEventBehaviour()
    {
        if (GameObject.TryGetComponent(out CharacterAnimator characterAnimator))
        {
            characterAnimator.ChangeAnimationState(newState, layerName);
        }
    }
}

[Serializable]
public class ReadyToAttack : TriggerEvent
{
    public override void TriggerEventBehaviour()
    {
        if (GameObject.TryGetComponent(out MeleeCombatController controller))
        {
            controller.readyToAttack = true;
        }
    }
}

[Serializable]
public class ReadAttackInput : WindowEvent
{
    public override void OnEventStarted()
    {
        base.OnEventStarted();
    }

    public override void OnEventFinished()
    {
        base.OnEventFinished();
    }
}

[Serializable]
public class TriggerNextAttack : TriggerEvent
{
    public override void TriggerEventBehaviour()
    {
        if (GameObject.TryGetComponent(out MeleeCombatController controller))
        {
            controller.readyToAttack = true;
        }
    }
}
