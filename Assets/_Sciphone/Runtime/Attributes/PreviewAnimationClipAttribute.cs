using UnityEngine;
using System;

namespace Sciphone
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PreviewAnimationClipAttribute : PropertyAttribute
    {
        public string clipName;

        public PreviewAnimationClipAttribute(string clipName)
        {
            this.clipName = clipName;
        }
    }
}