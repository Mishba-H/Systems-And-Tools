using UnityEngine;
using System;

namespace Sciphone
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PreviewAnimationClipAttribute : PropertyAttribute
    {
        public string clipName;
        public string propertiesListName;

        public PreviewAnimationClipAttribute(string clipName)
        {
            this.clipName = clipName;
            propertiesListName = null;
        }
        public PreviewAnimationClipAttribute(string clipName, string propertiesListName)
        {
            this.clipName = clipName;
            this.propertiesListName = propertiesListName;
        }
    }
}