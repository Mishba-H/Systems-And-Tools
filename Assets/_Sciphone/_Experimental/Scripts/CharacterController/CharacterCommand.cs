using System;
using UnityEngine;
using Sciphone.ComboGraph;

public class CharacterCommand : MonoBehaviour
{
    internal event Action<CharacterMover.MovementMode> ChangeMovementModeCommand;
    internal event Action<Vector3> FaceDirCommand;
    internal event Action<Vector3> MoveDirCommand;

    internal event Action<bool> WalkCommand;
    internal event Action<bool> RunCommand;
    internal event Action<bool> SprintCommand;
    internal event Action<bool> CrouchCommand;
    internal event Action JumpCommand;
    internal event Action AirJumpCommand;

    internal event Action ParkourUpCommand;
    internal event Action ParkourDownCommand;

    internal event Action DodgeCommand;
    internal event Action<AttackType> AttackCommand;
    internal event Action BlockCommand;

    public void InvokeChangeMovementMode(CharacterMover.MovementMode value) => ChangeMovementModeCommand?.Invoke(value);
    public void InvokeFaceDir(Vector3 dir) => FaceDirCommand?.Invoke(dir);
    public void InvokeMoveDir(Vector3 dir) => MoveDirCommand?.Invoke(dir);

    public void InvokeWalk(bool value) => WalkCommand?.Invoke(value);
    public void InvokeRun(bool value) => RunCommand?.Invoke(value);
    public void InvokeSprint(bool value) => SprintCommand?.Invoke(value);
    public void InvokeCrouch(bool value) => CrouchCommand?.Invoke(value);

    public void InvokeJump() => JumpCommand?.Invoke();
    public void InvokeAirJump() => AirJumpCommand?.Invoke();

    public void InvokeParkourUp() => ParkourUpCommand?.Invoke();
    public void InvokeParkourDown() => ParkourDownCommand?.Invoke();

    public void InvokeDodge() => DodgeCommand?.Invoke();
    public void InvokeAttack(AttackType type) => AttackCommand?.Invoke(type);
    public void InvokeBlock() => BlockCommand?.Invoke();
}