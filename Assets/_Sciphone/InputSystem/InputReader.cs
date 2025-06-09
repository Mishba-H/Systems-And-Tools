using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

public class InputReader : MonoBehaviour, IPlayerActions
{
    public Dictionary<string, Action<InputAction.CallbackContext>> inputTable;
    private PlayerInput playerInput;
    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        playerInput = GetComponent<PlayerInput>();
        playerInput.actions = playerInputActions.asset;

        inputTable = new()
        {
            { "Look", null },
            { "Move", null },
            { "Walk", null },
            { "Sprint", null },
            { "Crouch", null },
            { "Jump", null },
            { "Dodge", null },
            { "Block", null },
            { "Attack", null },
            { "AltAttack", null }
        };
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.SetCallbacks(this);
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }

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

    public void OnLook(InputAction.CallbackContext context)
    {
        Invoke("Look", context);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Invoke("Move", context);
    }

    public void OnWalk(InputAction.CallbackContext context)
    {
        Invoke("Walk", context);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        Invoke("Sprint", context);
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        Invoke("Crouch", context);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Invoke("Jump", context);
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        Invoke("Dodge", context);
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        Invoke("Block", context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        Invoke("Attack", context);
    }

    public void OnAltAttack(InputAction.CallbackContext context)
    {
        Invoke("AltAttack", context);
    }
}