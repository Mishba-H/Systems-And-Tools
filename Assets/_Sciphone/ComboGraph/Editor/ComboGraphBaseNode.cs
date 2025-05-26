#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Sciphone.ComboGraph
{
    [Serializable]
    public class ComboGraphBaseNode : Node
    {
        protected ComboGraphAsset comboGraphAsset;
        protected SerializedObject serializedObject;
        protected ComboGraphNodeInfo nodeInfo;

        public virtual void Initialize(ComboGraphAsset asset)
        {
            comboGraphAsset = asset;
            nodeInfo = new ComboGraphNodeInfo();
            nodeInfo.nodeType = NodeType.None;
        }

        public virtual void Draw()
        {
            SetPosition(new Rect(nodeInfo.position, Vector2.zero));
            serializedObject = new SerializedObject(comboGraphAsset);
        }

        public ComboGraphNodeInfo GetNodeInfo() 
        {
            return nodeInfo; 
        }

        public void SetNodeInfo(ComboGraphNodeInfo nodeInfo)
        {
            this.nodeInfo = nodeInfo;
        }

        public void SetNodeInfo(Vector2 position, string id = "")
        {
            if (id == "")
            {
                nodeInfo.guid = NewGuid();
            }
            else
            {
                nodeInfo.guid = id;
            }
            nodeInfo.position = position;
        }

        private string NewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        protected void AddInputPort()
        {
            Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputContainer.Add(inputPort);
        }

        protected void AddOutputPort()
        {
            Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputContainer.Add(outputPort);
        }
    }
}
#endif