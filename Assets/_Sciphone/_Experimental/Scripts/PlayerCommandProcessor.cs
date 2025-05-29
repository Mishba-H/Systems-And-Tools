using System;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCommandProcessor : MonoBehaviour
{
    public CharacterCommand characterCommand;
    public Transform cameraTransform;

    public CharacterMover.MovementMode movementMode = CharacterMover.MovementMode.Forward;

    public Vector2 moveInput;
    public InputDevice inputDevice;

    public bool sprint = false;
    public bool walk = false;
    public bool crouch = false;

    private void Awake()
    {
        characterCommand = GetComponent<CharacterCommand>();
    }

    private void Start()
    {
        InputReader.instance.Subscribe("Move", OnMoveInput);
        InputReader.instance.Subscribe("Walk", OnWalkInput);
        InputReader.instance.Subscribe("Sprint", OnSprintInput);
        InputReader.instance.Subscribe("Crouch", OnCrouchInput);
        InputReader.instance.Subscribe("Jump", OnJumpInput);

        characterCommand.InvokeChangeMovementMode(movementMode);
    }

    private void Update()
    {
        ProcessInputs();
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            characterCommand.InvokeJump();
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            crouch = !crouch;
            characterCommand.InvokeCrouch(crouch);
        }
    }

    private void OnSprintInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            sprint = !sprint;
        }
    }

    private void OnWalkInput(InputAction.CallbackContext context)
    {
        if (context.control.device is Keyboard)
        {
            if (context.performed)
            {
                walk = true;
            }
            else if (context.canceled)
            {
                walk = false;
            }
        }
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        inputDevice = context.control.device;
    }

    private void ProcessInputs()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up);
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up);
        Vector3 worldMoveDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;

        if (movementMode == CharacterMover.MovementMode.Forward)
        {
            characterCommand.InvokeFaceDir(worldMoveDir);
        }
        else if (movementMode == CharacterMover.MovementMode.EightWay)
        {
            characterCommand.InvokeFaceDir(camForward);
        }

        if (moveInput.sqrMagnitude == 0f)
        {
            sprint = false;
        }

        if (moveInput.sqrMagnitude == 0f)
        {
            characterCommand.InvokeMoveDir(worldMoveDir);
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(false);
        }
        else if (sprint)
        {
            characterCommand.InvokeMoveDir(worldMoveDir);
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(true);
        }
        else if (moveInput.sqrMagnitude < 0.99f || walk)
        {
            characterCommand.InvokeMoveDir(worldMoveDir);
            characterCommand.InvokeWalk(true);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(false);
        }
        else if (moveInput.sqrMagnitude >= 0.99f)
        {
            characterCommand.InvokeMoveDir(worldMoveDir);
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(true);
            characterCommand.InvokeSprint(false);
        }
    }

    [Button(nameof(SwitchMovementMode))]
    public void SwitchMovementMode()
    {
        if (movementMode == CharacterMover.MovementMode.Forward)
        {
            movementMode = CharacterMover.MovementMode.EightWay;
            characterCommand.InvokeChangeMovementMode(movementMode);
        }
        else if (movementMode == CharacterMover.MovementMode.EightWay)
        {
            movementMode = CharacterMover.MovementMode.Forward;
            characterCommand.InvokeChangeMovementMode(movementMode);
        }
    }
}