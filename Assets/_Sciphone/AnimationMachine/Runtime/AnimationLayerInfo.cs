using System;
using System.Collections.Generic;
using UnityEngine;
using Sciphone;
using UnityEngine.Animations;
using System.Collections;
using UnityEngine.Playables;
using UnityEditor;

[Serializable]
public class AnimationLayerInfo
{
    public string layerName = "DefaultLayer";
    [HideInInspector] public AnimationMachine animMachine;
    [SerializeReference, Polymorphic] public List<AnimationLayerProperty> properties = new List<AnimationLayerProperty>();
    [SerializeReference, Polymorphic] public List<AnimationStateInfo> states;
    public AnimationMixerPlayable stateMixer;

    [HideInInspector] public AnimationStateInfo activeState;
    [HideInInspector] public Coroutine statesBlendCoroutine;
    public void ChangeStateImmediate(AnimationStateInfo newState)
    {
        if (activeState == newState)
        {
            activeState.ResetState();
            return;
        }
        activeState = newState;
        for (int i = 0; i < stateMixer.GetInputCount(); i++)
        {
            if (i == ((Playable)stateMixer).GetIndexOf(newState.playable))
            {
                stateMixer.SetInputWeight(i, 1);
                continue;
            }
            stateMixer.SetInputWeight(i, 0);
        }
        activeState.ResetState();
    }
    public void ChangeState(AnimationStateInfo newState, MonoBehaviour activeMonoBehaviour)
    {
        if (activeState == newState)
        {
            activeState.ResetState();
            return;
        }
        if (statesBlendCoroutine != null)
        {
            activeMonoBehaviour.StopCoroutine(statesBlendCoroutine);
            statesBlendCoroutine = null;
            int stateIndex = ((Playable)stateMixer).GetIndexOf(activeState.playable);
            ((Playable)stateMixer).NormalizeWeights(stateIndex);
        }
        activeState = newState;
        statesBlendCoroutine = activeMonoBehaviour.StartCoroutine(BlendStates(newState, activeMonoBehaviour));
    }
    public IEnumerator BlendStates(AnimationStateInfo stateInfo, MonoBehaviour activeMonoBehaviour)
    {
        stateInfo.ResetState();

        float[] previousWeights = new float[stateMixer.GetInputCount()];
        for (int i = 0; i < stateMixer.GetInputCount(); i++)
        {
            previousWeights[i] = stateMixer.GetInputWeight(i);
        }

        float blendDuration = 0.2f;
        if (stateInfo.TryGetProperty<BlendDurationProperty>(out AnimationStateProperty property))
        {
            blendDuration = (property as BlendDurationProperty).blendDuration;
        }

        float elapsedTime = 0f;
        int stateIndex = ((Playable)stateMixer).GetIndexOf(stateInfo.playable);
        while (elapsedTime <= blendDuration)
        {
            for (int i = 0; i < stateMixer.GetInputCount(); i++)
            {
                if (i == stateIndex)
                {
                    stateMixer.SetInputWeight(i, elapsedTime / blendDuration);
                }
                else
                {
                    stateMixer.SetInputWeight(i, previousWeights[i] * (1 - elapsedTime / blendDuration));
                }
            }
            ((Playable)stateMixer).NormalizeWeights(stateIndex);
            elapsedTime += Time.deltaTime * animMachine.timeScale;
            yield return null;
        }

        for (int i = 0; i < stateMixer.GetInputCount(); i++)
        {
            if (i == ((Playable)stateMixer).GetIndexOf(stateInfo.playable))
            {
                stateMixer.SetInputWeight(i, 1);
                continue;
            }
            stateMixer.SetInputWeight(i, 0);
        }
        statesBlendCoroutine = null;
    }
    public bool TryGetProperty<T>(out AnimationLayerProperty property) where T : AnimationLayerProperty
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
