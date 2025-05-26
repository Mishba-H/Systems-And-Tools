using System;
using UnityEngine;

namespace Sciphone.ComboGraph
{
    [Serializable]
    public class ComboGraphNodeInfo
    {
        public string guid;
        public Vector2 position;
        public NodeType nodeType;
    }

    [Serializable]
    public class ComboGraphStartNodeInfo : ComboGraphNodeInfo
    {
        public ComboGraphStartNodeInfo()
        {
            nodeType = NodeType.Start;
        }
    }

    [Serializable]
    public class ComboGraphAttackNodeInfo : ComboGraphNodeInfo
    {
        [SerializeReference] public AttackData attackData;

        public ComboGraphAttackNodeInfo()
        {
            nodeType = NodeType.Attack;
            attackData = new AttackData();
        }
    }

    [Serializable]
    public class ComboGraphEndNodeInfo : ComboGraphNodeInfo
    {
        public ComboGraphEndNodeInfo()
        {
            nodeType = NodeType.End;
        }
    }
}