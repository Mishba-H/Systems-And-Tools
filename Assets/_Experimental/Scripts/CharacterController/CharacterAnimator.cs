using Sciphone;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private AnimationMachine animMachine;
    private Character character;

    public string currentStateName = "Idle";

    private void Awake()
    {
        character = GetComponent<Character>();
        animMachine = GetComponent<AnimationMachine>();
    }

    public void ChangeAnimationState(string newStateName, string layerName)
    {
        if (CheckTransitions(currentStateName, newStateName))
        {

        }
        else
        {
            currentStateName = newStateName;
            animMachine.PlayActive(currentStateName, layerName);
        }
    }

    public bool CheckTransitions(string currentStateName, string newStateName)
    {
        return false;
    }

    [SerializeReference, Polymorphic] public AnimationClipState oneShotTest;
    
    [Button(nameof(PlayOneShot))]
    public void PlayOneShot()
    {
        animMachine.PlayOneShot(oneShotTest);
    }
}
