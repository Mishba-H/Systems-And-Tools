#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(LocalGizmoHandleAttribute))]
public class LocalGizmoHandleDrawer : PropertyDrawer
{
    private static Dictionary<string, bool> handleVisibility = new Dictionary<string, bool>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Vector3)
        {
            EditorGUI.LabelField(position, label.text, "Use [LocalGizmoHandle] on Vector3 only");
            return;
        }

        float buttonWidth = 50f;
        Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth, position.height);
        Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

        EditorGUI.PropertyField(fieldRect, property, label);

        string key = GetPropertyKey(property);
        if (!handleVisibility.ContainsKey(key))
            handleVisibility[key] = true;

        string toggle = handleVisibility[key] ? "Hide" : "Show";
        if (GUI.Button(buttonRect, toggle))
            handleVisibility[key] = !handleVisibility[key];
    }

    public static bool IsHandleVisible(SerializedProperty property)
    {
        string key = GetPropertyKey(property);
        return handleVisibility.TryGetValue(key, out bool show) && show;
    }

    private static string GetPropertyKey(SerializedProperty property)
    {
        return property.serializedObject.targetObject.GetInstanceID() + "_" + property.propertyPath;
    }
}

#endif