using System;
using System.Collections;
using System.Collections.Generic;
using Sciphone;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using ZLinq;

public class AnimationMachine : MonoBehaviour
{
    public event Action<float> OnGraphEvaluate;
    public event Action OnActiveStateChanged;

    [TabGroup("Graph Settings")] public string graphName = "Playable Graph";
    [TabGroup("Graph Settings")][Range(0.001f, 3f)] public float timeScale = 1f;
    [TabGroup("Graph Settings")] public bool enableStopMotion;
    [TabGroup("Graph Settings")][Range(1, 60)] public int stopMotionRate = 60;
    [TabGroup("Graph Settings")] public float accumulatedTime = 0f;

    [TabGroup("Root Motion Properties")] public Vector3 rootDeltaPosition;
    [TabGroup("Root Motion Properties")] public Quaternion rootDeltaRotation;
    [TabGroup("Root Motion Properties")] public Vector3 rootLinearVelocity;
    private float currNT;
    private float prevNT;

    [SerializeReference, Polymorphic] public List<AnimationLayerInfo> layers;
    private List<AnimationLayerInfo> rootLayers;
    private List<AnimationLayerInfo> overrideLayers;
    private List<AnimationLayerInfo> additiveLayers;

    internal PlayableGraph playableGraph;
    private AnimationPlayableOutput playableOutput;
    private AnimationLayerMixerPlayable rootPlayable;
    private Animator animator;

    [HideInInspector] public AnimationStateInfo rootState;
    internal AnimationLayerInfo currentRootLayer;

    private Coroutine layersBlendCoroutine;
    private Coroutine playOneShotCoroutine;

    private float[] previousWeights;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        DisableAnimatorGraph();
        Initialize();
        rootState = currentRootLayer.currentState;
    }

    public void Update()
    {
        HandleGraphEvaluation(Time.deltaTime);
        StateUpdateLogic(rootState);
    }

    void DisableAnimatorGraph()
    {
        if (animator)
        {
            animator.playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            animator.playableGraph.Stop();
        }
    }

    public void Initialize()
    {
#if UNITY_EDITOR
        InititalizeRootMotionProperty();
#endif

        playableGraph = PlayableGraph.Create(graphName);
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playableGraph.Play();
        GraphVisualizerClient.Show(playableGraph);

        rootPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 0);
        playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
        playableOutput.SetSourcePlayable(rootPlayable);

        //Sort Layers by type
        rootLayers = new();
        overrideLayers = new();
        additiveLayers = new();
        foreach (var layer in layers)
        {
            if (layer.TryGetProperty(out LayerTypeProperty layerTypeProperty))
            {
                switch (layerTypeProperty.layerType)
                {
                    case AnimationLayerType.Root: rootLayers.Add(layer); break;
                    case AnimationLayerType.Override: overrideLayers.Add(layer); break;
                    case AnimationLayerType.Additive: additiveLayers.Add(layer); break;
                }
            }
            if (layer.TryGetProperty(out AvatarMaskProperty avatarMaskProperty))
            {
                rootPlayable.SetLayerMaskFromAvatarMask((uint)layers.GetIndexOf(layer), avatarMaskProperty.mask);
            }
        }

        foreach (var layer in rootLayers)
        {
            layer.Initialize(this);
            rootPlayable.AddInput(layer.layerPlayable, 0);
        }
        foreach (var layer in overrideLayers)
        {
            layer.Initialize(this);
            rootPlayable.AddInput(layer.layerPlayable, 0);
        }
        foreach (var layer in additiveLayers)
        {
            layer.Initialize(this);
            rootPlayable.AddInput(layer.layerPlayable, 0);
        }

        rootPlayable.SetInputWeight(0, 1);
        currentRootLayer = layers[0];
        previousWeights = new float[rootLayers.Count];
    }

    private void ChangeRootState(AnimationStateInfo newState)
    {
        if (rootState == newState) return;
        
        rootState = newState;

        // Sets root motion parameters on active state change to zero
        currNT = 0f;
        prevNT = 0f;
        EvaluateRootMotionData(1f, currNT, prevNT);

        /*// This calculates root motion parameters for one frame when the active state is changed
        if (enableStopMotion)
        {
            var currNT = stepTime / activeState.length;
            var prevNT = 0f;
            EvaluateRootMotionData(stepTime, currNT, prevNT);
        }
        else
        {
            var currNT = Time.deltaTime * timeScale / activeState.length;
            var prevNT = 0f;
            EvaluateRootMotionData(Time.deltaTime * timeScale, currNT, prevNT);
        }*/

        OnActiveStateChanged?.Invoke();
    }

    private void HandleGraphEvaluation(float dt)
    {
        if (enableStopMotion)
        {
            float frameTime = 1f / stopMotionRate;
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
        currNT = rootState.NormalizedTime();
        if (currNT == 1f)
        {
            if (rootState.TryGetProperty(out LoopProperty propeerty))
            {
                var overflowTime = PrecidctOverflowTime(rootState, prevNT, enableStopMotion ? 1f / stopMotionRate : Time.deltaTime * timeScale);
                currNT = overflowTime / rootState.length;
                rootState.ResetState(currNT);
            }
        }
        EvaluateRootMotionData(evaluationTime, currNT, prevNT);
        prevNT = currNT;
    }

    public void StateUpdateLogic(AnimationStateInfo stateInfo)
    {
        if (stateInfo.TryGetProperty(out PlaybackSpeedProperty property))
        {
            stateInfo.statePlayable.SetSpeed(property.playbackSpeed);
        }

        if (stateInfo.GetType() == typeof(FourWayBlendState))
        {
            ((FourWayBlendState)stateInfo).UpdateWeights();
        }
        else if (stateInfo.GetType() == typeof(EightWayBlendState))
        {
            ((EightWayBlendState)stateInfo).UpdateWeights();
        }

        var realNormalizedTime = stateInfo.NormalizedTime();
        for (int i = 0; i < stateInfo.events.Count; i++)
        {
            stateInfo.events[i].Evaluate(realNormalizedTime);
        }
    }

    private float PrecidctOverflowTime(AnimationStateInfo stateInfo, float prevNT, float graphEvaluationTime)
    {
        return graphEvaluationTime - (1 - prevNT) * stateInfo.length;
    }

    public void PlayActive(string stateName, string layerName)
    {
        if (rootState.TryGetProperty<NotCancellableProperty>(out _))
        {
            return;
        }

        if (playOneShotCoroutine != null)
        {
            rootPlayable.DisconnectInput(rootPlayable.GetInputCount() - 1);
            rootPlayable.SetInputCount(layers.AsValueEnumerable().Count());
            rootState.statePlayable.Destroy();
            StopCoroutine(playOneShotCoroutine);
            playOneShotCoroutine = null;
        }

        AnimationLayerInfo targetLayer = rootLayers.GetLayerInfo(layerName);
        if (targetLayer == null)
        {
            Debug.LogError($"The layer [{layerName}] was not found.");
            return;
        }
        AnimationStateInfo targetState = targetLayer.GetStateInfo(stateName);
        if (targetState == null)
        {
            Debug.LogError($"The state [{stateName}] was not found on the layer [{currentRootLayer.layerName}]");
            return;
        }

        if (currentRootLayer == targetLayer)
        {
            currentRootLayer.ChangeState(targetState);
        }
        else
        {
            if (layersBlendCoroutine != null)
            {
                StopCoroutine(layersBlendCoroutine);
                layersBlendCoroutine = null;
                int a = rootLayers.GetIndexOf(currentRootLayer);
                for (int i = 0; i < rootLayers.Count; i++)
                {
                    if (i < a)
                    {
                        rootPlayable.SetInputWeight(i, 1f);
                    }
                    else if (a == i)
                    {
                        rootPlayable.SetInputWeight(i, 1f);
                    }
                    else if (i > a)
                    {
                        rootPlayable.SetInputWeight(i, 0f);
                    }
                }
            }
            layersBlendCoroutine = StartCoroutine(LayersBlendCoroutine(targetLayer, currentRootLayer));

            currentRootLayer = targetLayer;
            currentRootLayer.ChangeStateImmediate(targetState);
        }

        ChangeRootState(targetState);
    }

    public void PlayOverride(string stateName, string layerName)
    {

    }

    public void PlayAdditive(string stateName, string layerName)
    {

    }

    public void PlayOneShot(AnimationStateInfo state)
    {
        if (rootState.TryGetProperty<NotCancellableProperty>(out _))
        {
            return;
        }

        if (playOneShotCoroutine != null)
        {
            rootPlayable.DisconnectInput(rootPlayable.GetInputCount() - 1);
            rootPlayable.SetInputCount(layers.AsValueEnumerable().Count());
            rootState.statePlayable.Destroy();
            StopCoroutine(playOneShotCoroutine);
            playOneShotCoroutine = null;
        }
        playOneShotCoroutine = StartCoroutine(PlayOneShotCoroutine(state));

        ChangeRootState(state);
    }

    public IEnumerator LayersBlendCoroutine(AnimationLayerInfo targetLayer, AnimationLayerInfo currentLayer)
    {
        for (int i = 0; i < rootLayers.Count; i++)
        {
            previousWeights[i] = rootPlayable.GetInputWeight(i);
        }

        float blendDuration = 0.2f;
        if (targetLayer.currentState.TryGetProperty(out BlendDurationProperty property))
        {
            blendDuration = (property as BlendDurationProperty).blendDuration;
        }

        int a = rootLayers.GetIndexOf(targetLayer);
        int b = rootLayers.GetIndexOf(currentLayer);
        float elapsedTime = 0f;
        while (elapsedTime < blendDuration)
        {
            for (int i = 0; i < rootLayers.Count; i++)
            {
                if (i < a && previousWeights[i] == 0f)
                {
                    rootPlayable.SetInputWeight(i, elapsedTime / blendDuration);
                }
                else if (i == a)
                {
                    if (a > b)
                        rootPlayable.SetInputWeight(i, elapsedTime / blendDuration);
                    else
                        rootPlayable.SetInputWeight(i, 1f);
                }
                else if (i > a && previousWeights[i] == 1f)
                {
                    rootPlayable.SetInputWeight(i, 1f - elapsedTime / blendDuration);
                }
            }
            elapsedTime += Time.deltaTime * timeScale;
            yield return null;
        }

        for (int i = 0; i < rootLayers.Count; i++)
        {
            if (i < a && previousWeights[i] == 0f)
            {
                rootPlayable.SetInputWeight(i, 1f);
            }
            else if (i == a)
            {
                rootPlayable.SetInputWeight(i, 1f);
            }
            else if (i > a && previousWeights[i] == 1f)
            {
                rootPlayable.SetInputWeight(i, 0f);
            }
        }
        rootPlayable.SetInputWeight(a, 1f);
        layersBlendCoroutine = null;
    }

    public IEnumerator PlayOneShotCoroutine(AnimationStateInfo state)
    {
        // Add to graph
        state.Initialize(this);
        state.ResetState();
        rootPlayable.AddInput(state.statePlayable, 0);

        // Blend In
        float blendDuration = 0.2f;
        if (state.TryGetProperty(out BlendDurationProperty property))
        {
            blendDuration = (property as BlendDurationProperty).blendDuration;
        }

        float elapsedTime = 0f;
        while (elapsedTime < blendDuration)
        {
            rootPlayable.SetInputWeight(rootPlayable.GetInputCount() - 1, elapsedTime / blendDuration);
            elapsedTime += Time.deltaTime * timeScale;
            yield return null;
        }
        rootPlayable.SetInputWeight(rootPlayable.GetInputCount() - 1, 1f);

        // Wait until clip ends
        while (state.NormalizedTime() < 1f)
            yield return null;

        // Blend Out
        elapsedTime = 0f;
        while (elapsedTime < blendDuration)
        {
            rootPlayable.SetInputWeight(rootPlayable.GetInputCount() - 1, (1f - elapsedTime / blendDuration));
            elapsedTime += Time.deltaTime * timeScale;
            yield return null;
        }
        rootPlayable.SetInputWeight(rootPlayable.GetInputCount() - 1, 0f);

        // Remove from graph
        rootPlayable.DisconnectInput(rootPlayable.GetInputCount() - 1);
        rootPlayable.SetInputCount(layers.Count);
        state.statePlayable.Destroy();
        playOneShotCoroutine = null;
        ChangeRootState(currentRootLayer.currentState);
    }

    private void EvaluateRootMotionData(float dt, float currNT, float prevNT)
    {
        if (rootState.TryGetProperty(out RootMotionCurvesProperty property))
        {
            var curves = property.rootMotionData;
            rootDeltaPosition = new Vector3(
                GetCurveDelta(curves.rootTX, curves.totalTime, currNT, prevNT),
                GetCurveDelta(curves.rootTY, curves.totalTime, currNT, prevNT),
                GetCurveDelta(curves.rootTZ, curves.totalTime, currNT, prevNT));

            rootDeltaRotation = new Quaternion(
                GetCurveDelta(curves.rootQX, curves.totalTime, currNT, prevNT),
                GetCurveDelta(curves.rootQY, curves.totalTime, currNT, prevNT),
                GetCurveDelta(curves.rootQZ, curves.totalTime, currNT, prevNT),
                GetCurveDelta(curves.rootQW, curves.totalTime, currNT, prevNT));
        }
        else
        {
            rootDeltaPosition = Vector3.zero;
            rootDeltaRotation = Quaternion.identity;
        }

        rootLinearVelocity = rootDeltaPosition / dt;

        if (rootState.TryGetProperty<PlaybackSpeedProperty>(out var speedProperty))
        {
            float speed = (speedProperty as PlaybackSpeedProperty).playbackSpeed;
            rootLinearVelocity *= 1 / speed;
        }
    }

    private float GetCurveDelta(AnimationCurve curve, float totalTime, float currentNormalizedTime, float prevNormalizedTime)
    {
        if (curve.length == 0)
            return 0f;

        if (currentNormalizedTime > prevNormalizedTime)
        {
            float currentDisplacement = curve.Evaluate(currentNormalizedTime * totalTime);
            float prevDisplacement = curve.Evaluate(prevNormalizedTime * totalTime);
            return currentDisplacement - prevDisplacement;
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
                if (state.TryGetProperty<RootMotionCurvesProperty>(out var property))
                {
                    var playWindow = new Vector2(0f, 1f);
                    if (state.TryGetProperty<PlayWindowProperty>(out var normalizedTimes))
                    {
                        playWindow = (normalizedTimes as PlayWindowProperty).playWindow;
                    }

                    if (state is AnimationClipState)
                    {
                        (property as RootMotionCurvesProperty).rootMotionData = (state as AnimationClipState).clip.ExtractRootMotionData(playWindow.x, playWindow.y);
                    }
                    else if (state is FourWayBlendState)
                    {
                        (property as RootMotionCurvesProperty).rootMotionData = (state as FourWayBlendState).Forward.ExtractRootMotionData(playWindow.x, playWindow.y);
                    }
                    else if (state is EightWayBlendState)
                    {
                        (property as RootMotionCurvesProperty).rootMotionData = (state as EightWayBlendState).Forward.ExtractRootMotionData(playWindow.x, playWindow.y);
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
    }
#endif
}
