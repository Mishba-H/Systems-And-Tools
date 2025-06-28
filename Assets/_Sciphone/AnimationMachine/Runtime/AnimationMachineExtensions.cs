using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;
using UnityEditor;
using ZLinq;

public static class AnimationMachineExtensions
{
    public static AnimationLayerInfo GetLayerInfo(this List<AnimationLayerInfo> layers, string layerName)
    {
        return layers.AsValueEnumerable().FirstOrDefault(t => t.layerName == layerName);
    }
    public static AnimationStateInfo GetStateInfo(this AnimationLayerInfo layer, string stateName)
    {
        return layer.states.AsValueEnumerable().FirstOrDefault(t => t.stateName == stateName);
    }
    public static int GetIndexOf(this List<AnimationLayerInfo> layers, AnimationLayerInfo layer)
    {
        return layers.IndexOf(layer);
    }
    public static int GetIndexOf(this AnimationLayerInfo layer, AnimationStateInfo state)
    {
        return layer.states.IndexOf(state);
    }
    public static int GetIndexOf(this Playable mixer, Playable playable)
    {
        for (int i = 0; i < mixer.GetInputCount(); i++)
        {
            if (playable.Equals(mixer.GetInput(i)))
                return i;
        }
        return -1;
    }
    public static void NormalizeWeights(this Playable playable, int index)
    {
        float weightSum = 0f;
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            weightSum += playable.GetInputWeight(i);
        }

        if (weightSum == 0f)
        {
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                if (i == index)
                    playable.SetInputWeight(i, 1f);
                else
                    playable.SetInputWeight(i, 0f);
            }
            return;
        }

        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            playable.SetInputWeight(i, playable.GetInputWeight(i) / weightSum);
        }
    }
#if UNITY_EDITOR
    public static RootMotionData ExtractRootMotionData(this AnimationClip clip, float normalizedStart = 0f, float normalizedEnd = 1f)
    {
        var data = new RootMotionData();

        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding binding in curveBindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            switch (binding.propertyName)
            {
                case "RootT.x":
                    data.rootTX = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootT.y":
                    data.rootTY = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootT.z":
                    data.rootTZ = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootQ.x":
                    data.rootQX = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootQ.y":
                    data.rootQY = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootQ.z":
                    data.rootQW = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
                case "RootQ.w":
                    data.rootQZ = curve.CropAnimationCurve(normalizedStart, normalizedEnd);
                    break;
            }
        }
        data.totalTime = data.rootTX.keys[data.rootTX.length - 1].time;
        return data;
    }
#endif
    public static AnimationCurve CropAnimationCurve(this AnimationCurve curve, float normalizedStart = 0f, float normalizedEnd = 1f)
    {
        float startTime = curve.keys[0].time;
        float endTime = curve.keys[curve.length - 1].time;

        float cropStartTime = Mathf.Lerp(startTime, endTime, normalizedStart);
        float cropEndTime = Mathf.Lerp(startTime, endTime, normalizedEnd);

        AnimationCurve croppedCurve = new AnimationCurve();

        // Compute tangents at start and end points
        float startValue = curve.Evaluate(cropStartTime);
        float endValue = curve.Evaluate(cropEndTime);

        float startTangent = curve.GetTangent(cropStartTime);
        float endTangent = curve.GetTangent(cropEndTime);

        // Add start keyframe with correct tangent
        croppedCurve.AddKey(new Keyframe(0f, startValue, startTangent, startTangent));

        // Add intermediate keyframes (preserving original ones)
        foreach (Keyframe key in curve.keys)
        {
            if (key.time > cropStartTime && key.time < cropEndTime)
            {
                float adjustedTime = key.time - cropStartTime;
                Keyframe adjustedKey = new Keyframe(adjustedTime, key.value, key.inTangent, key.outTangent);
                croppedCurve.AddKey(adjustedKey);
            }
        }

        // Add end keyframe with correct tangent
        float endAdjustedTime = cropEndTime - cropStartTime;
        croppedCurve.AddKey(new Keyframe(endAdjustedTime, endValue, endTangent, endTangent));

        return croppedCurve;
    }
    private static float GetTangent(this AnimationCurve curve, float time)
    {
        float delta = 0.01f; // Small step to approximate derivative
        float valueBefore = curve.Evaluate(time - delta);
        float valueAfter = curve.Evaluate(time + delta);

        return (valueAfter - valueBefore) / (2 * delta);
    }
    public static float GetMaxValue(this AnimationCurve curve, float precision = 0.01f)
    {
        if (curve == null || curve.length == 0)
            return 0f; // Return a very low value if the curve is empty

        float startTime = curve.keys[0].time;
        float endTime = curve.keys[curve.length - 1].time;
        float maxValue = float.MinValue;

        for (float t = startTime; t <= endTime; t += precision)
        {
            float value = curve.Evaluate(t);
            if (value > maxValue)
                maxValue = value;
        }

        return maxValue;
    }
    public static Vector3 EvaluateScaleFactor(RootMotionCurvesProperty rootMotionProperty, ScaleModeProperty scaleMode, Vector3 targetValue)
    {
        var curves = rootMotionProperty.rootMotionData;
        float totalTime = curves.totalTime;

        float GetScale(AnimationCurve curve, ScaleMode mode, float targetValue)
        {
            return mode switch
            {
                ScaleMode.None => 1f,
                ScaleMode.Zero => 0f,
                ScaleMode.AvgValue => targetValue / ((curve.Evaluate(totalTime) - curve.Evaluate(0f)) / totalTime),
                ScaleMode.MaxValue => targetValue / (GetMaxValue(curve) - curve.Evaluate(0f)),
                ScaleMode.TotalValue => targetValue / (curve.Evaluate(totalTime) - curve.Evaluate(0f)),
                _ => 1f
            };
        }

        return new Vector3(
        GetScale(curves.rootTX, scaleMode.scaleModeX, targetValue.x),
        GetScale(curves.rootTY, scaleMode.scaleModeY, targetValue.y),
        GetScale(curves.rootTZ, scaleMode.scaleModeZ, targetValue.z));
    }
}