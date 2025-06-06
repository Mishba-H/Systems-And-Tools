using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

public class InputReader : MonoBehaviour, IPlayerActions
{
    public Dictionary<string, Action<InputAction.CallbackContext>> inputTable;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        inputTable = new();
        inputTable.Add("Look", null);
        inputTable.Add("Move", null);
        inputTable.Add("Walk", null);
        inputTable.Add("Sprint", null);
        inputTable.Add("Crouch", null);
        inputTable.Add("Jump", null);
        inputTable.Add("Dodge", null);
        inputTable.Add("Block", null);
        inputTable.Add("Attack", null);
        inputTable.Add("AttackAlt", null);
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
        Invoke("AttackAlt", context);
    }
}