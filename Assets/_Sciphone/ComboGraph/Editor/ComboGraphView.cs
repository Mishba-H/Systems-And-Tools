#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sciphone.ComboGraph
{
    public class ComboGraphView : GraphView
    {
        [SerializeField] private ComboGraphAsset comboGraphAsset;
        [SerializeField] private ComboGraphEditorWindow editorWindow;
        [SerializeField] private SerializedObject serializedObject;

        public MiniMap miniMap;

        public List<ComboGraphBaseNode> graphNodes;
        public Dictionary<string, ComboGraphBaseNode> nodeDictionary;
        public Dictionary<Edge, ComboGraphConnectionInfo> connectionDictionary;

        public ComboGraphAsset ComboGraphAsset
        {
            set
            {
                comboGraphAsset = value;
            }
        }

        public ComboGraphView(ComboGraphEditorWindow editor, SerializedObject serializedObject)
        {
            style.flexGrow = 1;

            editorWindow = editor;
            this.serializedObject = serializedObject;

            graphNodes = new List<ComboGraphBaseNode>();
            nodeDictionary = new Dictionary<string, ComboGraphBaseNode>();
            connectionDictionary = new Dictionary<Edge, ComboGraphConnectionInfo>();

            AddManipulators();
            AddGridBackground();
            AddMiniMap();

            OnGraphViewChanged();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port)
                {
                    return;
                }

                if (startPort.node == port.node)
                {
                    return;
                }

                if (startPort.direction == port.direction)
                {
                    return;
                }

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextualMenu("Add Start Node", NodeType.Start));
            this.AddManipulator(CreateNodeContextualMenu("Add Attack Node", NodeType.Attack));
            this.AddManipulator(CreateNodeContextualMenu("Add End Node", NodeType.End));
        }

        private void AddGridBackground()
        {
            GridBackground grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
        }

        private void AddMiniMap()
        {
            miniMap = new MiniMap()
            {
                anchored = true
            };

            miniMap.SetPosition(new Rect(15, 50, 200, 180));

            Add(miniMap);

            miniMap.visible = false;
        }

        private void OnGraphViewChanged()
        {
            graphViewChanged = (changes) =>
            {
                if (changes.edgesToCreate != null)
                {
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        CreateConnection(edge);
                    }
                }

                if (changes.movedElements != null)
                {
                    List<ComboGraphBaseNode> movedNodes = changes.movedElements.OfType<ComboGraphBaseNode>().ToList();
                    if (movedNodes.Count > 0)
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Removed Node");

                        foreach (ComboGraphBaseNode movedNode in movedNodes)
                        {
                            UpdatePosition(movedNode, movedNode.GetPosition().position);
                        }

                        serializedObject.Update();
                    }
                }

                if (changes.elementsToRemove != null)
                {
                    List<ComboGraphBaseNode> removedNodes = changes.elementsToRemove.OfType<ComboGraphBaseNode>().ToList();
                    if (removedNodes.Count > 0)
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Removed Node");

                        foreach (ComboGraphBaseNode nodeToRemove in removedNodes)
                        {
                            RemoveNode(nodeToRemove);
                        }

                        serializedObject.Update();
                    }


                    List<Edge> removedEdges = changes.elementsToRemove.OfType<Edge>().ToList();
                    if (removedEdges.Count > 0)
                    {
                        Undo.RecordObject(serializedObject.targetObject, "Removed Edge");

                        foreach (Edge edgeToRemove in removedEdges)
                        {
                            RemoveEdge(edgeToRemove);
                        }
                    }
                }

                return changes;
            };
        }

        public void ClearGraph()
        {
            graphNodes.Clear();
            nodeDictionary.Clear();
            graphElements.ForEach(graphElement => RemoveElement(graphElement));
        }

        public void DrawFromAsset()
        {
            ClearGraph();
            foreach (var nodeInfo in comboGraphAsset.nodeInfos)
            {
                AddNodeFromAsset(nodeInfo);
            }
            foreach (var connectionInfo in comboGraphAsset.connectionInfos)
            {
                AddEdgeFromAsset(connectionInfo);
            }
        }

        public void CreateAndAddNode(NodeType nodeType, Vector2 position, string id = "")
        {
            Undo.RecordObject(serializedObject.targetObject, "Added Node");

            if (nodeType == NodeType.Start && comboGraphAsset.nodeInfos.FirstOrDefault(n => n.nodeType == NodeType.Start) != null)
            {
                Debug.LogWarning("There can be only one Start Node");
                return;
            }

            if (nodeType == NodeType.End && comboGraphAsset.nodeInfos.FirstOrDefault(n => n.nodeType == NodeType.End) != null)
            {
                Debug.LogWarning("There can be only one End Node");
                return;
            }

            ComboGraphBaseNode node = CreateInstanceOfType(nodeType);

            node.Initialize(comboGraphAsset);
            node.SetNodeInfo(position, id);

            AddElement(node);

            comboGraphAsset.nodeInfos.Add(AddNodeInfoToAsset(node));
            graphNodes.Add(node);
            nodeDictionary.Add(node.GetNodeInfo().guid, node);

            node.Draw();
            node.RefreshExpandedState();

            serializedObject.Update();
        }

        private void AddNodeFromAsset(ComboGraphNodeInfo nodeInfo)
        {
            ComboGraphBaseNode node = CreateInstanceOfType(nodeInfo.nodeType);

            node.Initialize(comboGraphAsset);
            node.SetNodeInfo(nodeInfo);
            node.Draw();
            node.RefreshExpandedState();

            AddElement(node);

            graphNodes.Add(node);
            nodeDictionary.Add(node.GetNodeInfo().guid, node);
        }

        private void RemoveNode(ComboGraphBaseNode node)
        {
            comboGraphAsset.nodeInfos.Remove(node.GetNodeInfo());
            nodeDictionary.Remove(node.GetNodeInfo().guid);
            graphNodes.Remove(node);
        }

        private void CreateConnection(Edge edge)
        {
            ComboGraphBaseNode inputNode = (ComboGraphBaseNode)edge.input.node;
            int inputPortIndex = inputNode.inputContainer.IndexOf(edge.input);
            ComboGraphBaseNode outputNode = (ComboGraphBaseNode)edge.output.node;
            int outputPortIndex = outputNode.outputContainer.IndexOf(edge.output);

            ComboGraphConnectionInfo connectionInfo = new ComboGraphConnectionInfo(
                inputNode.GetNodeInfo().guid, inputPortIndex,
                outputNode.GetNodeInfo().guid, outputPortIndex);
            comboGraphAsset.connectionInfos.Add(connectionInfo);

            connectionDictionary.Add(edge, connectionInfo);
        }

        private void AddEdgeFromAsset(ComboGraphConnectionInfo connectionInfo)
        {
            ComboGraphBaseNode inputNode = nodeDictionary[connectionInfo.inputPort.nodeId];
            ComboGraphBaseNode outputNode = nodeDictionary[connectionInfo.outputPort.nodeId];

            Port inputPort = inputNode.inputContainer.ElementAt(connectionInfo.inputPort.portIndex) as Port;
            Port outputPort = outputNode.outputContainer.ElementAt(connectionInfo.outputPort.portIndex) as Port;

            Edge edge = inputPort.ConnectTo(outputPort);
            AddElement(edge);

            connectionDictionary.Add(edge, connectionInfo);
        }

        private void RemoveEdge(Edge edge)
        {
            if (connectionDictionary.TryGetValue(edge, out ComboGraphConnectionInfo connectionInfo))
            {
                connectionDictionary.Remove(edge);
                comboGraphAsset.connectionInfos.Remove(connectionInfo);
            }
        }

        private void UpdatePosition(ComboGraphBaseNode node, Vector2 newPosition)
        {
            var nodeInfo = comboGraphAsset.nodeInfos.FirstOrDefault((t) => t.guid == node.GetNodeInfo().guid);
            nodeInfo.position = newPosition;
        }

        private ComboGraphBaseNode CreateInstanceOfType(NodeType nodeType)
        {
            ComboGraphBaseNode node;
            switch (nodeType)
            {
                case NodeType.Start:
                    node = Activator.CreateInstance<ComboGraphStartNode>();
                    break;
                case NodeType.Attack:
                    node = Activator.CreateInstance<ComboGraphAttackNode>();
                    break;
                case NodeType.End:
                    node = Activator.CreateInstance<ComboGraphEndNode>();
                    break;
                default:
                    node = Activator.CreateInstance<ComboGraphBaseNode>();
                    break;
            }
            return node;
        }

        private ComboGraphNodeInfo AddNodeInfoToAsset(ComboGraphBaseNode node)
        {
            switch (node.GetNodeInfo().nodeType)
            {
                case NodeType.None:
                    return node.GetNodeInfo();
                case NodeType.Start:
                    return node.GetNodeInfo() as ComboGraphStartNodeInfo;
                case NodeType.Attack:
                    return node.GetNodeInfo() as ComboGraphAttackNodeInfo;
                case NodeType.End:
                    return node.GetNodeInfo() as ComboGraphEndNodeInfo;
            }

            return null;
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, NodeType nodeType)
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => CreateAndAddNode(
                    nodeType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition)))
            );

            return contextualMenuManipulator;
        }

        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            Vector2 worldMousePosition = mousePosition;

            if (isSearchWindow)
            {
                worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent, mousePosition - editorWindow.position.position);
            }
            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);

            return localMousePosition;
        }
    }
}
#endif