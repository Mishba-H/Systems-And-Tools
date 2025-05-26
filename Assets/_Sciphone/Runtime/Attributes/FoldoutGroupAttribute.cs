using UnityEngine;

namespace Sciphone
{
    /// <summary>
    /// Attribute for grouping fields into a foldout in the Unity Inspector.
    /// </summary>
    public class FoldoutGroupAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public FoldoutGroupAttribute(string label = "Foldout")
        {
            Label = label;
        }
    }
}