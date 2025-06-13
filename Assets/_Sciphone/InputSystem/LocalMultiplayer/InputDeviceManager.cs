using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceManager : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    public void RemovePlayerByDevice(InputDevice device)
    {
        var playerInput = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None).FirstOrDefault(p => p.devices.Contains(device));
        if (playerInput != null)
        {
            PlayerInputManager.instance.playerLeftEvent.Invoke(playerInput);
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                break;
            case InputDeviceChange.Removed:
                RemovePlayerByDevice(device);
                break;
            case InputDeviceChange.Disconnected:
                RemovePlayerByDevice(device);
                break;
            case InputDeviceChange.Reconnected:
                break;
            case InputDeviceChange.Enabled:
                break;
            case InputDeviceChange.Disabled:
                break;
            case InputDeviceChange.UsageChanged:
                break;
            case InputDeviceChange.ConfigurationChanged:
                break;
        }
    }
}