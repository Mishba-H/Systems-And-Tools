using System;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using UnityEngine.InputSystem;

public class SplitScreenManager : MonoBehaviour
{
    [SerializeField] public List<Camera> cameras;
    [SerializeReference, Polymorphic] public List<SplitScreenPreset> presets;

    private void Awake()
    {
        SetupBlackBackground();
    }

    void SetupBlackBackground()
    {
        GameObject camObj = new GameObject("BlackBackgroundCamera");
        Camera cam = camObj.AddComponent<Camera>();

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 0;
        cam.depth = -100;
        cam.rect = new Rect(0, 0, 1, 1);
    }

    public void ApplyPreset(int index)
    {
        AssignCamerasFromPlayerInput();

        var preset = presets[index];
        for (int i = 0; i < cameras.Count; i++)
        {   
            cameras[i].enabled = i < preset.viewports.Count;
            if (cameras[i].enabled)
                cameras[i].rect = preset.viewports[i];
        }
    }

    private void AssignCamerasFromPlayerInput()
    {
        cameras.Clear();
        var playerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var playerInput in playerInputs)
        {
            cameras.Add(playerInput.camera);
        }
    }
}

[Serializable]
public class SplitScreenPreset
{
    public string name;
    public List<Rect> viewports;
}