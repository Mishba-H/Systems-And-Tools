#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

[InitializeOnLoad]
public static class LocalGizmoHandleManager
{
    static LocalGizmoHandleManager()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        foreach (MonoBehaviour comp in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!comp.enabled || comp.gameObject.hideFlags != HideFlags.None)
                continue;

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.GetIterator();

            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyType == SerializedPropertyType.Vector3)
                    {
                        FieldInfo field = GetField(comp.GetType(), prop.propertyPath);
                        if (field == null) continue;

                        if (field.GetCustomAttribute<LocalGizmoHandleAttribute>() != null)
                        {
                            if (!LocalGizmoHandleDrawer.IsHandleVisible(prop))
                                continue;

                            Vector3 local = prop.vector3Value;
                            Vector3 world = comp.transform.TransformPoint(local);

                            EditorGUI.BeginChangeCheck();
                            Vector3 newWorld = Handles.PositionHandle(world, Quaternion.identity);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Vector3 newLocal = comp.transform.InverseTransformPoint(newWorld);
                                prop.vector3Value = newLocal;
                                so.ApplyModifiedProperties();
                            }
                        }
                    }
                }
                while (prop.NextVisible(false));
            }
        }
    }

    static FieldInfo GetField(System.Type type, string path)
    {
        string[] parts = path.Replace(".Array.data", "").Split('.');
        FieldInfo field = null;
        foreach (var part in parts)
        {
            if (type == null) return null;
            field = type.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return null;

            type = field.FieldType.IsArray ? field.FieldType.GetElementType() : field.FieldType;
        }

        return field;
    }
}
#endif