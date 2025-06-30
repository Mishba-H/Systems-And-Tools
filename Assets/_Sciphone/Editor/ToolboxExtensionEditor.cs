using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using Toolbox.Editor;

namespace Sciphone
{
    [CustomEditor(typeof(Object), true)]
    public class ToolboxExtensionEditor : ToolboxEditor
    {
        public class SerializedPropertyComparer : IEqualityComparer<SerializedProperty>
        {
            public bool Equals(SerializedProperty x, SerializedProperty y)
            {
                if (x == null || y == null) return false;
                return x.propertyPath == y.propertyPath;
            }
            public int GetHashCode(SerializedProperty obj)
            {
                return obj.propertyPath.GetHashCode();
            }
        }

        private SerializedPropertyComparer comparer;
        private List<SerializedProperty> allProperties;
        private List<SerializedProperty> defaultProperties;

        private Dictionary<string, List<SerializedProperty>> tabGroups;
        private string[] tabNames;
        private static Dictionary<string, int> selectedTabIndices;
        private List<string> drawnTabs;

        private Dictionary<string, List<SerializedProperty>> foldoutGroups;
        private static Dictionary<string, bool> foldoutStates;
        private List<string> drawnFoldouts;

        private static Dictionary<MethodInfo, bool> methodFoldouts = new Dictionary<MethodInfo, bool>();
        private static Dictionary<MethodInfo, object[]> methodParameters = new Dictionary<MethodInfo, object[]>();

        private void OnEnable()
        {
            if (target is not MonoBehaviour && target is not ScriptableObject)
            {
                return;
            }

            comparer = new SerializedPropertyComparer();
            allProperties = new List<SerializedProperty>();
            defaultProperties = new List<SerializedProperty>();

            tabGroups = new Dictionary<string, List<SerializedProperty>>();
            if (selectedTabIndices == null)
                selectedTabIndices = new ();
            drawnTabs = new List<string>();

            foldoutGroups = new Dictionary<string, List<SerializedProperty>>();
            if (foldoutStates == null)
                foldoutStates = new ();
            drawnFoldouts = new List<string>();

            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                allProperties.Add(property.Copy());

                var tabAttr = GetFieldAttribute<TabGroupAttribute>(property);
                if (tabAttr != null)
                {
                    if (!tabGroups.ContainsKey(tabAttr.TabName))
                        tabGroups[tabAttr.TabName] = new List<SerializedProperty>();
                    tabGroups[tabAttr.TabName].Add(property.Copy());
                }

                var foldoutAttr = GetFieldAttribute<FoldoutGroupAttribute>(property);
                if (foldoutAttr != null)
                {
                    if (!foldoutGroups.ContainsKey(foldoutAttr.Label))
                    {
                        foldoutGroups[foldoutAttr.Label] = new List<SerializedProperty>();
                        string foldoutId = GetFoldoutGroupID(foldoutAttr.Label);
                        if (!foldoutStates.ContainsKey(foldoutId))
                            foldoutStates[foldoutId] = true;
                    }
                    foldoutGroups[foldoutAttr.Label].Add(property.Copy());
                }

                if (tabAttr == null && foldoutAttr == null)
                {
                    defaultProperties.Add(property.Copy());
                }
            }

            if (tabGroups.Keys.Count > 0)
            {
                tabNames = new string[tabGroups.Keys.Count];
                tabGroups.Keys.CopyTo(tabNames, 0);
            }
        }

        public override void DrawCustomInspector()
        {
            if (!(target is MonoBehaviour) && !(target is ScriptableObject))
            {
                return;
            }

            serializedObject.Update();
            drawnTabs.Clear();
            drawnFoldouts.Clear();
            int tabGroupIndex = 0;

            for (int i = 0; i < allProperties.Count; i++)
            {
                var property = allProperties[i];
                if (defaultProperties.Contains(property, comparer))
                {
                    ToolboxEditorGui.DrawToolboxProperty(property);
                }
                else if (IsPropertyInTabGroups(property))
                {
                    HashSet<string> groupTabs = new HashSet<string>();
                    while (IsPropertyInTabGroups(property))
                    {
                        var tabName = GetFieldAttribute<TabGroupAttribute>(property).TabName;
                        groupTabs.Add(tabName);
                        i++;
                        if (i == allProperties.Count) break;
                        else property = allProperties[i];
                    }
                    i--;
                    DrawTabGroup(groupTabs.ToArray());
                    foreach (string tab in groupTabs)
                        drawnTabs.Add(tab);
                    tabGroupIndex++;
                }
                else if (IsPropertyInFoldoutGroups(property))
                {
                    string label = GetFieldAttribute<FoldoutGroupAttribute>(property).Label;
                    DrawFoldoutGroup(label, foldoutGroups[label]);
                    while (IsPropertyInFoldoutGroups(property))
                    {
                        i++;
                        if (i == allProperties.Count) break;
                        else property = allProperties[i];
                    }
                    i--;
                    drawnFoldouts.Add(label);
                }
            }

            // Handle ButtonAttribute methods
            Object targetObject = target;
            MethodInfo[] methods = targetObject.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute != null)
                {
                    DrawButtonWithParameters(targetObject, method);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTabGroup(string[] tabNames)
        {
            tabNames = tabNames.Except(drawnTabs).ToArray();
            if (tabNames.Length == 0) return;

            string groupId = GetTabGroupID(tabNames); // Group ID based on all tab names
            if (!selectedTabIndices.TryGetValue(groupId, out int selected))
                selected = 0;

            selected = GUILayout.Toolbar(selected, tabNames);
            selectedTabIndices[groupId] = selected;

            EditorGUILayout.Space();
            foreach (var property in tabGroups[tabNames[selected]])
            {
                ToolboxEditorGui.DrawToolboxProperty(property);
            }
            DrawHorizontalLine(Color.grey);
            EditorGUILayout.Space();
        }

        private void DrawFoldoutGroup(string label, List<SerializedProperty> properties)
        {
            if (drawnFoldouts.Contains(label)) return;

            string foldoutId = GetFoldoutGroupID(label);
            if (!foldoutStates.TryGetValue(foldoutId, out bool state))
                state = true;

            foldoutStates[foldoutId] = EditorGUILayout.Foldout(state, label, true);
            if (foldoutStates[foldoutId])
            {
                foreach (var property in properties)
                {
                    ToolboxEditorGui.DrawToolboxProperty(property);
                }
            }
        }

        private string GetTabGroupID(string[] tabNames)
        {
            string combined = string.Join("_", tabNames.OrderBy(t => t));
            return $"{target.GetInstanceID()}_TabGroup_{combined}";
        }

        private string GetFoldoutGroupID(string label)
        {
            return $"{target.GetInstanceID()}_Foldout_{label}";
        }

        private bool IsPropertyInTabGroups(SerializedProperty property)
        {
            foreach (var list in tabGroups.Values)
            {
                if (list.Contains(property, comparer))
                    return true;
            }
            return false;
        }

        private bool IsPropertyInFoldoutGroups(SerializedProperty property)
        {
            foreach (var list in foldoutGroups.Values)
            {
                if (list.Contains(property, comparer))
                    return true;
            }
            return false;
        }

        private T GetFieldAttribute<T>(SerializedProperty property) where T : PropertyAttribute
        {
            var targetType = serializedObject.targetObject.GetType();
            var field = targetType.GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var attributes = field.GetCustomAttributes(typeof(T), true);
                if (attributes.Length > 0)
                    return (T)attributes[0];
            }
            return null;
        }

        private void DrawButtonWithParameters(Object targetObject, MethodInfo method)
        {
            ParameterInfo[] parametersInfo = method.GetParameters();
            bool hasParameters = parametersInfo.Length > 0;

            GUILayout.Space(5f);
            Rect totalRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

            if (hasParameters)
            {
                if (!methodFoldouts.ContainsKey(method))
                {
                    methodFoldouts[method] = false;
                }

                // Define layout
                float foldoutWidth = 45f;
                float spacing = 12f;
                float buttonWidth = totalRect.width - foldoutWidth - spacing;

                // Draw the button
                Rect buttonRect = new Rect(totalRect.x, totalRect.y, buttonWidth, totalRect.height);
                if (GUI.Button(buttonRect, method.Name))
                {
                    object[] parameters = methodParameters.ContainsKey(method) ? methodParameters[method] : null;
                    method.Invoke(targetObject, parameters);
                }

                // Draw the foldout
                Rect foldoutRect = new Rect(totalRect.x + buttonWidth + spacing, totalRect.y, foldoutWidth, totalRect.height);
                methodFoldouts[method] = EditorGUI.Foldout(
                    foldoutRect,
                    methodFoldouts[method],
                    methodFoldouts[method] ? "Hide" : "Show",
                    true
                );

                // Draw parameter fields if foldout is open
                if (methodFoldouts[method])
                {
                    if (!methodParameters.ContainsKey(method))
                    {
                        methodParameters[method] = new object[parametersInfo.Length];
                    }

                    EditorGUI.indentLevel++;
                    for (int i = 0; i < parametersInfo.Length; i++)
                    {
                        ParameterInfo param = parametersInfo[i];
                        object currentValue = methodParameters[method][i];
                        methodParameters[method][i] = DrawParameterField(param, currentValue);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // No parameters: draw button directly
                if (GUI.Button(totalRect, method.Name))
                {
                    method.Invoke(targetObject, null);
                }
            }
        }

        private object DrawParameterField(ParameterInfo param, object currentValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(param.Name), GUILayout.MaxWidth(100));

            object newValue = currentValue;
            if (param.ParameterType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(currentValue != null ? (int)currentValue : 0);
            }
            else if (param.ParameterType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(currentValue != null ? (float)currentValue : 0f);
            }
            else if (param.ParameterType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(currentValue != null ? (string)currentValue : "");
            }
            else if (param.ParameterType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(currentValue != null ? (bool)currentValue : false);
            }
            else if (param.ParameterType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(currentValue != null ? (System.Enum)currentValue : (System.Enum)System.Enum.GetValues(param.ParameterType).GetValue(0));
            }
            else if (typeof(Object).IsAssignableFrom(param.ParameterType))
            {
                newValue = EditorGUILayout.ObjectField(currentValue != null ? (Object)currentValue : null, param.ParameterType, true);
            }
            else
            {
                EditorGUILayout.LabelField($"Unsupported Type: {param.ParameterType}");
            }

            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public void DrawHorizontalLine(Color color, float thickness = 1, float padding = 10)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += padding * 0.5f;

            rect.x -= 2f;                       // optional: stretch left a bit
            rect.width += 6f;                  // optional: stretch right a bit
            rect.width -= 5f;       // apply right padding

            EditorGUI.DrawRect(rect, color);
        }
    }
}
#endif