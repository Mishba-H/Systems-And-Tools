#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sciphone.ComboGraph
{
    public class ComboGraphEditorWindow : EditorWindow
    {
        [SerializeField] private ComboGraphAsset comboGraphAsset;
        [SerializeField] private SerializedObject serializedObject;

        [SerializeField] private ComboGraphView graphView;
        [SerializeField] private Toolbar toolbar;

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            AddToolbar();
            Initialize(comboGraphAsset);
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(graphView);
        }

        [MenuItem("Window/Combo Graph Editor")]
        public static void Open()
        {
            ComboGraphEditorWindow window = GetWindow<ComboGraphEditorWindow>(typeof(ComboGraphEditorWindow), typeof(SceneView));
            window.titleContent = new GUIContent("Combo Graph");
            window.Show();
            window.AddToolbar();
        }

        public static void Open(ComboGraphAsset target)
        {
            ComboGraphEditorWindow window = GetWindow<ComboGraphEditorWindow>(typeof(ComboGraphEditorWindow), typeof(SceneView));
            window.titleContent = new GUIContent("Combo Graph");
            window.Show();
            window.Focus();
            window.AddToolbar();
            window.Initialize(target);
            window.UpdateToolbarAssetField();
        }

        private void Initialize(ComboGraphAsset target)
        {
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
                graphView = null;
            }

            if (target == null)
            {
                return;
            }

            comboGraphAsset = target;
            serializedObject = new SerializedObject(comboGraphAsset);
            AddGraphView();
        }

        private void AddGraphView()
        {
            if (comboGraphAsset == null) return;

            graphView = new ComboGraphView(this, serializedObject);
            rootVisualElement.Add(graphView);
            graphView.ComboGraphAsset = comboGraphAsset;
            graphView.DrawFromAsset();
        }

        private void AddToolbar()
        {
            if (toolbar != null) return;

            toolbar = new Toolbar(); 
            
            ObjectField assetField = new ObjectField("Graph Asset");
            assetField.objectType = typeof(ComboGraphAsset);
            assetField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>
            {
                Initialize(evt.newValue as ComboGraphAsset);
            });
            toolbar.Add(assetField);

            Button miniMapButton = new Button(() => ToggleMiniMap())
            {
                text = "Minimap"
            };
            toolbar.Add(miniMapButton);

            Button frameAllButton = new Button(() => graphView.FrameAll())
            {
                text = "Frame All"
            };
            toolbar.Add(frameAllButton);

            rootVisualElement.Add(toolbar);
        }

        private void ToggleMiniMap()
        {
            graphView.miniMap.visible = !graphView.miniMap.visible;
        }

        private void OnUndoRedoPerformed()
        {
            if (comboGraphAsset == null) return;

            graphView.DrawFromAsset();
        }

        private void UpdateToolbarAssetField()
        {
            if (toolbar == null) return;

            ObjectField assetField = toolbar.Q<ObjectField>();

            if (assetField != null)
            {
                assetField.SetValueWithoutNotify(comboGraphAsset);
            }
        }
    }
}
#endif

