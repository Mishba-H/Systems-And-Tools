using UnityEngine;

namespace Sciphone
{
    /// <summary>
    /// Attribute for grouping fields into tabs in the Unity Inspector.
    /// </summary>
    public class TabGroupAttribute : PropertyAttribute
    {
        public string TabName;

        public TabGroupAttribute(string tabName = "Tab")
        {
            TabName = tabName;
        }
    }
}