using System;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;
using Sciphone;
using UnityEngine.Animations;
using ZLinq;

[Serializable]
public abstract class AnimationStateInfo
{
    public string stateName = "DefaultState";
    [SerializeReference, Polymorphic] public List<AnimationStateProperty> properties = new List<AnimationStateProperty>();
    [SerializeReference, Polymorphic] public List<IAnimationEvent> events;
    [SerializeReference, Polymorphic] public List<IAnimationData> data;
    [HideInInspector] public float length;

    internal AnimationMachine animMachine;
    internal Playable statePlayable;

    public virtual void Initialize(AnimationMachine animMachine)
    {
        this.animMachine = animMachine;
        foreach (var e in events)
        {
            e.GameObject = animMachine.gameObject;
        }
    }
    public virtual void ResetState(float normalizedTime = 0f) { }
    public virtual void UpdateWeights() { }
    public float NormalizedTime()
    {
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            var currentTime = (float)statePlayable.GetTime() - playWindow.x * length;
            var duration = (playWindow.y - playWindow.x) * length;
            return currentTime / duration;
        }
        return (float)(statePlayable.GetTime() / statePlayable.GetDuration());
    }
    public bool TryGetProperty<T>(out T property) where T : AnimationStateProperty
    {
        property = properties.AsValueEnumerable().FirstOrDefault(t => t.GetType() == typeof(T)) as T;
        return property != null;
    }
    public bool TryGetData<T>(out T animData) where T : AnimationData
    {
        animData = data.AsValueEnumerable().FirstOrDefault(t => t.GetType() == typeof(T)) as T;
        return animData != null;
    }
}

[Serializable]
public class AnimationClipState : AnimationStateInfo
{
    [PreviewAnimationClip(nameof(clip), nameof(properties))] public float preview;
    [ClipOrFBX] public AnimationClip clip;

    public override void Initialize(AnimationMachine animMachine)
    {
        base.Initialize(animMachine);

        var graph = animMachine.playableGraph;

        statePlayable = AnimationClipPlayable.Create(graph, clip);
        length = clip.length;
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetDuration(playWindow.y * clip.length);
        }
        else
        {
            statePlayable.SetDuration(clip.length);
        }
    }
    public override void ResetState(float normalizedTime = 0f)
    {
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            statePlayable.SetTime(normalizedTime * length);
            statePlayable.SetTime(normalizedTime * length);
        }
        animMachine.playableGraph.Evaluate(0f);
    }

}

[Serializable]
public class FourWayBlendState : AnimationStateInfo
{
    [PreviewAnimationClip(nameof(Forward), nameof(properties))] public float preview;
    [ClipOrFBX] public AnimationClip Forward;
    [ClipOrFBX] public AnimationClip Backward;
    [ClipOrFBX] public AnimationClip Right;
    [ClipOrFBX] public AnimationClip Left;
    [ClipOrFBX] public AnimationClip Center;
    [Range(-1f, 1f)] public float blendX;
    [Range(-1f, 1f)] public float blendY;
    public float[] weights = new float[5];

    public override void Initialize(AnimationMachine animMachine)
    {
        base.Initialize(animMachine);

        var graph = animMachine.playableGraph;

        statePlayable = AnimationMixerPlayable.Create(graph, 5);
        graph.Connect(AnimationClipPlayable.Create(graph, Forward), 0, statePlayable, 0);
        graph.Connect(AnimationClipPlayable.Create(graph, Backward), 0, statePlayable, 1);
        graph.Connect(AnimationClipPlayable.Create(graph, Right), 0, statePlayable, 2);
        graph.Connect(AnimationClipPlayable.Create(graph, Left), 0, statePlayable, 3);
        graph.Connect(AnimationClipPlayable.Create(graph, Center), 0, statePlayable, 4);
        length = (Forward.length + Backward.length + Right.length + Left.length) / 4;

        if (TryGetProperty<PlayWindowProperty>(out var property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetDuration(playWindow.y * length);
        }
        else
        {
            statePlayable.SetDuration(length);
        }
        for (int i = 0; i < statePlayable.GetInputCount(); i++)
        {
            Playable inputPlayable = statePlayable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty(out property))
                {
                    var playWindow = property.playWindow;
                    inputPlayable.SetDuration(playWindow.y * length);
                }
                else
                {
                    inputPlayable.SetDuration(length);
                }
            }
        }
    }
    public override void ResetState(float normalizedTime = 0f)
    {
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            statePlayable.SetTime(normalizedTime * length);
            statePlayable.SetTime(normalizedTime * length);
        }
        for (int i = 0; i < statePlayable.GetInputCount(); i++)
        {
            Playable inputPlayable = statePlayable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty(out property))
                {
                    var playWindow = property.playWindow;
                    inputPlayable.SetTime(playWindow.x * length + normalizedTime * length);
                    inputPlayable.SetTime(playWindow.x * length + normalizedTime * length);
                }
                else
                {
                    inputPlayable.SetTime(normalizedTime * length);
                    inputPlayable.SetTime(normalizedTime * length);
                }
            }
        }
        animMachine.playableGraph.Evaluate(0f);
    }
    public override void UpdateWeights()
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = 0f;
        }
        if (blendX == 0f && blendY > 0f)
            weights[0] = 1f;
        else if (blendX == 0f && blendY < 0f)
            weights[1] = 1f;
        else if (blendX > 0f && blendY == 0f)
            weights[2] = 1f;
        else if (blendX < 0f && blendY == 0f)
            weights[3] = 1f;
        else 
            weights[4] = 1f;
        for (int i = 0; i < statePlayable.GetInputCount(); i++)
        {
            statePlayable.SetInputWeight(i, weights[i]);
        }
    }
}

[Serializable]
public class EightWayBlendState : AnimationStateInfo
{
    [PreviewAnimationClip(nameof(Forward), nameof(properties))] public float preview;
    [ClipOrFBX] public AnimationClip ForwardLeft;
    [ClipOrFBX] public AnimationClip Forward;
    [ClipOrFBX] public AnimationClip ForwardRight;
    [ClipOrFBX] public AnimationClip CenterLeft;
    [ClipOrFBX] public AnimationClip Center;
    [ClipOrFBX] public AnimationClip CenterRight;
    [ClipOrFBX] public AnimationClip BackwardLeft;
    [ClipOrFBX] public AnimationClip Backward;
    [ClipOrFBX] public AnimationClip BackwardRight;
    [Range(-1f, 1f)] public float blendX;
    [Range(-1f, 1f)] public float blendY;
    public float[] weights = new float[9];

    public override void Initialize(AnimationMachine animMachine)
    {
        base.Initialize(animMachine);

        var graph = animMachine.playableGraph;

        statePlayable = AnimationMixerPlayable.Create(graph, 9);
        graph.Connect(AnimationClipPlayable.Create(graph, ForwardLeft), 0, statePlayable, 0);
        graph.Connect(AnimationClipPlayable.Create(graph, Forward), 0, statePlayable, 1);
        graph.Connect(AnimationClipPlayable.Create(graph, ForwardRight), 0, statePlayable, 2);
        graph.Connect(AnimationClipPlayable.Create(graph, CenterLeft), 0, statePlayable, 3);
        graph.Connect(AnimationClipPlayable.Create(graph, Center), 0, statePlayable, 4);
        graph.Connect(AnimationClipPlayable.Create(graph, CenterRight), 0, statePlayable, 5);
        graph.Connect(AnimationClipPlayable.Create(graph, BackwardLeft), 0, statePlayable, 6);
        graph.Connect(AnimationClipPlayable.Create(graph, Backward), 0, statePlayable, 7);
        graph.Connect(AnimationClipPlayable.Create(graph, BackwardRight), 0, statePlayable, 8);

        length = (ForwardLeft.length + Forward.length + ForwardRight.length +
            BackwardLeft.length + Backward.length + BackwardRight.length +
            CenterLeft.length + CenterRight.length) / 8;
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetDuration(playWindow.y * length);
        }
        else
        {
            statePlayable.SetDuration(length);
        }
        for (int i = 0; i < statePlayable.GetInputCount(); i++)
        {
            Playable inputPlayable = statePlayable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty(out property))
                {
                    var playWindow = property.playWindow;
                    inputPlayable.SetDuration(playWindow.y * length);
                }
                else
                {
                    inputPlayable.SetDuration(length);
                }
            }
        }
    }
    public override void ResetState(float normalizedTime = 0f)
    {
        if (TryGetProperty(out PlayWindowProperty property))
        {
            var playWindow = property.playWindow;
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
            statePlayable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            statePlayable.SetTime(normalizedTime * length);
            statePlayable.SetTime(normalizedTime * length);
        }
        for (int i = 0; i < statePlayable.GetInputCount(); i++)
        {
            Playable inputPlayable = statePlayable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty(out property))
                {
                    var playWindow = property.playWindow;
                    inputPlayable.SetTime(playWindow.x * length + normalizedTime * length);
                    inputPlayable.SetTime(playWindow.x * length + normalizedTime * length);
                }
                else
                {
                    inputPlayable.SetTime(normalizedTime * length);
                    inputPlayable.SetTime(normalizedTime * length);
                }
            }
        }
        animMachine.playableGraph.Evaluate(0f);
    }
    public override void UpdateWeights()
    {
        float[] gridX = { -1, 0, 1 }; // Left, Center, Right
        float[] gridY = { 1, 0, -1 }; // Top, Center, Bottom

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                float distX = Mathf.Abs(blendX - gridX[x]);
                float distY = Mathf.Abs(blendY - gridY[y]);
                float weightX = 1 - Mathf.Clamp01(distX);
                float weightY = 1 - Mathf.Clamp01(distY);
                float weight = weightX * weightY;
                int index = y * 3 + x;
                weights[index] = weight;
            }
        }

        // Normalize weights so they sum to 1
        float totalWeight = 0f;
        foreach (float w in weights)
            totalWeight += w;
        if (totalWeight > 0)
        {
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= totalWeight;
        }

        // Set weights for mixer
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int index = y * 3 + x;
                statePlayable.SetInputWeight(index, weights[index]);
            }
        }
    }
}