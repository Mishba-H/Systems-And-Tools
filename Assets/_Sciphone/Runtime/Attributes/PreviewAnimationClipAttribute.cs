using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sciphone
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PreviewAnimationClipAttribute : PropertyAttribute
    {
        public string clipName { get; }

        public PreviewAnimationClipAttribute(string clipName)
        {
            this.clipName = clipName;
        }
    }
}