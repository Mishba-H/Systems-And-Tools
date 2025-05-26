using System.Collections.Generic;
using Sciphone;
using UnityEngine;

[CreateAssetMenu]
public class ScriptableAnimationMachineAsset : ScriptableObject
{
    [SerializeReference, Polymorphic] public List<AnimationLayerInfo> layers;
}