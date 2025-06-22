#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sciphone
{
    [CustomPropertyDrawer(typeof(PreviewAnimationClipAttribute))]
    public class PreviewAnimationClipDrawer : PropertyDrawer
    {
        bool rootXZ = true;
        bool rootY = true;
        private static GameObject lastSelectedObject;
        static PreviewAnimationClipDrawer()
        {
            // Listen to hierarchy selection changes
            Selection.selectionChanged += OnSelectionChanged;
        }
        private static void OnSelectionChanged()
        {
            GameObject currentSelectedObject = Selection.activeGameObject;

            // Reset the previous object to T-pose if it had an Animator
            if (lastSelectedObject != null && lastSelectedObject != currentSelectedObject)
            {
                ResetToTPose(lastSelectedObject);
            }

            // Update the last selected object
            lastSelectedObject = currentSelectedObject;
        }
        private void PreviewAnimationClip(AnimationClip clip, float progress)
        {
            GameObject target = Selection.activeGameObject;
            Animator animator = target?.GetComponent<Animator>();
            if (animator)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var hipsPosition = hips.position;
                clip.SampleAnimation(target, progress * clip.length);

                var y = rootY ? hips.position.y : hipsPosition.y;
                var x = rootXZ ? hips.position.x : hipsPosition.x;
                var z = rootXZ ? hips.position.z : hipsPosition.z;
                hips.position = new Vector3(x, y, z);
            }
            else
            {
                Debug.LogWarning("No Animator found on selected object.");
            }
        }
        private void ResetToTPose()
        {
            if (Selection.activeGameObject?.GetComponent<Animator>() is Animator animator)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
        private static void ResetToTPose(GameObject gameObject)
        {
            if (gameObject?.GetComponent<Animator>() != null && gameObject?.GetComponent<Animator>() is Animator animator)
            {
                animator.Rebind(); // Reset to T-pose
                animator.Update(0f); // Apply the reset immediately
            }
        }
        private SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingName)
        {
            if (property == null || string.IsNullOrEmpty(siblingName))
            {
                return null;
            }

            string propertyPath = property.propertyPath;
            int lastDot = propertyPath.LastIndexOf('.');
            string parentPath = lastDot >= 0 ? propertyPath.Substring(0, lastDot) : "";
            string siblingPath = string.IsNullOrEmpty(parentPath) ? siblingName : $"{parentPath}.{siblingName}";

            return property.serializedObject.FindProperty(siblingPath);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Float)
            {
                EditorGUI.LabelField(position, label.text, "Use [PreviewClip] with float.");
                return;
            }

            PreviewAnimationClipAttribute previewAttr = (PreviewAnimationClipAttribute)attribute;
            SerializedProperty clipProperty = FindSiblingProperty(property, previewAttr.clipName);
            if (clipProperty == null || clipProperty.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "Invalid AnimationClip reference.");
                return;
            }
            AnimationClip clip = clipProperty.objectReferenceValue as AnimationClip;
            if (clip == null)
            {
                EditorGUI.LabelField(position, label.text, " AnimationClip reference is null.");
                return;
            }

            EditorGUI.BeginChangeCheck();
            Rect labelPos = position;
            labelPos.width = position.width * 0.2f;
            EditorGUI.LabelField(labelPos, label);
            Rect sliderPosition = position;
            sliderPosition.xMin = labelPos.xMax;
            sliderPosition.width = position.xMax - 90f - labelPos.xMax - 3f;
            property.floatValue = EditorGUI.Slider(sliderPosition, property.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck() && clip && !Application.isPlaying)
            {
                PreviewAnimationClip(clip, property.floatValue);
            }
            Rect rect = position;
            rect.xMin = position.xMax - 90;
            rect.width = 40;
            rootXZ = EditorGUI.ToggleLeft(rect, "XZ", rootXZ);
            rect.xMin += 40;
            rect.width = 30;
            rootY = EditorGUI.ToggleLeft(rect, "Y", rootY);
            rect.xMin += 30;
            rect.width = 20f;
            if (GUI.Button(rect, new GUIContent("T", "Reset to T-Pose")))
            {
                ResetToTPose();
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif