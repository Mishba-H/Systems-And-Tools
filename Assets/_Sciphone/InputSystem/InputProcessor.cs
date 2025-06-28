using System;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using ZLinq;

public class InputProcessor : MonoBehaviour
{
    public event Action<InputSequenceType, Vector2> OnProcessInputSequence;

    private InputReader inputReader;

    public List<InputSequence> allSequences;
    public int memorySize = 10; // Max no. of buffered inputs
    public float inputBufferTime = 1.0f;
    public float moveInputThreshold = 0.7f; // Min magnitude to register a directional input
    private Vector2 moveInput;
    private Vector2 refMoveInput;
    private float previousMagnitude;

    [SerializeReference, Polymorphic] public List<ProcessedInput> inputBuffer;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();

        allSequences = allSequences.AsValueEnumerable().OrderByDescending(seq => seq.sequence.Count).ToList();

        inputBuffer = new List<ProcessedInput>();
    }

    private void Start()
    {
        inputReader.Subscribe("Move", OnMoveInput);
        inputReader.Subscribe("Attack", OnAttackInput);
        inputReader.Subscribe("AltAttack", OnAttackAltInput);
    }

    private void Update()
    {
        CleanBuffer();
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        ProcessMoveInput(context.ReadValue<Vector2>());
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.interaction is TapInteraction)
            {
                RegisterInput(InputType.AttackTap);
            }
            else if (context.interaction is HoldInteraction)
            {
                RegisterInput(InputType.AttackHold);
            }
        }
    }

    private void OnAttackAltInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.interaction is TapInteraction)
            {
                RegisterInput(InputType.AltAttackTap);
            }
            else if (context.interaction is HoldInteraction)
            {
                RegisterInput(InputType.AltAttackHold);
            }
        }
    }

    private void ProcessMoveInput(Vector2 inputDir)
    {
        if (inputBuffer.Count > 0)
        {
            InputType currentType = InputType.None;
            if (inputDir.magnitude > moveInputThreshold)
            {
                if (Vector2.Angle(inputDir, refMoveInput) > 150)
                {
                    currentType = InputType.Back;
                    refMoveInput = -inputDir;
                }
                else if (Vector2.Angle(inputDir, refMoveInput) < 30)
                {
                    currentType = InputType.Front;
                    refMoveInput = inputDir;
                }
                else
                {
                    currentType = InputType.Front;
                    refMoveInput = inputDir;
                    inputBuffer.Clear();
                }
            }
            // the condition : {previousMagnitude < moveInputThreshold} makes sure move inputs are registered when crossing above the threshold
            if (currentType != InputType.None && previousMagnitude < moveInputThreshold)
            {
                RegisterInput(currentType);
                moveInput = inputDir;
            }
        }
        else
        {
            if (inputDir.magnitude > moveInputThreshold)
            {
                RegisterInput(InputType.Front);
                refMoveInput = inputDir;
                moveInput = inputDir;
            }
        }
        previousMagnitude = inputDir.magnitude;
    }

    private void RegisterInput(InputType inputType)
    {
        inputBuffer.Add(new ProcessedInput(inputType, Time.time));
        if (inputBuffer.Count > memorySize) inputBuffer.RemoveAt(0);
        CheckSequences();
    }

    private void CleanBuffer()
    {
        for (int i = inputBuffer.Count - 1; i >= 0; i--)
        {
            var input = inputBuffer[i];
            if (Time.time - input.inputTime > inputBufferTime)
            {
                inputBuffer.RemoveAt(i);
            }
        }
    }

    private void CheckSequences()
    {
        foreach (InputSequence inputSequence in allSequences)
        {
            if (inputSequence.sequence.Count > inputBuffer.Count)
            {
                continue;
            }
            bool sequenceMatched = true;
            bool moveInputPresent = false;
            for (int i = 0; i < inputSequence.sequence.Count; i++)
            {
                var bufferedElement = inputBuffer[inputBuffer.Count - inputSequence.sequence.Count + i];
                if (inputSequence.sequence[i] != bufferedElement.inputType ||
                    Time.time - inputBuffer[inputBuffer.Count - inputSequence.sequence.Count + i].inputTime > inputSequence.time)
                {
                    sequenceMatched = false;
                }

                if (bufferedElement.inputType == InputType.Front || bufferedElement.inputType == InputType.Back)
                {
                    moveInputPresent = true;
                }
            }
            if (sequenceMatched)
            {
                if (moveInputPresent)
                {
                    OnProcessInputSequence?.Invoke(inputSequence.sequenceType, moveInput);
                }
                else
                {
                    OnProcessInputSequence?.Invoke(inputSequence.sequenceType, Vector2.zero);
                }
                break;
            }
        }
    }
}

[Serializable]
public class ProcessedInput
{
    public InputType inputType;
    public float inputTime;

    public ProcessedInput(InputType type, float time)
    {
        inputType = type;
        inputTime = time;
    }
}

public enum InputType
{
    None,
    Front,
    Back,
    AttackTap,
    AttackHold,
    AltAttackTap,
    AltAttackHold
}

[Serializable]
public class InputSequence
{
    public InputSequenceType sequenceType;
    public List<InputType> sequence;
    public float time;
}

public enum InputSequenceType
{
    None,
    AttackTap,
    AltAttackTap,
    AttackHold,
    AltAttackHold,
    BackFrontAttack,
    BackFrontAltAttack,
    FrontFrontAttack,
    FrontFrontAltAttack
}