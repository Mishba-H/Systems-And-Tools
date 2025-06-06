using System;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using System.Linq;

public class InputProcessor : MonoBehaviour
{
    public Action<InputSequenceType> OnProcessInput { get; internal set; }

    private InputReader inputReader;

    public List<InputSequence> allSequences;

    public int memorySize = 10; // Max no. of buffered inputs
    public float inputBufferTime = 1.0f;
    public float moveInputThreshold = 0.7f; // Min magnitude to register a directional input
    private Vector2 storedMoveDir;
    private float previousMagnitude;

    [SerializeReference, Polymorphic] public List<ProcessedInput> inputBuffer;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();

        allSequences = allSequences.OrderByDescending(seq => seq.sequence.Count).ToList();

        inputBuffer = new List<ProcessedInput>();
    }

    private void Start()
    {
        inputReader.Subscribe("Move", OnMoveInput);
        inputReader.Subscribe("Attack", OnAttackInput);
        inputReader.Subscribe("AttackAlt", OnAttackAltInput);
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
                if (Vector2.Angle(inputDir, storedMoveDir) > 150)
                {
                    currentType = InputType.Back;
                    storedMoveDir = -inputDir;
                }
                else if (Vector2.Angle(inputDir, storedMoveDir) < 30)
                {
                    currentType = InputType.Front;
                    storedMoveDir = inputDir;
                }
                else
                {
                    currentType = InputType.Front;
                    storedMoveDir = inputDir;
                    inputBuffer.Clear();
                }
            }
            if (currentType != InputType.None && previousMagnitude < moveInputThreshold)
            {
                RegisterInput(currentType);
            }
        }
        else
        {
            if (inputDir.magnitude > moveInputThreshold)
            {
                RegisterInput(InputType.Front);
                storedMoveDir = inputDir;
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
        inputBuffer.RemoveAll(input => Time.time - input.inputTime > inputBufferTime);
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
            for (int i = 0; i < inputSequence.sequence.Count; i++)
            {
                if (inputSequence.sequence[i] != inputBuffer[inputBuffer.Count - inputSequence.sequence.Count + i].inputType ||
                    Time.time - inputBuffer[inputBuffer.Count - inputSequence.sequence.Count + i].inputTime > inputSequence.time)
                {
                    sequenceMatched = false;
                }
            }
            if (sequenceMatched)
            {
                OnProcessInput?.Invoke(inputSequence.sequenceType);
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