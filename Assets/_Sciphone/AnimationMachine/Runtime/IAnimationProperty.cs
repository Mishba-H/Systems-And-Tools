using System;
using UnityEngine;

public interface IAnimationProperty
{
    object Value { get; set; }
}

#region ANIMATION_STATE_PROPERTIES
[Serializable]
public abstract class AnimationStateProperty : IAnimationProperty
{
    public virtual object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
[Serializable]
public class LoopProperty : AnimationStateProperty
{
    public bool loop = true;
    public override object Value
    {
        get => loop;
        set => loop = (bool)value;
    }
}

[Serializable]
public class PlayWindowProperty : AnimationStateProperty
{
    [MinMaxSlider(0, 1)] public Vector2 playWindow = new Vector2(0, 1);
    public override object Value
    {
        get => playWindow;
        set => playWindow = (Vector2)value;
    }
}

[Serializable]
public class NotCancellableProperty : AnimationStateProperty
{
    public bool notCancellable = true;
    public override object Value
    {
        get => notCancellable;
        set => notCancellable = (bool)value;
    }
}

[Serializable]
public class PlaybackSpeedProperty : AnimationStateProperty
{
    public float playbackSpeed = 1.0f;
    public override object Value
    {
        get => playbackSpeed;
        set => playbackSpeed = (float)value;
    }
}

public class BlendDurationProperty : AnimationStateProperty
{
    [Range(0f, 3f)] public float blendDuration = 0.2f;
    public override object Value 
    { 
        get => blendDuration; 
        set => blendDuration = (float)value; 
    }
}

[Serializable]
public class RootMotionCurvesProperty : AnimationStateProperty
{
    public RootMotionData rootMotionData;
    public override object Value
    {
        get => rootMotionData;
        set => rootMotionData = (RootMotionData)value;
    }
}
[Serializable]
public struct RootMotionData
{
    public AnimationCurve rootTX;
    public AnimationCurve rootTY;
    public AnimationCurve rootTZ;
    public AnimationCurve rootQX;
    public AnimationCurve rootQY;
    public AnimationCurve rootQZ;
    public AnimationCurve rootQW;

    public float totalTime;
    public float totalTXDelta;
    public float totalTYDelta;
    public float totalTZDelta;
    public float totalQXDelta;
    public float totalQYDelta;
    public float totalQZDelta;
    public float totalQWDelta;
}
#endregion

#region ANIMATION_LAYER_PROPERTIES
[Serializable]
public abstract class AnimationLayerProperty : IAnimationProperty
{
    public virtual object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
[Serializable]
public class LayerTypeProperty : AnimationLayerProperty
{
    AnimationLayerType layerType = AnimationLayerType.Active;
    public override object Value
    {
        get => layerType;
        set => layerType = (AnimationLayerType)value;
    }
}
public enum AnimationLayerType
{
    Active,
    Additive,
    Override
}
[Serializable]
public class LayerWeightProperty : AnimationLayerProperty
{
    public float weight = 0f;
    public override object Value
    {
        get => weight;
        set => weight = (float)value;
    }
}
[Serializable]
public class AvatarMaskProperty : AnimationLayerProperty
{
    public AvatarMask mask;
    public override object Value
    {
        get => mask;
        set => mask = (AvatarMask)value;
    }
}
#endregion