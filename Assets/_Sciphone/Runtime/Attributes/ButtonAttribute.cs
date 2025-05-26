using UnityEngine;
using System;

namespace Sciphone
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public ButtonAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}