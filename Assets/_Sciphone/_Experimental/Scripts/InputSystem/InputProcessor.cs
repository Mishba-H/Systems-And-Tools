using System;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using System.Linq;

public class InputProcessor : MonoBehaviour
{
    public static InputProcessor instance;

    public Action<InputSequenceType> OnProcessInput { get; internal set; }

    public List<InputSequence> allSequences;

    public int memorySize = 10; // Max stored inputs
    public float inputBufferTime = 1.0f; // Time window for sequences
    public float moveInputThreshold = 0.7f; // Min magnitude to register a direction
    private Vector2 storedMoveDir;
    private float previousMagnitude;

    [SerializeReference, Polymorphic] public List<ProcessedInput> inputBuffer;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Multiple InputProcessor instances detected.");
            return;
        }
        instance = this;


        allSequences = allSequences.OrderByDescending(seq => seq.sequence.Count).ToList();

        inputBuffer = new List<ProcessedInput>();
    }

    private void Start()
    {
        InputReader.instance.Move += OnMoveInput;
        InputReader.instance.Attack += OnAttackInput;
        InputReader.instance.AltAttack += OnAltAttackInput;
    }

    private void Update()
    {
        CleanBuffer();
    }

    private void OnMoveInput(Vector2 vector)
    {
        ProcessMoveInput(vector);
    }

    private void OnAttackInput(IInputInteraction interaction)
    {
        if (interaction is TapInteraction)
        {
            RegisterInput(InputType.AttackTap);
        }
        else if (interaction is HoldInteraction)
        {
            RegisterInput(InputType.AttackHold);
        }
    }

    private void OnAltAttackInput(IInputInteraction interaction)
    {
        if (interaction is TapInteraction)
        {
            RegisterInput(InputType.AltAttackTap);
        }
        else if (interaction is HoldInteraction)
        {
            RegisterInput(InputType.AltAttackHold);
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
        foreach (InputSequence seq in allSequences)
        {
            if (seq.sequence.Count > inputBuffer.Count)
            {
                continue;
            }
            bool sequenceMatched = true;
            for (int i = 0; i < seq.sequence.Count; i++)
            {
                if (seq.sequence[i] != inputBuffer[inputBuffer.Count - seq.sequence.Count + i].inputType ||
                    Time.time - inputBuffer[inputBuffer.Count - seq.sequence.Count + i].inputTime > seq.time)
                {
                    sequenceMatched = false;
                }
            }
            if (sequenceMatched)
            {
                OnProcessInput?.Invoke(seq.sequenceType);
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