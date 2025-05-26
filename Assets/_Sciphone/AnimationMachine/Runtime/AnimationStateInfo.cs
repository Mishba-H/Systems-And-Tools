using System;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;
using Sciphone;
using UnityEngine.Animations;

[Serializable]
public abstract class AnimationStateInfo
{
    public string stateName = "DefaultState";
    [SerializeReference, Polymorphic] public List<AnimationStateProperty> properties = new List<AnimationStateProperty>();
    [SerializeReference, Polymorphic] public List<IAnimationEvent> events;
    [HideInInspector] public AnimationMachine animMachine;
    [HideInInspector] public Playable playable;
    [HideInInspector] public float length;
    public virtual void AddToGraph(PlayableGraph graph) { }
    public virtual void ResetState(float normalizedTime = 0f) { }
    public virtual float GetNormalizedTime() { return 0f; }
    public bool TryGetProperty<T>(out AnimationStateProperty property) where T : AnimationStateProperty
    {
        foreach (var prop in properties)
        {
            if (prop == null) continue;
            if (prop.GetType() == typeof(T))
            {
                property = prop;
                return true;
            }
        }
        property = null;
        return false;
    }
}

[Serializable]
public class AnimationClipState : AnimationStateInfo
{
    public AnimationClip clip;
    [PreviewAnimationClip(nameof(clip))] public float preview;
    public override void AddToGraph(PlayableGraph graph)
    {
        playable = AnimationClipPlayable.Create(graph, clip);
        length = clip.length;
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            var playWindow = (property as PlayWindowProperty).playWindow;
            playable.SetDuration(playWindow.y * clip.length);
        }
        else
        {
            playable.SetDuration(clip.length);
        }
    }
    public override void ResetState(float normalizedTime = 0f)
    {
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            var playWindow = (property as PlayWindowProperty).playWindow;
            playable.SetTime(playWindow.x * length + normalizedTime * length);
            playable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            playable.SetTime(normalizedTime * length);
            playable.SetTime(normalizedTime * length);
        }
        animMachine.playableGraph.Evaluate(0f);
    }
    public override float GetNormalizedTime()
    {
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            var playWindow = (Vector2)property.Value;
            var currentTime = playable.GetTime() - playWindow.x * clip.length;
            var duration = (playWindow.y - playWindow.x) * clip.length;
            return (float)(currentTime / duration);
        }
        return (float)(playable.GetTime() / playable.GetDuration());
    }
}

[Serializable]
public class FourWayBlendState : AnimationStateInfo
{
    public AnimationClip Forward;
    public AnimationClip Backward;
    public AnimationClip Right;
    public AnimationClip Left;
    public AnimationClip Center;
    [PreviewAnimationClip(nameof(Forward))] public float preview;
    [Range(-1f, 1f)] public float blendX;
    [Range(-1f, 1f)] public float blendY;
    public float[] weights = new float[5];
    public override void AddToGraph(PlayableGraph graph)
    {
        AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(graph, 5);
        graph.Connect(AnimationClipPlayable.Create(graph, Forward), 0, mixer, 0);
        graph.Connect(AnimationClipPlayable.Create(graph, Backward), 0, mixer, 1);
        graph.Connect(AnimationClipPlayable.Create(graph, Right), 0, mixer, 2);
        graph.Connect(AnimationClipPlayable.Create(graph, Left), 0, mixer, 3);
        graph.Connect(AnimationClipPlayable.Create(graph, Center), 0, mixer, 4);
        playable = mixer;
        length = (Forward.length + Backward.length + Right.length + Left.length) / 4;
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            Vector2 playWindow = (Vector2)property.Value;
            playable.SetDuration(playWindow.y * length);
        }
        else
        {
            playable.SetDuration(length);
        }
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            Playable inputPlayable = playable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty<PlayWindowProperty>(out property))
                {
                    Vector2 playWindow = (Vector2)property.Value;
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
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            Vector2 playWindow = (Vector2)property.Value;
            playable.SetTime(playWindow.x * length + normalizedTime * length);
            playable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            playable.SetTime(normalizedTime * length);
            playable.SetTime(normalizedTime * length);
        }
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            Playable inputPlayable = playable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty<PlayWindowProperty>(out property))
                {
                    Vector2 playWindow = (Vector2)property.Value;
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
    public override float GetNormalizedTime()
    {
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            var playWindow = (Vector2)property.Value;
            var currentTime = playable.GetTime() - playWindow.x * length;
            var duration = (playWindow.y - playWindow.x) * length;
            return (float)(currentTime / duration);
        }
        return (float)(playable.GetTime() / playable.GetDuration());
    }
    public void UpdateWeights()
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
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            playable.SetInputWeight(i, weights[i]);
        }
    }
}

[Serializable]
public class EightWayBlendState : AnimationStateInfo
{
    public AnimationClip ForwardLeft;
    public AnimationClip Forward;
    public AnimationClip ForwardRight;
    public AnimationClip CenterLeft;
    public AnimationClip Center;
    public AnimationClip CenterRight;
    public AnimationClip BackwardLeft;
    public AnimationClip Backward;
    public AnimationClip BackwardRight;
    [PreviewAnimationClip(nameof(Forward))] public float preview;
    [Range(-1f, 1f)] public float blendX;
    [Range(-1f, 1f)] public float blendY;
    public float[] weights = new float[9];
    public override void AddToGraph(PlayableGraph graph)
    {
        AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(graph, 9);
        graph.Connect(AnimationClipPlayable.Create(graph, ForwardLeft), 0, mixer, 0);
        graph.Connect(AnimationClipPlayable.Create(graph, Forward), 0, mixer, 1);
        graph.Connect(AnimationClipPlayable.Create(graph, ForwardRight), 0, mixer, 2);
        graph.Connect(AnimationClipPlayable.Create(graph, CenterLeft), 0, mixer, 3);
        graph.Connect(AnimationClipPlayable.Create(graph, Center), 0, mixer, 4);
        graph.Connect(AnimationClipPlayable.Create(graph, CenterRight), 0, mixer, 5);
        graph.Connect(AnimationClipPlayable.Create(graph, BackwardLeft), 0, mixer, 6);
        graph.Connect(AnimationClipPlayable.Create(graph, Backward), 0, mixer, 7);
        graph.Connect(AnimationClipPlayable.Create(graph, BackwardRight), 0, mixer, 8);
        playable = mixer;

        length = (ForwardLeft.length + Forward.length + ForwardRight.length +
            BackwardLeft.length + Backward.length + BackwardRight.length +
            CenterLeft.length + CenterRight.length) / 8;
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            Vector2 playWindow = (Vector2)property.Value;
            playable.SetDuration(playWindow.y * length);
        }
        else
        {
            playable.SetDuration(length);
        }
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            Playable inputPlayable = playable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty<PlayWindowProperty>(out property))
                {
                    Vector2 playWindow = (Vector2)property.Value;
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
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            Vector2 playWindow = (Vector2)property.Value;
            playable.SetTime(playWindow.x * length + normalizedTime * length);
            playable.SetTime(playWindow.x * length + normalizedTime * length);
        }
        else
        {
            playable.SetTime(normalizedTime * length);
            playable.SetTime(normalizedTime * length);
        }
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            Playable inputPlayable = playable.GetInput(i);
            if (inputPlayable.IsValid())
            {
                if (TryGetProperty<PlayWindowProperty>(out property))
                {
                    Vector2 playWindow = (Vector2)property.Value;
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
    public override float GetNormalizedTime()
    {
        if (TryGetProperty<PlayWindowProperty>(out AnimationStateProperty property))
        {
            var playWindow = (Vector2)property.Value;
            var currentTime = playable.GetTime() - playWindow.x * length;
            var duration = (playWindow.y - playWindow.x) * length;
            return (float)(currentTime / duration);
        }
        return (float)(playable.GetTime() / playable.GetDuration());
    }
    public void UpdateWeights()
    {
        float[] gridX = { -1, 0, 1 }; // Left, Center, Right
        float[] gridY = { 1, 0, -1 }; // Top, Center, Bottom
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                // Calculate the contribution of this cell
                float distX = Mathf.Abs(blendX - gridX[x]);
                float distY = Mathf.Abs(blendY - gridY[y]);
                // Invert distances to get weights (closer points have higher weights)
                float weightX = 1 - Mathf.Clamp01(distX);
                float weightY = 1 - Mathf.Clamp01(distY);
                // Calculate the combined weight for this cell
                float weight = weightX * weightY;
                // Assign the weight to the corresponding grid cell
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
                playable.SetInputWeight(index, weights[index]);
            }
        }
    }
}