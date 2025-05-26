using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

public class InputReader : MonoBehaviour, IPlayerActions
{
    public static InputReader instance;

    PlayerInput playerInput;
    PlayerInputActions inputActions;

    public Dictionary<string, Action<InputAction.CallbackContext>> inputTable;

    public void Subscribe(string inputName, Action<InputAction.CallbackContext> callback)
    {
        if (inputTable.ContainsKey(inputName))
        {
            inputTable[inputName] += callback;
        }
    }

    public void Unsubscribe(string inputName, Action<InputAction.CallbackContext> callback)
    {
        if (inputTable.ContainsKey(inputName))
        {
            inputTable[inputName] -= callback;
        }
    }

    public void Invoke(string inputName, InputAction.CallbackContext context)
    {
        if (inputTable.TryGetValue(inputName, out var action))
        {
            action?.Invoke(context);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Multiple InputReader instances detected.");
        }
        instance = this;

        playerInput = GetComponent<PlayerInput>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.SetCallbacks(this);
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    public event Action<Vector2, InputDevice> Look;
    public void OnLook(InputAction.CallbackContext context)
    {
        Look?.Invoke(context.ReadValue<Vector2>(), context.control.device);
    }

    public event Action<Vector2> Move;
    public void OnMove(InputAction.CallbackContext context)
    {
        Move?.Invoke(context.ReadValue<Vector2>());
    }

    public event Action<bool, InputDevice> Walk;
    public void OnWalk(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Walk?.Invoke(true, context.control.device);
                break;
            case InputActionPhase.Canceled:
                Walk?.Invoke(false, context.control.device);
                break;
        }
    }

    public event Action<bool, InputDevice> Sprint;
    public void OnSprint(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Sprint?.Invoke(true, context.control.device);
                break;
            case InputActionPhase.Canceled:
                Sprint?.Invoke(false, context.control.device);
                break;
        }
    }

    public event Action Crouch;
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
            Crouch?.Invoke();
    }

    public event Action<bool> Jump;
    public void OnJump(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Jump?.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Jump?.Invoke(false);
                break;
        }
    }

    public event Action Dodge;
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
            Dodge?.Invoke();
    }

    public event Action Block;
    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.performed)
            Block?.Invoke();
    }

    public event Action<IInputInteraction> Attack;
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            Attack?.Invoke(context.interaction);
    }

    public event Action<IInputInteraction> AltAttack;
    public void OnAltAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            AltAttack?.Invoke(context.interaction);
    }
}