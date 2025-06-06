using System;
using UnityEngine;
using Sciphone.ComboGraph;

public class CharacterCommand : MonoBehaviour
{
    internal event Action<BaseController.MovementMode> ChangeMovementModeCommand;
    internal event Action<Vector3> FaceDirCommand;
    internal event Action<Vector3> MoveDirCommand;

    internal event Action<bool> WalkCommand;
    internal event Action<bool> RunCommand;
    internal event Action<bool> SprintCommand;
    internal event Action<bool> CrouchCommand;
    internal event Action JumpCommand;

    internal event Action ParkourUpCommand;
    internal event Action ParkourDownCommand;

    internal event Action DodgeCommand;
    internal event Action<AttackType> AttackCommand;
    internal event Action BlockCommand;

    public void InvokeChangeMovementModeCommand(BaseController.MovementMode value) => ChangeMovementModeCommand?.Invoke(value);
    public void InvokeFaceDirCommand(Vector3 dir) => FaceDirCommand?.Invoke(dir);
    public void InvokeMoveDirCommand(Vector3 dir) => MoveDirCommand?.Invoke(dir);

    public void InvokeWalkCommand(bool value) => WalkCommand?.Invoke(value);
    public void InvokeRunCommand(bool value) => RunCommand?.Invoke(value);
    public void InvokeSprintCommand(bool value) => SprintCommand?.Invoke(value);
    public void InvokeCrouchCommand(bool value) => CrouchCommand?.Invoke(value);
    public void InvokeJumpCommand() => JumpCommand?.Invoke();

    public void InvokeParkourUpCommand() => ParkourUpCommand?.Invoke();
    public void InvokeParkourDownCommand() => ParkourDownCommand?.Invoke();

    public void InvokeDodgeCommand() => DodgeCommand?.Invoke();
    public void InvokeAttackCommand(AttackType type) => AttackCommand?.Invoke(type);
    public void InvokeBlockCommand() => BlockCommand?.Invoke();
}