#if UNITY_EDITOR
namespace Sciphone.ComboGraph
{
    public class ComboGraphEndNode : ComboGraphBaseNode
    {
        public override void Initialize(ComboGraphAsset asset)
        {
            base.Initialize(asset);
            nodeInfo = new ComboGraphEndNodeInfo();

            AddInputPort();
        }

        public override void Draw()
        {
            base.Draw();
            title = "End";
        }
    }
}
#endif