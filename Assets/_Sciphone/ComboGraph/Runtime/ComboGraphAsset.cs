using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor;
#endif

namespace Sciphone.ComboGraph
{
    [CreateAssetMenu(fileName = "NewComboGraph", menuName = "ComboGraph/New Combo Graph")]
    public class ComboGraphAsset : ScriptableObject
    {
        [SerializeReference] public List<ComboGraphNodeInfo> nodeInfos;
        [SerializeReference] public List<ComboGraphConnectionInfo> connectionInfos;

        [SerializeReference] public ComboGraphNodeInfo currentNodeInfo;

        public void Initialize()
        {
            currentNodeInfo = GetStartNode();
        }

        public bool TryGetDataFromNextNode(AttackType attackType, out AttackData data)
        {
            foreach (var nodeInfo in GetNextNodes(currentNodeInfo))
            {
                if (nodeInfo is ComboGraphAttackNodeInfo)
                {
                    if ((nodeInfo as ComboGraphAttackNodeInfo).attackData.attackType == attackType)
                    {
                        currentNodeInfo = nodeInfo as ComboGraphAttackNodeInfo;
                        data = (nodeInfo as ComboGraphAttackNodeInfo).attackData;
                        return true;
                    }
                }
            }
            currentNodeInfo = GetStartNode();
            data = null;
            return false;
        }

        public ComboGraphNodeInfo GetStartNode()
        {
            foreach (var nodeInfo in nodeInfos)
            {
                if (nodeInfo.nodeType == NodeType.Start)
                {
                    return nodeInfo;
                }
            }
            return null;
        }

        public List<ComboGraphNodeInfo> GetNextNodes(ComboGraphNodeInfo nodeInfo)
        {
            List<ComboGraphNodeInfo > nextNodes = new List<ComboGraphNodeInfo>();
            foreach (var c in connectionInfos)
            {
                if (c.outputPort.nodeId == nodeInfo.guid)
                {
                    nextNodes.Add(nodeInfos.FirstOrDefault(n => n.guid == c.inputPort.nodeId));
                }
            }
            return nextNodes;
        }

#if UNITY_EDITOR
        [Button(nameof(OpenEditor))]
        public void OpenEditor()
        {
            ComboGraphEditorWindow.Open(this);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset.GetType() == typeof(ComboGraphAsset))
            {
                ComboGraphEditorWindow.Open((ComboGraphAsset)asset);
                return true;
            }
            return false;
        }
#endif
    }
}