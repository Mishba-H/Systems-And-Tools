using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCommandProcessor : MonoBehaviour
{
    private Character character;
    private CharacterCommand characterCommand;
    [SerializeField] private Transform cameraTransform;

    public BaseController.MovementMode movementMode = BaseController.MovementMode.Forward;

    public Vector2 moveInput;
    public InputDevice inputDevice;

    public bool sprint = false;
    public bool walk = false;

    private void Awake()
    {
        character = GetComponent<Character>();
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
            characterCommand.InvokeCrouch(!character.PerformingAction<Crouch>());
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

        if (context.control.device is Gamepad)
        {
            if (moveInput.sqrMagnitude < 0.99f)
            {
                walk = true;
            }
            else
            {
                walk = false;
            }
        }
    }

    private void ProcessInputs()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up);
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up);
        Vector3 worldMoveDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        characterCommand.InvokeMoveDir(worldMoveDir);

        if (moveInput.sqrMagnitude == 0f)
        {
            sprint = false;
        }

        if (moveInput.sqrMagnitude == 0f)
        {
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(false);
        }
        else if (sprint)
        {
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(true);
        }
        else if (walk)
        {
            characterCommand.InvokeWalk(true);
            characterCommand.InvokeRun(false);
            characterCommand.InvokeSprint(false);
        }
        else
        {
            characterCommand.InvokeWalk(false);
            characterCommand.InvokeRun(true);
            characterCommand.InvokeSprint(false);
        }

        if (movementMode == BaseController.MovementMode.Forward)
        {
            characterCommand.InvokeFaceDir(worldMoveDir);
        }
        else if (movementMode == BaseController.MovementMode.EightWay)
        {
            characterCommand.InvokeFaceDir(camForward);
        }
    }

    [Button(nameof(SwitchMovementMode))]
    public void SwitchMovementMode()
    {
        if (movementMode == BaseController.MovementMode.Forward)
        {
            movementMode = BaseController.MovementMode.EightWay;
            characterCommand.InvokeChangeMovementMode(movementMode);
        }
        else if (movementMode == BaseController.MovementMode.EightWay)
        {
            movementMode = BaseController.MovementMode.Forward;
            characterCommand.InvokeChangeMovementMode(movementMode);
        }
    }
}