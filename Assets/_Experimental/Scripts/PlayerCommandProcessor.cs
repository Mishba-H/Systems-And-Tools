using Sciphone;
using Sciphone.ComboGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCommandProcessor : MonoBehaviour
{
    [SerializeField] private GameObject characterGameObject;
    [SerializeField] private Transform cameraTransform;

    private InputReader inputReader;
    private InputProcessor inputProcessor;
    private Character character;
    private CharacterCommand characterCommand;

    public BaseController.MovementMode movementMode = BaseController.MovementMode.Forward;

    public Vector2 moveInput;
    public InputDevice inputDevice;

    public bool sprint = false;
    public bool walk = false;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();
        inputProcessor = GetComponent<InputProcessor>();

        character = characterGameObject.GetComponent<Character>();
        characterCommand = characterGameObject.GetComponent<CharacterCommand>();
    }

    private void Start()
    {
        inputReader.Subscribe("Move", OnMoveInput);
        inputReader.Subscribe("Walk", OnWalkInput);
        inputReader.Subscribe("Sprint", OnSprintInput);
        inputReader.Subscribe("Crouch", OnCrouchInput);
        inputReader.Subscribe("Jump", OnJumpInput);
        inputReader.Subscribe("Dodge", OnDodgeInput);

        inputProcessor.OnProcessInput += OnAttackInput;

        characterCommand.InvokeChangeMovementModeCommand(movementMode);
    }

    private void Update()
    {
        ProcessInputs();
    }

    private void ProcessInputs()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, characterGameObject.transform.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, characterGameObject.transform.up).normalized;

        Vector3 worldMoveDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        characterCommand.InvokeMoveDirCommand(worldMoveDir);

        if (moveInput.sqrMagnitude == 0f)
        {
            sprint = false;
        }

        if (moveInput.sqrMagnitude == 0f)
        {
            characterCommand.InvokeWalkCommand(false);
            characterCommand.InvokeRunCommand(false);
            characterCommand.InvokeSprintCommand(false);
        }
        else if (sprint)
        {
            characterCommand.InvokeWalkCommand(false);
            characterCommand.InvokeRunCommand(false);
            characterCommand.InvokeSprintCommand(true);
        }
        else if (walk)
        {
            characterCommand.InvokeWalkCommand(true);
            characterCommand.InvokeRunCommand(false);
            characterCommand.InvokeSprintCommand(false);
        }
        else
        {
            characterCommand.InvokeWalkCommand(false);
            characterCommand.InvokeRunCommand(true);
            characterCommand.InvokeSprintCommand(false);
        }

        if (movementMode == BaseController.MovementMode.Forward)
        {
            characterCommand.InvokeFaceDirCommand(worldMoveDir);
        }
        else if (movementMode == BaseController.MovementMode.EightWay)
        {
            characterCommand.InvokeFaceDirCommand(camForward);
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

    private void OnSprintInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            sprint = !sprint;
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            characterCommand.InvokeCrouchCommand(!character.PerformingAction<Crouch>());
        }
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            characterCommand.InvokeJumpCommand();
            characterCommand.InvokeParkourUpCommand();
        }
    }

    private void OnDodgeInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            characterCommand.InvokeDodgeCommand();
            characterCommand.InvokeParkourDownCommand();
        }
    }

    private void OnAttackInput(InputSequenceType sequenceType)
    {
        switch (sequenceType)
        {
            case InputSequenceType.AttackTap:
                if (character.PerformingAction<Sprint>())
                    characterCommand.InvokeAttackCommand(AttackType.SprintLightAttack);
                else if ((character.PerformingAction<Roll>() && character.GetControllerModule<MeleeCombatController>().relativeDodgeDir == Vector2.up))
                    characterCommand.InvokeAttackCommand(AttackType.DodgeAttack);
                else
                    characterCommand.InvokeAttackCommand(AttackType.LightAttack);
                break;
            case InputSequenceType.AltAttackTap:
                if (character.PerformingAction<Sprint>())
                    characterCommand.InvokeAttackCommand(AttackType.SprintHeavyAttack);
                else if (character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
                    characterCommand.InvokeAttackCommand(AttackType.DodgeAttack);
                else
                    characterCommand.InvokeAttackCommand(AttackType.HeavyAttack);
                break;
            case InputSequenceType.AttackHold:
                characterCommand.InvokeAttackCommand(AttackType.LightHoldAttack);
                break;
            case InputSequenceType.AltAttackHold:
                characterCommand.InvokeAttackCommand(AttackType.HeavyHoldAttack);
                break;
            case InputSequenceType.BackFrontAttack:
                characterCommand.InvokeAttackCommand(AttackType.BackFrontLightAttack);
                break;
            case InputSequenceType.BackFrontAltAttack:
                characterCommand.InvokeAttackCommand(AttackType.BackFrontHeavyAttack);
                break;
            case InputSequenceType.FrontFrontAttack:
                characterCommand.InvokeAttackCommand(AttackType.FrontFrontLightAttack);
                break;
            case InputSequenceType.FrontFrontAltAttack:
                characterCommand.InvokeAttackCommand(AttackType.FrontFrontHeavyAttack);
                break;
        }
    }


    [Button(nameof(SwitchMovementMode))]
    public void SwitchMovementMode()
    {
        if (movementMode == BaseController.MovementMode.Forward)
        {
            movementMode = BaseController.MovementMode.EightWay;
            characterCommand.InvokeChangeMovementModeCommand(movementMode);
        }
        else if (movementMode == BaseController.MovementMode.EightWay)
        {
            movementMode = BaseController.MovementMode.Forward;
            characterCommand.InvokeChangeMovementModeCommand(movementMode);
        }
    }
}