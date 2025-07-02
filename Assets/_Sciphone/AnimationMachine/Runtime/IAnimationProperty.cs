using System;
using UnityEngine;

public interface IAnimationProperty
{
}

#region ANIMATION_STATE_PROPERTIES
[Serializable]
public abstract class AnimationStateProperty : IAnimationProperty
{
}

[Serializable]
public class LoopProperty : AnimationStateProperty
{
}

[Serializable]
public class PlayWindowProperty : AnimationStateProperty
{
    [MinMaxSlider(0, 1)] public Vector2 playWindow = new Vector2(0, 1);
}

[Serializable]
public class NotCancellableProperty : AnimationStateProperty
{
}

[Serializable]
public class PlaybackSpeedProperty : AnimationStateProperty
{
    public float playbackSpeed = 1.0f;
}

public class BlendDurationProperty : AnimationStateProperty
{
    [Range(0f, 3f)] public float blendDuration = 0.2f;
}

[Serializable]
public class RootMotionCurvesProperty : AnimationStateProperty
{
    public RootMotionData rootMotionData;
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
}

[Serializable]
public class ScaleModeProperty : AnimationStateProperty
{
    public ScaleMode scaleModeX;
    public ScaleMode scaleModeY;
    public ScaleMode scaleModeZ;
}
public enum ScaleMode
{
    None,
    Zero,
    AvgValue,
    MaxValue,
    TotalValue,
    Invert
}
#endregion

#region ANIMATION_LAYER_PROPERTIES
[Serializable]
public abstract class AnimationLayerProperty : IAnimationProperty
{
}
[Serializable]
public class LayerTypeProperty : AnimationLayerProperty
{
    public AnimationLayerType layerType = AnimationLayerType.Root;
}
public enum AnimationLayerType
{
    Root,
    Override,
    Additive
}
[Serializable]
public class AvatarMaskProperty : AnimationLayerProperty
{
    public AvatarMask mask;
}
#endregion