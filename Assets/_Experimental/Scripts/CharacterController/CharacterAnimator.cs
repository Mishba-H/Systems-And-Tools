using System;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private Character character;

    public string currentStateName = "Idle";

    public bool isInTransition;
    private string savedStateName;
    private string savedLayerName;
    [SerializeReference, Polymorphic] public List<TransitionInfo> transitions;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.PreUpdateLoop += Character_PreUpdateLoop;
    }

    private void Character_PreUpdateLoop()
    {
        if (isInTransition && Mathf.Abs(character.animMachine.rootState.NormalizedTime() - 1) < 0.01f)
        {
            isInTransition = false;
            character.animMachine.PlayActive(savedStateName, savedLayerName);
        }
    }

    public void ChangeAnimationState(string newStateName, string layerName)
    {
        if (isInTransition)
        {
            isInTransition = false;
        }

        foreach (var transition in transitions)
        {
            if (transition.previousStateName == currentStateName && transition.nextStateName == newStateName)
            {
                isInTransition = true;
                savedStateName = newStateName;
                savedLayerName = layerName;
                currentStateName = transition.transitionStateName;
                character.animMachine.PlayActive(transition.transitionStateName, "Transitions");
                return;
            }
        }

        currentStateName = newStateName;
        character.animMachine.PlayActive(currentStateName, layerName);
    }
}

[Serializable]
public class TransitionInfo
{
    public string transitionStateName;
    public string previousStateName;
    public string nextStateName;
}
