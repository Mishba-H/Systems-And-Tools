#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LocalGizmoHandleAttribute))]
public class LocalGizmoHandleDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float buttonWidth = 60f;
        Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth - 4, position.height);
        Rect buttonRect = new Rect(fieldRect.xMax + 4, position.y, buttonWidth, position.height);

        EditorGUI.PropertyField(fieldRect, property, label);

        bool isActive = LocalGizmoHandleManager.IsHandleActive(property);
        string buttonText = isActive ? "Hide" : "Show";

        if (GUI.Button(buttonRect, buttonText))
        {
            if (isActive)
                LocalGizmoHandleManager.ClearActiveHandle();
            else
                LocalGizmoHandleManager.SetActiveHandle(property);
        }
    }
}
#endif