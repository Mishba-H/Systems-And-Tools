#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class LocalGizmoHandleManager
{
    private class HandleTarget
    {
        public Object target;
        public string propertyPath;
    }

    static LocalGizmoHandleManager()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += ClearIfSelectionChanged;
    }

    private static HandleTarget activeHandle;

    public static void SetActiveHandle(SerializedProperty property)
    {
        if (property == null || property.serializedObject?.targetObject == null)
            return;

        activeHandle = new HandleTarget
        {
            target = property.serializedObject.targetObject,
            propertyPath = property.propertyPath
        };
    }

    public static void ClearActiveHandle()
    {
        activeHandle = null;
    }

    public static bool IsHandleActive(SerializedProperty property)
    {
        return activeHandle != null &&
               activeHandle.target == property.serializedObject.targetObject &&
               activeHandle.propertyPath == property.propertyPath;
    }

    private static void ClearIfSelectionChanged()
    {
        ClearActiveHandle();
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        if (activeHandle == null)
            return;

        if (!(activeHandle.target is MonoBehaviour target))
        {
            ClearActiveHandle();
            return;
        }

        var so = new SerializedObject(target);
        var prop = so.FindProperty(activeHandle.propertyPath);
        if (prop == null || prop.propertyType != SerializedPropertyType.Vector3)
        {
            ClearActiveHandle();
            return;
        }

        Vector3 local = prop.vector3Value;
        Vector3 world = target.transform.TransformPoint(local);

        EditorGUI.BeginChangeCheck();
        Vector3 newWorld = Handles.PositionHandle(world, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 newLocal = target.transform.InverseTransformPoint(newWorld);
            prop.vector3Value = newLocal;
            so.ApplyModifiedProperties();
        }
    }

    // Optional for lists/arrays: helper to get field info from path
    public static FieldInfo GetFieldInfoFromPropertyPath(System.Type type, string path)
    {
        string[] parts = path.Replace(".Array.data", "").Split('.');
        FieldInfo field = null;

        foreach (var part in parts)
        {
            if (type == null) return null;
            field = type.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return null;

            type = field.FieldType.IsArray ? field.FieldType.GetElementType()
                 : IsGenericList(field.FieldType) ? field.FieldType.GetGenericArguments()[0]
                 : field.FieldType;
        }

        return field;
    }

    static bool IsGenericList(System.Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}
#endif