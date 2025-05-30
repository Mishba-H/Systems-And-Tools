using Sciphone;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private AnimationMachine animMachine;
    private Character character;

    private float error = 0.1f;
    private float lerpSpeed = 10f;

    public string currentStateName = "Idle";

    private void Awake()
    {
        character = GetComponent<Character>();
        animMachine = GetComponent<AnimationMachine>();
    }

    private void Update()
    {
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

    /*private void HandleCharacterAnimation()
    {
        if (character.PerformingAction<Attack>())
        {
            var attacks = character.GetControllerModule<MeleeCombatController>().attacksPerformed;
            if (attacks.Count > 0)
                animMachine.PlayActive(attacks[attacks.Count - 1].attackName, "GreatSword");
        }
        else if (character.PerformingAction<Idle>())
        {
            if (character.PerformingAction<Crouch>())
                animMachine.PlayActive("Crouch", "Base");
            else
                animMachine.PlayActive("Idle", "Base");
        }
        else if (character.PerformingAction<Walk>())
        {
            if (character.PerformingAction<Crouch>())
                animMachine.PlayActive("Crouch", "Base");
            else
                animMachine.PlayActive("Walk", "Base");
        }
        else if (character.PerformingAction<Run>())
        {
            if (character.PerformingAction<Crouch>())
                animMachine.PlayActive("Crouch", "Base");
            else
                animMachine.PlayActive("Run", "Base");
        }
        else if (character.PerformingAction<Sprint>())
        {
            animMachine.PlayActive("Sprint", "Base");
        }
        else if (character.PerformingAction<Jump>())
        {
            animMachine.PlayActive("Jump", "Base");
        }
        else if (character.PerformingAction<AirJump>())
        {
            animMachine.PlayActive("AirJump", "Base");
        }
        else if (character.PerformingAction<Fall>())
        {
            animMachine.PlayActive("Fall", "Base");
        }
        else if (character.PerformingAction<Evade>())
        {
            animMachine.PlayActive("Evade", "Base");
        }
        else if (character.PerformingAction<Roll>())
        {
            animMachine.PlayActive("Roll", "Base");
        }
        else if (character.PerformingAction<ClimbOverLow>())
        {
            animMachine.PlayActive("ClimbOverLow", "Parkour");
        }
        else if (character.PerformingAction<ClimbOverHigh>())
        {
            animMachine.PlayActive("ClimbOverHigh", "Parkour");
        }
    }*/

    private void HandleBlendParameters()
    {
        Vector2 moveInput = new Vector2(0f, 1f);
        if (character.PerformingAction<Walk>())
        {
            Vector2 moveDir = moveInput.normalized;
            float moveDirX = moveDir.x;
            float moveDirZ = moveDir.y;

            var state = (EightWayBlendState)animMachine.layers.GetLayerInfo("Base").GetStateInfo("Walk");
            state.blendX = Mathf.Abs(moveDirX - state.blendX) < error ?
                moveDirX : Mathf.Lerp(state.blendX, moveDirX, lerpSpeed * Time.deltaTime);
            state.blendY = Mathf.Abs(moveDirZ - state.blendY) < error ?
                moveDirZ : Mathf.Lerp(state.blendY, moveDirZ, lerpSpeed * Time.deltaTime);
        }
        if (character.PerformingAction<Run>())
        {
            Vector2 moveDir = moveInput.normalized;
            float moveDirX = moveDir.x > error ? 1 : moveDir.x < -error ? -1 : 0;
            float moveDirZ = moveDir.y > error ? 1 : moveDir.y < -error ? -1 : 0;

            var state = (EightWayBlendState)animMachine.layers.GetLayerInfo("Base").GetStateInfo("Run");
            state.blendX = Mathf.Abs(moveDirX - state.blendX) < error ?
                moveDirX : Mathf.Lerp(state.blendX, moveDirX, lerpSpeed * Time.deltaTime);
            state.blendY = Mathf.Abs(moveDirZ - state.blendY) < error ?
                moveDirZ : Mathf.Lerp(state.blendY, moveDirZ, lerpSpeed * Time.deltaTime);
        }
        if (character.PerformingAction<Crouch>())
        {
            Vector2 moveDir = moveInput.normalized;
            float moveDirX = moveDir.x > error ? 1 : moveDir.x < -error ? -1 : 0;
            float moveDirZ = moveDir.y > error ? 1 : moveDir.y < -error ? -1 : 0;

            var state = (EightWayBlendState)animMachine.layers.GetLayerInfo("Base").GetStateInfo("CrouchMove");
            state.blendX = Mathf.Abs(moveDirX - state.blendX) < error ?
                moveDirX : Mathf.Lerp(state.blendX, moveDirX, lerpSpeed * Time.deltaTime);
            state.blendY = Mathf.Abs(moveDirZ - state.blendY) < error ?
                moveDirZ : Mathf.Lerp(state.blendY, moveDirZ, lerpSpeed * Time.deltaTime);
        }
        if (character.PerformingAction<Evade>())
        {
            var state = (FourWayBlendState)animMachine.layers.GetLayerInfo("Base").GetStateInfo("Evade");
            state.blendX = character.GetControllerModule<MeleeCombatController>().dodgeDir.x;
            state.blendY = character.GetControllerModule<MeleeCombatController>().dodgeDir.y;
        }
        if (character.PerformingAction<Roll>())
        {
            var state = (FourWayBlendState)animMachine.layers.GetLayerInfo("Base").GetStateInfo("Roll");
            state.blendX = character.GetControllerModule<MeleeCombatController>().dodgeDir.x;
            state.blendY = character.GetControllerModule<MeleeCombatController>().dodgeDir.y;
        }
    }

    [SerializeReference, Polymorphic] public AnimationClipState oneShotTest;
    [Button(nameof(PlayOneShot))]
    public void PlayOneShot()
    {
        animMachine.PlayOneShot(oneShotTest);
    }
}
