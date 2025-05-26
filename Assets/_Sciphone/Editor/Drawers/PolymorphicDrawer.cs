#if UNITY_EDITOR
using System;

using UnityEditor;
using UnityEngine;

namespace Sciphone
{
    using Toolbox.Editor;
    using Toolbox.Editor.Drawers;
    using Toolbox.Editor.Internal.Types;

    [CustomPropertyDrawer(typeof(PolymorphicAttribute))]
    public class PolymorphicDrawer : PropertyDrawer
    {
        private const float labelWidthOffset = -80.0f;

        private static readonly TypeConstraintContext sharedConstraint = new TypeConstraintSerializeReference(null);
        private static readonly TypeAppearanceContext sharedAppearance = new TypeAppearanceContext(sharedConstraint, TypeGrouping.None, true);
        private static readonly TypeField typeField = new TypeField(sharedConstraint, sharedAppearance);

        private void UpdateContexts(PolymorphicAttribute attribute)
        {
            sharedAppearance.TypeGrouping = attribute.TypeGrouping;
        }

        private Type GetParentType(PolymorphicAttribute attribute, SerializedProperty property)
        {
            var fieldInfo = property.GetFieldInfo(out _);
            var fieldType = property.GetProperType(fieldInfo);
            var candidateType = attribute.ParentType;
            if (candidateType != null)
            {
                if (fieldType.IsAssignableFrom(candidateType))
                {
                    return candidateType;
                }

                /*ToolboxEditorLog.AttributeUsageWarning(attribute, property,
                    $"Provided {nameof(attribute.ParentType)} ({candidateType}) cannot be used because it's not assignable from: '{fieldType}'");*/
            }

            return fieldType;
        }

        private void CreateTypeProperty(SerializedProperty property, Type parentType, PolymorphicAttribute attribute, Rect position)
        {
            TypeUtility.TryGetTypeFromManagedReferenceFullTypeName(property.managedReferenceFullTypename, out var currentType);
            typeField.OnGui(position, attribute.AddTextSearchField, (type) =>
            {
                try
                {
                    if (!property.serializedObject.isEditingMultipleObjects)
                    {
                        UpdateTypeProperty(property, type, attribute);
                    }
                    else
                    {
                        var targets = property.serializedObject.targetObjects;
                        foreach (var target in targets)
                        {
                            using (var so = new SerializedObject(target))
                            {
                                var sp = so.FindProperty(property.propertyPath);
                                UpdateTypeProperty(sp, type, attribute);
                            }
                        }
                    }
                }
                catch (Exception e) when (e is ArgumentNullException || e is NullReferenceException)
                {
                    /*ToolboxEditorLog.LogWarning("Invalid attempt to update disposed property.");*/
                }
            }, currentType, parentType);
        }

        private void UpdateTypeProperty(SerializedProperty property, Type targetType, PolymorphicAttribute attribute)
        {
            var forceUninitializedInstance = attribute.ForceUninitializedInstance;
            var obj = ReflectionUtility.CreateInstance(targetType, forceUninitializedInstance);
            property.serializedObject.Update();
            property.managedReferenceValue = obj;
            property.serializedObject.ApplyModifiedProperties();

            //NOTE: fix for invalid cached properties, e.g. changing parent's managed reference can change available children
            // since we cannot check if cached property is "valid" we need to clear the whole cache
            //TODO: reverse it and provide dedicated event when a managed property is changed through a dedicated handler
            DrawerStorageManager.ClearStorages();
        }

        private Rect PrepareTypePropertyPosition(bool hasLabel, in Rect labelPosition, in Rect inputPosition, bool isPropertyExpanded)
        {
            var position = new Rect(inputPosition);
            if (!hasLabel)
            {
                position.xMin += EditorGUIUtility.standardVerticalSpacing;
                return position;
            }

            /*//skip row only if label exists
            if (isPropertyExpanded)
            {
                //property is expanded and we have place to move it to the next row
                position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                position = EditorGUI.IndentedRect(position);
                return position;
            }*/

            var baseLabelWidth = EditorGUIUtility.labelWidth + labelWidthOffset;
            var realLabelWidth = labelPosition.width;
            //adjust position to already rendered label
            position.xMin += Mathf.Max(baseLabelWidth, realLabelWidth);
            return position;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Use with ManagedReference only.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Foldout for the ManagedReference property
            Rect foldoutRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            // Draw type selection dropdown
            var polymorphicAttr = (PolymorphicAttribute)attribute;
            var parentType = GetParentType(polymorphicAttr, property);
            Rect inputRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            CreateTypeProperty(property, parentType, polymorphicAttr, inputRect);

            if (property.isExpanded)
            {
                if (property.managedReferenceValue != null)
                {
                    // Iterate through the children while limiting scope
                    SerializedProperty childProperty = property.Copy();
                    SerializedProperty endProperty = property.GetEndProperty(); // Define the scope endpoint

                    float yOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    Rect childPosition = new Rect(position.x, yOffset, position.width, EditorGUIUtility.singleLineHeight);

                    if (childProperty.NextVisible(true)) // Move to the first child
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(childProperty, endProperty)) break; // Stop at the end of this property's scope

                            float childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                            childPosition.height = childHeight;

                            EditorGUI.PropertyField(childPosition, childProperty, true);
                            childPosition.y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                        } while (childProperty.NextVisible(false)); // Iterate through visible children
                    }
                }
                else
                {
                    // If no object is assigned, display a warning message
                    Rect warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(warningRect, "No object assigned. Select a type to create an instance.");
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight; // Base height for the foldout

            if (property.isExpanded)
            {
                if (property.managedReferenceValue != null)
                {
                    SerializedProperty childProperty = property.Copy();
                    SerializedProperty endProperty = property.GetEndProperty(); // Define the scope endpoint

                    if (childProperty.NextVisible(true)) // Move to the first child
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(childProperty, endProperty)) break; // Stop at the end of this property's scope

                            totalHeight += EditorGUI.GetPropertyHeight(childProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                        } while (childProperty.NextVisible(false)); // Iterate through visible children
                    }
                }
                else
                {
                    // Add space for the warning message
                    totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return totalHeight;
        }
    }
}
#endif