using System;
using System.Collections.Generic;
using UnityEngine;
using Sciphone;
using UnityEngine.Animations;
using System.Collections;
using UnityEngine.Playables;
using static UnityEditor.Experimental.GraphView.GraphView;

[Serializable]
public class AnimationLayerInfo
{
    public string layerName = "DefaultLayer";
    [SerializeReference, Polymorphic] public List<AnimationLayerProperty> properties = new List<AnimationLayerProperty>();
    [SerializeReference, Polymorphic] public List<AnimationStateInfo> states;

    internal AnimationMachine animMachine;
    internal Playable layerPlayable;
    internal AnimationStateInfo currentState;
    private Coroutine statesBlendCoroutine;
    private float[] previousWeights;

    public void Initialize(AnimationMachine animMachine)
    {
        this.animMachine = animMachine;
        layerPlayable = AnimationMixerPlayable.Create(animMachine.playableGraph, 0);

        foreach (var state in states)
        {
            state.Initialize(animMachine);
            layerPlayable.AddInput(state.statePlayable, 0);
        }

        layerPlayable.SetInputWeight(0, 1);
        currentState = states[0];
        previousWeights = new float[layerPlayable.GetInputCount()];
    }
    public void ChangeStateImmediate(AnimationStateInfo targetState)
    {
        if (currentState == targetState)
        {
            currentState.ResetState();
            return;
        }
        currentState = targetState;
        for (int i = 0; i < layerPlayable.GetInputCount(); i++)
        {
            if (i == layerPlayable.GetIndexOf(targetState.statePlayable))
            {
                layerPlayable.SetInputWeight(i, 1);
                continue;
            }
            layerPlayable.SetInputWeight(i, 0);
        }
        currentState.ResetState();
    }
    public void ChangeState(AnimationStateInfo targetState)
    {
        if (currentState == targetState)
        {
            currentState.ResetState();
            return;
        }

        if (statesBlendCoroutine != null)
        {
            animMachine.StopCoroutine(statesBlendCoroutine);
            statesBlendCoroutine = null;
            int stateIndex = layerPlayable.GetIndexOf(currentState.statePlayable);
            layerPlayable.NormalizeWeights(stateIndex);
        }
        currentState = targetState;
        statesBlendCoroutine = animMachine.StartCoroutine(BlendStates(targetState));
    }
    public IEnumerator BlendStates(AnimationStateInfo targetState)
    {
        targetState.ResetState();

        for (int i = 0; i < layerPlayable.GetInputCount(); i++)
        {
            previousWeights[i] = layerPlayable.GetInputWeight(i);
        }

        float blendDuration = 0.2f;
        if (targetState.TryGetProperty(out BlendDurationProperty property))
        {
            blendDuration = property.blendDuration;
        }

        float elapsedTime = 0f;
        int stateIndex = layerPlayable.GetIndexOf(targetState.statePlayable);
        while (elapsedTime < blendDuration)
        {
            for (int i = 0; i < layerPlayable.GetInputCount(); i++)
            {
                if (i == stateIndex)
                {
                    layerPlayable.SetInputWeight(i, elapsedTime / blendDuration);
                }
                else
                {
                    layerPlayable.SetInputWeight(i, previousWeights[i] * (1 - elapsedTime / blendDuration));
                }
            }
            layerPlayable.NormalizeWeights(stateIndex);
            elapsedTime += Time.deltaTime * animMachine.timeScale;
            yield return null;
        }

        for (int i = 0; i < layerPlayable.GetInputCount(); i++)
        {
            if (i == layerPlayable.GetIndexOf(targetState.statePlayable))
            {
                layerPlayable.SetInputWeight(i, 1);
                continue;
            }
            layerPlayable.SetInputWeight(i, 0);
        }
        statesBlendCoroutine = null;
    }
    public bool TryGetProperty<T>(out T property) where T : AnimationLayerProperty
    {
        foreach (var prop in properties)
        {
            if (prop == null) continue;
            if (prop.GetType() == typeof(T))
            {
                property = prop as T;
                return true;
            }
        }
        property = null;
        return false;
    }
}
