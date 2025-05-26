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
        private List<int> selectedTabIndices = new List<int>();
        private List<string> drawnTabs;

        private Dictionary<string, List<SerializedProperty>> foldoutGroups;
        private Dictionary<string, bool> foldoutStates;
        private List<string> drawnFoldouts;

        private Dictionary<MethodInfo, object[]> methodParameters = new Dictionary<MethodInfo, object[]>();

        private bool IsPropertyInTabGroups(SerializedProperty property)
        {
            foreach (var list in tabGroups.Values)
            {
                if (list.Contains(property, comparer))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPropertyInFoldoutGroups(SerializedProperty property)
        {
            foreach (var list in foldoutGroups.Values)
            {
                if (list.Contains(property, comparer))
                {
                    return true;
                }
            }
            return false;
        }

        private void DrawTabGroup(string[] tabNames, int tabGroupIndex)
        {
            tabNames = tabNames.Except(drawnTabs).ToArray();
            if (tabNames.Length == 0) return;

            if (selectedTabIndices.Count - 1 < tabGroupIndex)
            {
                selectedTabIndices.Add(0);
            }
            // Draw tab group inside a box
            EditorGUILayout.BeginVertical("box");
            // Draw tab selection
            selectedTabIndices[tabGroupIndex] = GUILayout.Toolbar(selectedTabIndices[tabGroupIndex], tabNames);
            // Draw properties for the selected tab using Toolbox's property drawer
            EditorGUILayout.Space();
            foreach (var property in tabGroups[tabNames[selectedTabIndices[tabGroupIndex]]])
            {
                ToolboxEditorGui.DrawToolboxProperty(property);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawFoldoutGroup(string key, List<SerializedProperty> value)
        {
            if (drawnFoldouts.Contains(key)) return;

            EditorGUILayout.BeginVertical("box");
            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], key, true);
            if (foldoutStates[key])
            {
                foreach (var property in value)
                {
                    ToolboxEditorGui.DrawToolboxProperty(property);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void OnEnable()
        {
            if (!(target is MonoBehaviour) && !(target is ScriptableObject))
            {
                return;
            }

            comparer = new SerializedPropertyComparer();
            allProperties = new List<SerializedProperty>();

            defaultProperties = new List<SerializedProperty>();

            tabGroups = new Dictionary<string, List<SerializedProperty>>();
            drawnTabs = new List<string>();

            foldoutGroups = new Dictionary<string, List<SerializedProperty>>();
            foldoutStates = new Dictionary<string, bool>();
            drawnFoldouts = new List<string>();

            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // Skip the script field
            while (property.NextVisible(false))
            {
                allProperties.Add(property.Copy());

                var tabGroupAttribute = GetFieldAttribute<TabGroupAttribute>(property);
                if (tabGroupAttribute != null)
                {
                    if (!tabGroups.ContainsKey(tabGroupAttribute.TabName))
                    {
                        tabGroups[tabGroupAttribute.TabName] = new List<SerializedProperty>();
                    }
                    tabGroups[tabGroupAttribute.TabName].Add(property.Copy());
                }

                var foldoutGroupAttribute = GetFieldAttribute<FoldoutGroupAttribute>(property);
                if (foldoutGroupAttribute != null)
                {
                    if (!foldoutGroups.ContainsKey(foldoutGroupAttribute.Label))
                    {
                        foldoutGroups[foldoutGroupAttribute.Label] = new List<SerializedProperty>();
                        foldoutStates[foldoutGroupAttribute.Label] = true; // Default foldout state: expanded
                    }
                    foldoutGroups[foldoutGroupAttribute.Label].Add(property.Copy());
                }

                if (tabGroupAttribute == null && foldoutGroupAttribute == null)
                {
                    defaultProperties.Add(property.Copy());
                }
            }
            // Populate tab names
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
                    HashSet<string> tabNames = new HashSet<string>();
                    while (IsPropertyInTabGroups(property))
                    {
                        string tabName = GetFieldAttribute<TabGroupAttribute>(property).TabName;
                        tabNames.Add(tabName);
                        i++;
                        if (i == allProperties.Count) break;
                        else property = allProperties[i];
                    }
                    i--;
                    DrawTabGroup(tabNames.ToArray(), tabGroupIndex);
                    foreach (string tabName in tabNames)
                    {
                        drawnTabs.Add(tabName);
                    }
                    tabGroupIndex++;
                }
                else if (IsPropertyInFoldoutGroups(property))
                {
                    string foldoutKey = GetFieldAttribute<FoldoutGroupAttribute>(property).Label;
                    DrawFoldoutGroup(foldoutKey, foldoutGroups[foldoutKey]);
                    while (IsPropertyInFoldoutGroups(property))
                    {
                        i++;
                        if (i == allProperties.Count) break;
                        else property = allProperties[i];
                    }
                    i--;
                    drawnFoldouts.Add(foldoutKey);
                }
                else
                {
                    EditorGUILayout.LabelField("not in all properties");
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

        private T GetFieldAttribute<T>(SerializedProperty property) where T : PropertyAttribute
        {
            var targetType = serializedObject.targetObject.GetType();
            var fieldInfo = targetType.GetField(property.name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                var attributes = fieldInfo.GetCustomAttributes(typeof(T), true);
                if (attributes.Length > 0)
                {
                    return (T)attributes[0];
                }
            }
            return null;
        }

        private void DrawButtonWithParameters(Object targetObject, MethodInfo method)
        {
            // Begin vertical group with a box
            EditorGUILayout.BeginVertical("box");

            // Draw the button
            if (GUILayout.Button($"{method.Name}"))
            {
                // Execute the method
                object[] parameters = methodParameters.ContainsKey(method) ? methodParameters[method] : null;
                method.Invoke(targetObject, parameters);
            }

            // Get method parameters
            ParameterInfo[] parametersInfo = method.GetParameters();
            if (parametersInfo.Length > 0)
            {
                // Ensure methodParameters dictionary is initialized
                if (!methodParameters.ContainsKey(method))
                {
                    methodParameters[method] = new object[parametersInfo.Length];
                }

                // Draw fields for each parameter
                EditorGUI.indentLevel++;
                for (int i = 0; i < parametersInfo.Length; i++)
                {
                    ParameterInfo param = parametersInfo[i];
                    object currentValue = methodParameters[method][i];

                    // Draw parameter field based on parameter type
                    methodParameters[method][i] = DrawParameterField(param, currentValue);
                }
                EditorGUI.indentLevel--;
            }

            // End vertical group
            EditorGUILayout.EndVertical();
        }

        private object DrawParameterField(ParameterInfo param, object currentValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(param.Name, GUILayout.MaxWidth(100));

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
    }
}
#endif