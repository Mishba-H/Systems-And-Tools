using System;
using UnityEngine;

[Serializable]
public class TimeOfContact : AnimationData
{
    public float timeOfContact;
}

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
