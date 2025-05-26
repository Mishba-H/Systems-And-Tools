#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sciphone
{
    public class ComboGraphStartNode : ComboGraphBaseNode
    {
        public override void Initialize(ComboGraphAsset asset)
        {
            base.Initialize(asset);
            nodeInfo = new ComboGraphStartNodeInfo();

            AddOutputPort();
        }

        public override void Draw()
        {
            base.Draw();
            title = "Start";
        }
    }
}
#endif