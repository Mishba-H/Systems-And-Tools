using System;
using UnityEngine;

public interface IAnimationEvent
{
    GameObject GameObject { get; set; }
    bool hasTriggered { get; set; }
    void Evaluate(float normalizedTime);
    void TriggerEventBehaviour();
}
[Serializable]
public abstract class TriggerEvent : IAnimationEvent
{
    public GameObject GameObject { get; set; }
    public bool hasTriggered { get; set; }

    [Range(0f, 1f)] public float triggerTime;

    public void Evaluate(float normalizedTime)
    {
        if (normalizedTime >= triggerTime && !hasTriggered)
        {
            hasTriggered = true;
            TriggerEventBehaviour();
        }
        if (normalizedTime < triggerTime)
        {
            hasTriggered = false;
        }
    }

    public virtual void TriggerEventBehaviour() { }
}
[Serializable]
public abstract class WindowEvent : IAnimationEvent
{
    public GameObject GameObject { get; set; }
    public bool hasTriggered { get; set; }
    [MinMaxSlider(0f, 1f)] public Vector2 window;
    [HideInInspector] public bool isActive;

    public void Evaluate(float normalizedTime)
    {
        if (normalizedTime >= window.x && normalizedTime < window.y && !isActive && !hasTriggered)
        {
            isActive = true;
            TriggerEventBehaviour();
        }
        if (normalizedTime >= window.y && isActive && !hasTriggered)
        {
            isActive = false;
            TriggerEventBehaviour();
        }
        if (normalizedTime < window.x)
        {
            hasTriggered = false;
            isActive = false;
        }
        if (normalizedTime > window.y)
        {
            hasTriggered = true;
            isActive = false;
        }
    }

    public void TriggerEventBehaviour()
    {
        if (isActive) 
            OnEventStarted();
        else
            OnEventFinished();
    }

    public virtual void OnEventStarted() { }
    public virtual void OnEventFinished() { }
}
[Serializable]
public class LogEvent : TriggerEvent
{
    public override void TriggerEventBehaviour()
    {
        Debug.Log($"This is a log event");
    }
}
[Serializable]
public class LogWindow : WindowEvent
{
    public override void OnEventStarted()
    {
        Debug.Log($"This is a log event started");
    }

    public override void OnEventFinished()
    {
        Debug.Log($"This is a log event finished");
    }
}