#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;

namespace Sciphone
{
    public class ComboGraphAttackNode : ComboGraphBaseNode
    {
        protected PropertyField titleField;

        public override void Initialize(ComboGraphAsset asset)
        {
            base.Initialize(asset);
            nodeInfo = new ComboGraphAttackNodeInfo();

            AddInputPort();
            AddOutputPort();
        }

        public override void Draw()
        {
            base.Draw();

            SerializedProperty attackDataProperty = serializedObject.FindProperty("nodeInfos")
                .GetArrayElementAtIndex(comboGraphAsset.nodeInfos.IndexOf(nodeInfo))
                .FindPropertyRelative("attackData");

            // Start from the first visible property in attackData
            SerializedProperty property = attackDataProperty.Copy();
            SerializedProperty endProperty = attackDataProperty.GetEndProperty();

            while (property.NextVisible(true) && !SerializedProperty.EqualContents(property, endProperty))
            {
                PropertyField propertyField = new PropertyField(property);
                propertyField.Bind(serializedObject);
                extensionContainer.Add(propertyField);

                // Update node title when attackName changes
                if (property.name == "attackName")
                {
                    titleField = propertyField;
                    titleField.RegisterValueChangeCallback(evt =>
                    {
                        title = evt.changedProperty.stringValue;
                    });
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif