#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sciphone;

[CustomPropertyDrawer(typeof(ClipOrFBXAttribute))]
public class ClipOrFBXDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Allow dragging AnimationClip or FBX asset
        Object current = property.objectReferenceValue;
        Object input = EditorGUI.ObjectField(position, label, current, typeof(Object), false);

        if (input != current)
        {
            if (input is AnimationClip clip)
            {
                property.objectReferenceValue = clip;
            }
            else if (input is GameObject go)
            {
                string path = AssetDatabase.GetAssetPath(go);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset is AnimationClip fbxClip && !fbxClip.name.StartsWith("__preview__"))
                        {
                            property.objectReferenceValue = fbxClip;
                            break;
                        }
                    }
                }
            }
        }

        EditorGUI.EndProperty();
    }
}

#endif