#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sciphone
{
    [CustomPropertyDrawer(typeof(PreviewAnimationClipAttribute))]
    public class PreviewAnimationClipDrawer : PropertyDrawer
    {
        private static Dictionary<string, bool> rootXZMap = new Dictionary<string, bool>();
        private static Dictionary<string, bool> rootYMap = new Dictionary<string, bool>();
        private static Dictionary<string, bool> previewRangeToggleMap = new Dictionary<string, bool>();
        private static Dictionary<string, Vector2> previewRangeMap = new Dictionary<string, Vector2>();

        private static GameObject lastSelectedObject;

        private Vector2 defaultPreviewRange = new Vector2(0f, 1f);

        static PreviewAnimationClipDrawer()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }
        private static void OnSelectionChanged()
        {
            GameObject currentSelectedObject = Selection.activeGameObject;
            
            if (currentSelectedObject != null)
            {
                ResetToTPose(currentSelectedObject);
            }

            if (lastSelectedObject != null && lastSelectedObject != currentSelectedObject)
            {
                ResetToTPose(lastSelectedObject);
            }
            lastSelectedObject = currentSelectedObject;
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
            SerializedProperty propertiesListProperty = FindSiblingProperty(property, previewAttr.propertiesListName);

            if (clipProperty == null || clipProperty.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "Invalid AnimationClip reference.");
                return;
            }

            AnimationClip clip = clipProperty.objectReferenceValue as AnimationClip;
            if (clip == null)
            {
                EditorGUI.LabelField(position, label.text, "AnimationClip reference is null.");
                return;
            }

            string key = property.propertyPath;
            if (!rootXZMap.ContainsKey(key)) rootXZMap[key] = true;
            if (!rootYMap.ContainsKey(key)) rootYMap[key] = true;
            if (!previewRangeToggleMap.ContainsKey(key)) previewRangeToggleMap[key] = true;
            if (propertiesListProperty != null && propertiesListProperty.isArray)
            {
                previewRangeMap[key] = GetPlayWindowFromProperties(propertiesListProperty);
            }
            else if (!previewRangeMap.ContainsKey(key))
            {
                previewRangeMap[key] = new Vector2(0f, 1f);
            }

            // --- First Line: float slider ---
            Rect line1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect labelPos = new Rect(line1.x, line1.y, line1.width * 0.2f, line1.height);
            Rect sliderPos = new Rect(labelPos.xMax, line1.y, line1.width * 0.8f, line1.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.LabelField(labelPos, label);
            property.floatValue = EditorGUI.Slider(sliderPos, property.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck() && clip && !Application.isPlaying)
            {
                if (previewRangeToggleMap[key])
                {
                    PreviewAnimationClip(clip, property.floatValue, rootXZMap[key], rootYMap[key], previewRangeMap[key]);
                }
                else
                {
                    PreviewAnimationClip(clip, property.floatValue, rootXZMap[key], rootYMap[key], defaultPreviewRange);
                }
            }

            // --- Second Line: Preview Range + Toggles + Button ---
            Rect line2 = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f, position.width, EditorGUIUtility.singleLineHeight);

            float labelWidth = line2.width * 0.2f;
            float fieldSpacing = 4f;
            float floatFieldWidth = 30f;
            float toggleWidth = 30f;
            float buttonWidth = 20f;

            Rect previewLabel = new Rect(line2.x, line2.y, labelWidth, line2.height);
            EditorGUI.LabelField(previewLabel, "Preview Range");

            Vector2 range = previewRangeMap[key];
            float min = range.x;
            float max = range.y;

            Rect minField = new Rect(previewLabel.xMax, line2.y, floatFieldWidth, line2.height);
            Rect slider = new Rect(minField.xMax + fieldSpacing, line2.y, line2.width - labelWidth - floatFieldWidth * 2 - toggleWidth * 3 - buttonWidth - fieldSpacing * 7, line2.height);
            Rect maxField = new Rect(slider.xMax + fieldSpacing, line2.y, floatFieldWidth, line2.height);

            Rect previewToggle = new Rect(maxField.xMax + fieldSpacing, line2.y, toggleWidth, line2.height);
            Rect xzToggle = new Rect(previewToggle.xMax + fieldSpacing, line2.y, toggleWidth, line2.height);
            Rect yToggle = new Rect(xzToggle.xMax + fieldSpacing, line2.y, toggleWidth, line2.height);
            Rect button = new Rect(yToggle.xMax + fieldSpacing, line2.y, buttonWidth, line2.height);

            min = EditorGUI.FloatField(minField, min);
            max = EditorGUI.FloatField(maxField, max);
            EditorGUI.MinMaxSlider(slider, ref min, ref max, 0f, 1f);
            range = new Vector2(Mathf.Clamp01(min), Mathf.Clamp01(max));
            previewRangeMap[key] = range;

            previewRangeToggleMap[key] = GUI.Toggle(previewToggle, previewRangeToggleMap[key], new GUIContent("<>", "Preview Cropped Animation"), EditorStyles.miniButton);
            rootXZMap[key] = GUI.Toggle(xzToggle, rootXZMap[key], new GUIContent("XZ", "Preview Root Transform XZ"), EditorStyles.miniButton);
            rootYMap[key] = GUI.Toggle(yToggle, rootYMap[key], new GUIContent("Y", "Preview Root Transform Y"), EditorStyles.miniButton);

            if (GUI.Button(button, new GUIContent("T", "Reset to T-Pose")))
            {
                ResetToTPose();
                property.floatValue = 0f;
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
        private Vector2 GetPlayWindowFromProperties(SerializedProperty parentProperty)
        {
            for (int i = 0; i < parentProperty.arraySize; i++)
            {
                var element = parentProperty.GetArrayElementAtIndex(i);
                var playWindowProp = element.FindPropertyRelative("playWindow");
                if (playWindowProp != null && playWindowProp.propertyType == SerializedPropertyType.Vector2)
                {
                    return playWindowProp.vector2Value;
                }
            }
            return new Vector2(0f, 1f);
        }
        private void PreviewAnimationClip(AnimationClip clip, float progress, bool rootXZ, bool rootY, Vector2 previewRange)
        {
            GameObject target = Selection.activeGameObject;
            if (target.TryGetComponent(out Animator animator))
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

                clip.SampleAnimation(target, 0f);
                Vector3 hipsPositionAtZero = hips.position;

                clip.SampleAnimation(target, previewRange.x * clip.length);
                Vector3 hipsPositionAtPreviewStart = hips.position;

                Vector3 hipsStartOffset = hipsPositionAtPreviewStart - hipsPositionAtZero;

                var previewProgress = Mathf.Lerp(previewRange.x, previewRange.y, progress);
                clip.SampleAnimation(target, previewProgress * clip.length);

                var y = rootY ? hips.position.y - hipsStartOffset.y : hipsPositionAtZero.y;
                var x = rootXZ ? hips.position.x - hipsStartOffset.x : hipsPositionAtZero.x;
                var z = rootXZ ? hips.position.z - hipsStartOffset.z : hipsPositionAtZero.z;
                hips.position = new Vector3(x, y, z);
            }
            else
            {
                Debug.LogWarning("No Animator found on selected object.");
            }
        }
        private void ResetToTPose()
        {
            if (Selection.activeGameObject.TryGetComponent(out Animator animator))
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
        private static void ResetToTPose(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Animator animator))
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + 4f;
        }
    }
}
#endif