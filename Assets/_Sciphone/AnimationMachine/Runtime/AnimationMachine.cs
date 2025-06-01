using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sciphone;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationMachine : MonoBehaviour
{
    public event Action<float> OnGraphEvaluate;
    public event Action OnActiveStateChanged;

    private Animator animator;

    public string graphName = "Playable Graph";

    [Range(0f, 3f)] public float timeScale;

    public bool enableStopMotion;
    [Range(1, 60)] public int frameRate = 60;
    private float frameTime;
    private float accumulatedTime = 0f;

    public PlayableGraph playableGraph;
    public AnimationPlayableOutput playableOutput;
    public AnimationLayerMixerPlayable layerMixer;
    [SerializeReference, Polymorphic] public List<AnimationLayerInfo> layers;

    public AnimationStateInfo activeState;

    [HideInInspector] public AnimationLayerInfo activeLayer;
    public Coroutine layersBlendCoroutine;

    public AnimationStateInfo oneShotState;
    public Coroutine playOneShotCoroutine;

    private float currNT;
    private float prevNT;
    [FoldoutGroup("Root Motion Properties")] public Vector3 rootDeltaPosition;
    [FoldoutGroup("Root Motion Properties")] public Quaternion rootDeltaRotation;
    [FoldoutGroup("Root Motion Properties")] public Vector3 rootLinearVelocity;
    [FoldoutGroup("Root Motion Properties")] public Vector3 rootAngularVelocity;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        DisableAnimatorGraph();
        InitializeGraph();
        activeState = activeLayer.activeState;
    }

    public void Update()
    {
        HandleGraphEvaluation(Time.deltaTime);
        StateUpdateLogic(activeState);
    }

    void DisableAnimatorGraph()
    {
        if (animator != null)
        {
            animator.playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            animator.playableGraph.Stop();
        }
    }

    public void InitializeGraph()
    {
#if UNITY_EDITOR
        InititalizeRootMotionProperty();
#endif

        playableGraph = PlayableGraph.Create(graphName);
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        GraphVisualizerClient.Show(playableGraph);

        layerMixer = AnimationLayerMixerPlayable.Create(playableGraph, 0);
        playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
        playableOutput.SetSourcePlayable(layerMixer);

        foreach (var layer in layers)
        {
            layer.animMachine = this;

            layer.stateMixer = AnimationMixerPlayable.Create(playableGraph, 0);
            layerMixer.AddInput(layer.stateMixer, 0);

            if (layer.TryGetProperty<LayerTypeProperty>(out AnimationLayerProperty prop))
            {
                switch ((AnimationLayerType)prop.Value)
                {
                    case AnimationLayerType.Active:
                        break;
                    case AnimationLayerType.Additive:
                        layerMixer.SetLayerAdditive((uint)layers.GetIndexOf(layer), true);
                        break;
                    case AnimationLayerType.Override:
                        break;
                }
            }
            if (layer.TryGetProperty<AvatarMaskProperty>(out prop))
                layerMixer.SetLayerMaskFromAvatarMask((uint)layers.GetIndexOf(layer), (AvatarMask)prop.Value);

            foreach (var state in layer.states)
            {
                state.animMachine = this;
                state.AddToGraph(playableGraph);
                layer.stateMixer.AddInput(state.playable, 0);
                foreach (var e in state.events)
                {
                    e.GameObject = gameObject;
                }
            }

            layer.stateMixer.SetInputWeight(0, 1);
            layer.activeState = layer.states[0];
        }

        activeLayer = layers[0];
        layerMixer.SetInputWeight(0, 1);
        playableGraph.Play();
    }

    private void ChangeActiveState(AnimationStateInfo newState)
    {
        if (activeState == newState) return;
        
        activeState = newState;


        if (enableStopMotion)
        {
            var currNT = frameTime / activeState.length;
            var prevNT = 0f;
            EvaluateRootMotionData(frameTime, currNT, prevNT);
        }
        else
        {
            var currNT = Time.deltaTime / activeState.length;
            var prevNT = 0f;
            EvaluateRootMotionData(Time.deltaTime, currNT, prevNT);
        }

        OnActiveStateChanged?.Invoke();
    }

    private void HandleGraphEvaluation(float dt)
    {
        if (enableStopMotion)
        {
            frameTime = 1f / frameRate;
            accumulatedTime += dt * timeScale;
            while (accumulatedTime > frameTime)
            {
                accumulatedTime -= frameTime;
                playableGraph.Evaluate(frameTime);
                HandleLooping(frameTime);
                OnGraphEvaluate?.Invoke(frameTime);
            }
        }
        else
        {
            playableGraph.Evaluate(dt * timeScale);
            HandleLooping(dt * timeScale);
            OnGraphEvaluate?.Invoke(dt * timeScale);
        }
    }

    public void HandleLooping(float evaluationTime)
    {
        currNT = activeState.GetNormalizedTime();
        if (currNT == 1f)
        {
            if (activeState.TryGetProperty<LoopProperty>(out _))
            {
                var overflowTime = PrecidctOverflowTime(activeState, prevNT, enableStopMotion ? frameTime : Time.deltaTime * timeScale);
                currNT = overflowTime / activeState.length;
                activeState.ResetState(currNT);
            }
        }
        EvaluateRootMotionData(evaluationTime, currNT, prevNT);
        prevNT = currNT;
    }

    public void StateUpdateLogic(AnimationStateInfo stateInfo)
    {
        if (stateInfo.TryGetProperty<PlaybackSpeedProperty>(out AnimationStateProperty property))
        {
            stateInfo.playable.SetSpeed((float)property.Value);
        }

        if (stateInfo.GetType() == typeof(FourWayBlendState))
        {
            ((FourWayBlendState)stateInfo).UpdateWeights();
        }
        else if (stateInfo.GetType() == typeof(EightWayBlendState))
        {
            ((EightWayBlendState)stateInfo).UpdateWeights();
        }

        var realNormalizedTime = (float)stateInfo.playable.GetTime() / stateInfo.length;
        foreach (IAnimationEvent animEvent in stateInfo.events)
        {
            animEvent.Evaluate(realNormalizedTime);
        }
    }

    private float PrecidctOverflowTime(AnimationStateInfo stateInfo, float prevNT, float graphEvaluationTime)
    {
        return graphEvaluationTime - (1 - prevNT) * stateInfo.length;
    }

    public void PlayActive(string stateName)
    {
        PlayActive(stateName, activeLayer.layerName);
    }

    public void PlayActive(string stateName, string layerName)
    {
        if (activeState.TryGetProperty<NotCancellableProperty>(out _))
        {
            return;
        }

        if (playOneShotCoroutine != null)
        {
            layerMixer.DisconnectInput(layerMixer.GetInputCount() - 1);
            layerMixer.SetInputCount(layers.Count());
            oneShotState.playable.Destroy();
            oneShotState = null;
            StopCoroutine(playOneShotCoroutine);
            playOneShotCoroutine = null;
        }

        AnimationLayerInfo newLayer = layers.GetLayerInfo(layerName);
        if (newLayer == null)
        {
            Debug.LogError($"The layer [{layerName}] was not found.");
            return;
        }
        AnimationStateInfo newState = newLayer.GetStateInfo(stateName);
        if (newState == null)
        {
            Debug.LogError($"The state [{stateName}] was not found on the layer [{activeLayer.layerName}]");
            return;
        }

        currNT = 0f;
        prevNT = 0f;

        if (activeLayer == newLayer)
        {
            activeLayer.ChangeState(newState, this);
        }
        else
        {
            if (layersBlendCoroutine != null)
                StopCoroutine(layersBlendCoroutine);
            var prevLayer = activeLayer;
            activeLayer = newLayer;
            activeLayer.ChangeStateImmediate(newState);
            layersBlendCoroutine = StartCoroutine(LayersBlendCoroutine(newLayer, prevLayer));
        }

        ChangeActiveState(newState);
    }

    public void PlayOneShot(AnimationStateInfo state)
    {
        if (activeState.TryGetProperty<NotCancellableProperty>(out _))
        {
            return;
        }

        currNT = 0f;
        prevNT = 0f;

        if (playOneShotCoroutine != null)
        {
            layerMixer.DisconnectInput(layerMixer.GetInputCount() - 1);
            layerMixer.SetInputCount(layers.Count());
            oneShotState.playable.Destroy();
            oneShotState = null;
            StopCoroutine(playOneShotCoroutine);
            playOneShotCoroutine = null;
        }
        playOneShotCoroutine = StartCoroutine(PlayOneShotCoroutine(state));

        ChangeActiveState(state);
    }

    public IEnumerator LayersBlendCoroutine(AnimationLayerInfo nextLayer, AnimationLayerInfo prevLayer)
    {
        float blendDuration = 0.2f;
        if (nextLayer.activeState.TryGetProperty<BlendDurationProperty>(out AnimationStateProperty property))
        {
            blendDuration = (float)property.Value;
        }
        blendDuration *= 1 / timeScale;

        int i = layers.GetIndexOf(nextLayer);
        int j = layers.GetIndexOf(prevLayer);
        if (i > j)
        {
            float elapsedTime = 0f;
            while (elapsedTime < blendDuration)
            {
                layerMixer.SetInputWeight(i, elapsedTime / blendDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            layerMixer.SetInputWeight(i, 1);
        }
        else
        {
            float elapsedTime = 0f;
            while (elapsedTime < blendDuration)
            {
                layerMixer.SetInputWeight(j, 1 - elapsedTime / blendDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            layerMixer.SetInputWeight(j, 0);
        }
        layersBlendCoroutine = null;
    }

    public IEnumerator PlayOneShotCoroutine(AnimationStateInfo state)
    {
        // Add to graph
        oneShotState = state;
        state.AddToGraph(playableGraph);
        state.ResetState();
        layerMixer.AddInput(state.playable, 0);

        // Blend In
        float blendDuration = 0.2f;
        if (state.TryGetProperty<BlendDurationProperty>(out AnimationStateProperty property))
        {
            blendDuration = (float)property.Value;
        }
        blendDuration *= 1 / timeScale;

        float elapsedTime = 0f;
        while (elapsedTime < blendDuration)
        {
            layerMixer.SetInputWeight(layerMixer.GetInputCount() - 1, elapsedTime / blendDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        layerMixer.SetInputWeight(layerMixer.GetInputCount() - 1, 1);

        // Wait until clip ends
        while (state.GetNormalizedTime() < 1f)
            yield return null;

        // Blend Out
        elapsedTime = 0f;
        while (elapsedTime < blendDuration)
        {
            layerMixer.SetInputWeight(layerMixer.GetInputCount() - 1, (1 - elapsedTime / blendDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        layerMixer.SetInputWeight(layerMixer.GetInputCount() - 1, 0);

        // Remove from graph
        layerMixer.DisconnectInput(layerMixer.GetInputCount() - 1);
        layerMixer.SetInputCount(layers.Count);
        state.playable.Destroy();
        oneShotState = null;
        playOneShotCoroutine = null;
        ChangeActiveState(activeLayer.activeState);
    }

    private void EvaluateRootMotionData(float dt, float currNT, float prevNT)
    {
        if (activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            var curves = (RootMotionData)property.Value;
            rootDeltaPosition = new Vector3(
                GetCurveDelta(curves.rootTX, currNT, prevNT),
                GetCurveDelta(curves.rootTY, currNT, prevNT),
                GetCurveDelta(curves.rootTZ, currNT, prevNT));

            rootDeltaRotation = new Quaternion(
                GetCurveDelta(curves.rootQX, currNT, prevNT),
                GetCurveDelta(curves.rootQY, currNT, prevNT),
                GetCurveDelta(curves.rootQZ, currNT, prevNT),
                GetCurveDelta(curves.rootQW, currNT, prevNT));
        }
        else
        {
            rootDeltaPosition = Vector3.zero;
            rootDeltaRotation = Quaternion.identity;
        }

        rootLinearVelocity = rootDeltaPosition / dt;

        rootDeltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis); 
        if (angleInDegrees > 180f)
            angleInDegrees -= 360f;
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        rootAngularVelocity = axis * angleInRadians / dt;
    }

    private float GetCurveDelta(AnimationCurve curve, float currentNormalizedTime, float prevNormalizedTime)
    {
        if (curve.length == 0)
            return 0f;

        float totalTime = curve.keys[curve.length - 1].time;

        if (currentNormalizedTime > prevNormalizedTime)
        {
            float currentDisplacement = curve.Evaluate(currentNormalizedTime * totalTime);
            float prevDisplacement = curve.Evaluate(prevNormalizedTime * totalTime);
            return (currentDisplacement - prevDisplacement);
        }
        else if (currentNormalizedTime < prevNormalizedTime)
        {
            // Case of Looping
            return (curve.Evaluate(currentNormalizedTime * totalTime) - curve.Evaluate(0f)
                + curve.Evaluate(totalTime) - curve.Evaluate(prevNormalizedTime * totalTime));
        }
        else
        {
            return 0f;
        }
    }

    public void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

#if UNITY_EDITOR
    [Button(nameof(InititalizeRootMotionProperty))]
    public void InititalizeRootMotionProperty()
    {
        foreach (var layer in layers)
        {
            foreach (var state in layer.states)
            {
                if (state.TryGetProperty<RootMotionCurvesProperty>(out var curve))
                {
                    var playWindow = new Vector2(0f, 1f);
                    if (state.TryGetProperty<PlayWindowProperty>(out var normalizedTimes))
                    {
                        playWindow = (normalizedTimes as PlayWindowProperty).playWindow;
                    }

                    if (state is AnimationClipState)
                    {
                        curve.Value = (state as AnimationClipState).clip.ExtractRootMotionData(playWindow.x, playWindow.y);
                    }
                    else if (state is FourWayBlendState)
                    {
                        curve.Value = (state as FourWayBlendState).Forward.ExtractRootMotionData(playWindow.x, playWindow.y);
                    }
                    else if (state is EightWayBlendState)
                    {
                        curve.Value = (state as EightWayBlendState).Forward.ExtractRootMotionData(playWindow.x, playWindow.y);
                    }
                }
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    [Button(nameof(ImportLayersFromAsset))]
    public void ImportLayersFromAsset(ScriptableAnimationMachineAsset asset)
    {
        this.layers = asset.layers;
    }

    [Button(nameof(ExportLayersToAsset))]
    public void ExportLayersToAsset(ScriptableAnimationMachineAsset asset)
    {
        asset.layers = this.layers;

        UnityEditor.EditorUtility.SetDirty(asset);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}
