#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Locomotion.Narrative;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Locomotion.Narrative.EditorTools
{
    public class NarrativeTreeEditorWindow : EditorWindow
    {
        private NarrativeTreeAsset tree;
        private NarrativeGraphView graph;
        private VisualElement inspectorRoot;

        private NarrativeNode selectedNode;

        [MenuItem("Window/Locomotion/Narrative/Tree Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<NarrativeTreeEditorWindow>("Narrative Tree");
            w.minSize = new Vector2(980, 620);
            w.Show();
        }

        public static void ShowWindow(NarrativeTreeAsset tree)
        {
            var w = GetWindow<NarrativeTreeEditorWindow>("Narrative Tree");
            w.minSize = new Vector2(980, 620);
            w.tree = tree;
            w.Rebuild();
            w.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // Note: GraphView also defines an internal Toolbar type; use the public UIElements toolbar explicitly.
            var toolbar = new UnityEditor.UIElements.Toolbar();
            
            // Use IMGUIContainer for ObjectField to ensure scene objects work properly
            var treeFieldContainer = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                var newTree = EditorGUILayout.ObjectField("Tree", tree, typeof(NarrativeTreeAsset), true) as NarrativeTreeAsset;
                if (EditorGUI.EndChangeCheck())
                {
                    tree = newTree;
                    Rebuild();
                }
            });
            treeFieldContainer.style.flexGrow = 1f;
            
            toolbar.Add(treeFieldContainer);
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Sequence)) { text = "Add Sequence" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Selector)) { text = "Add Selector" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => AddChildToSelected(NarrativeNodeType.Action)) { text = "Add Action" });
            toolbar.Add(new UnityEditor.UIElements.ToolbarButton(() => Rebuild()) { text = "Refresh" });
            root.Add(toolbar);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;

            graph = new NarrativeGraphView();
            graph.style.flexGrow = 1.8f;
            graph.OnNodeSelected = node =>
            {
                selectedNode = node;
                RebuildInspector();
            };

            inspectorRoot = new ScrollView();
            inspectorRoot.style.flexGrow = 1f;
            inspectorRoot.style.paddingLeft = 8;
            inspectorRoot.style.paddingRight = 8;
            inspectorRoot.style.paddingTop = 6;
            inspectorRoot.style.paddingBottom = 6;

            body.Add(graph);
            body.Add(inspectorRoot);
            root.Add(body);

            Rebuild();
        }

        private void Rebuild()
        {
            selectedNode = null;
            if (graph != null)
            {
                graph.Populate(tree);
            }
            RebuildInspector();
        }

        private void AddChildToSelected(NarrativeNodeType type)
        {
            if (tree == null || tree.root == null)
                return;

            var parent = selectedNode ?? tree.root;

            // Only nodes with children can accept children.
            if (parent is NarrativeActionNode)
                parent = tree.root;

            Undo.RecordObject(tree, "Add Narrative Node");

            NarrativeNode child = type switch
            {
                NarrativeNodeType.Sequence => new NarrativeSequenceNode { title = "Sequence" },
                NarrativeNodeType.Selector => new NarrativeSelectorNode { title = "Selector" },
                NarrativeNodeType.Action => new NarrativeActionNode { title = "Action", action = new CallMethodAction() },
                _ => new NarrativeActionNode { title = "Action", action = new CallMethodAction() }
            };

            switch (parent)
            {
                case NarrativeSequenceNode seq:
                    seq.children.Add(child);
                    break;
                case NarrativeSelectorNode sel:
                    sel.children.Add(child);
                    break;
                default:
                    // fallback: root
                    if (tree.root is NarrativeSequenceNode rootSeq)
                        rootSeq.children.Add(child);
                    break;
            }

            EditorUtility.SetDirty(tree);
            graph?.Populate(tree);
            selectedNode = child;
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            inspectorRoot.Clear();

            if (tree == null)
            {
                inspectorRoot.Add(new Label("Assign a NarrativeTreeAsset to edit."));
                return;
            }

            var node = selectedNode ?? tree.root;
            if (node == null)
            {
                inspectorRoot.Add(new Label("Tree has no root node."));
                return;
            }

            inspectorRoot.Add(new Label("Node") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

            var titleField = new TextField("Title") { value = node.title };
            titleField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(tree, "Edit Narrative Node Title");
                node.title = evt.newValue;
                EditorUtility.SetDirty(tree);
                graph?.Populate(tree);
            });
            inspectorRoot.Add(titleField);

            inspectorRoot.Add(new Label($"Type: {node.NodeType}"));
            inspectorRoot.Add(new Label($"Id: {node.id}") { style = { color = new Color(0, 0, 0, 0.6f), fontSize = 10 } });

            inspectorRoot.Add(new Label("Contingency") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } });
            inspectorRoot.Add(new IMGUIContainer(() => DrawContingency(node.contingency)));

            if (node is NarrativeActionNode an)
            {
                inspectorRoot.Add(new Label("Action") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } });
                inspectorRoot.Add(new IMGUIContainer(() => DrawAction(an)));
            }
        }

        private void DrawContingency(NarrativeContingency c)
        {
            if (tree == null) return;

            EditorGUI.BeginChangeCheck();
            c.enabled = EditorGUILayout.Toggle("Enabled", c.enabled);
            c.op = (NarrativeLogicalOperator)EditorGUILayout.EnumPopup("Operator", c.op);

            if (GUILayout.Button("Add Condition: Component Member"))
            {
                Undo.RecordObject(tree, "Add Narrative Condition");
                c.conditions.Add(new ComponentMemberCondition());
                EditorUtility.SetDirty(tree);
            }

            for (int i = 0; i < c.conditions.Count; i++)
            {
                var cond = c.conditions[i];
                if (cond == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(cond.GetType().Name, EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    Undo.RecordObject(tree, "Remove Narrative Condition");
                    c.conditions.RemoveAt(i);
                    EditorUtility.SetDirty(tree);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (cond is ComponentMemberCondition cm)
                {
                    cm.targetKey = EditorGUILayout.TextField("Target Key", cm.targetKey);
                    cm.componentTypeName = EditorGUILayout.TextField("Component Type", cm.componentTypeName);
                    cm.memberName = EditorGUILayout.TextField("Member", cm.memberName);
                    cm.comparison = (ComparisonOperator)EditorGUILayout.EnumPopup("Compare", cm.comparison);
                    DrawNarrativeValue("Value", ref cm.compareTo);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tree, "Edit Narrative Contingency");
                EditorUtility.SetDirty(tree);
            }
        }

        private void DrawAction(NarrativeActionNode node)
        {
            if (tree == null) return;

            EditorGUI.BeginChangeCheck();

            NarrativeActionSpec a = node.action;
            string typeName = a != null ? a.GetType().Name : "(none)";
            EditorGUILayout.LabelField("Current", typeName);

            // Action type switcher
            var newType = (NarrativeActionKind)EditorGUILayout.EnumPopup("Action Type", GetKind(a));
            if (a == null || newType != GetKind(a))
            {
                Undo.RecordObject(tree, "Change Narrative Action Type");
                node.action = newType switch
                {
                    NarrativeActionKind.SpawnPrefab => new SpawnPrefabAction(),
                    NarrativeActionKind.SetProperty => new SetPropertyAction(),
                    NarrativeActionKind.CallMethod => new CallMethodAction(),
                    NarrativeActionKind.RunBehaviorTree => new RunBehaviorTreeAction(),
                    _ => new CallMethodAction()
                };
                EditorUtility.SetDirty(tree);
                a = node.action;
            }

            if (a == null)
            {
                EditorGUI.EndChangeCheck();
                return;
            }

            // Action-specific fields
            if (a is SpawnPrefabAction sp)
            {
                sp.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sp.prefab, typeof(GameObject), false);
                sp.parentKey = EditorGUILayout.TextField("Parent Key", sp.parentKey);
                sp.worldSpace = EditorGUILayout.Toggle("World Space", sp.worldSpace);
                sp.localPosition = EditorGUILayout.Vector3Field("Position", sp.localPosition);
                sp.localEulerAngles = EditorGUILayout.Vector3Field("Euler", sp.localEulerAngles);
            }
            else if (a is SetPropertyAction set)
            {
                set.targetKey = EditorGUILayout.TextField("Target Key", set.targetKey);
                set.componentTypeName = EditorGUILayout.TextField("Component Type", set.componentTypeName);
                set.memberName = EditorGUILayout.TextField("Member", set.memberName);
                DrawNarrativeValue("Value", ref set.value);
            }
            else if (a is CallMethodAction call)
            {
                call.targetKey = EditorGUILayout.TextField("Target Key", call.targetKey);
                call.componentTypeName = EditorGUILayout.TextField("Component Type", call.componentTypeName);
                call.methodName = EditorGUILayout.TextField("Method", call.methodName);

                int n = Mathf.Max(0, EditorGUILayout.IntField("Arg Count", call.args != null ? call.args.Length : 0));
                if (call.args == null || call.args.Length != n)
                {
                    Array.Resize(ref call.args, n);
                }

                for (int i = 0; i < n; i++)
                {
                    var v = call.args[i];
                    DrawNarrativeValue($"Arg {i}", ref v);
                    call.args[i] = v;
                }
            }
            else if (a is RunBehaviorTreeAction run)
            {
                run.actorKey = EditorGUILayout.TextField("Actor Key", run.actorKey);
                run.goal.goalName = EditorGUILayout.TextField("Goal Name", run.goal.goalName);
                run.goal.type = (GoalType)EditorGUILayout.EnumPopup("Goal Type", run.goal.type);
                run.goal.targetKey = EditorGUILayout.TextField("Target Key", run.goal.targetKey);
                run.goal.targetPosition = EditorGUILayout.Vector3Field("Target Position", run.goal.targetPosition);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tree, "Edit Narrative Action");
                EditorUtility.SetDirty(tree);
            }
        }

        private enum NarrativeActionKind
        {
            CallMethod,
            SetProperty,
            SpawnPrefab,
            RunBehaviorTree
        }

        private static NarrativeActionKind GetKind(NarrativeActionSpec a)
        {
            return a switch
            {
                SpawnPrefabAction => NarrativeActionKind.SpawnPrefab,
                SetPropertyAction => NarrativeActionKind.SetProperty,
                CallMethodAction => NarrativeActionKind.CallMethod,
                RunBehaviorTreeAction => NarrativeActionKind.RunBehaviorTree,
                _ => NarrativeActionKind.CallMethod
            };
        }

        private static void DrawNarrativeValue(string label, ref NarrativeValue v)
        {
            v.type = (NarrativeValueType)EditorGUILayout.EnumPopup(label + " Type", v.type);
            switch (v.type)
            {
                case NarrativeValueType.Bool:
                    v.boolValue = EditorGUILayout.Toggle(label, v.boolValue);
                    break;
                case NarrativeValueType.Int:
                    v.intValue = EditorGUILayout.IntField(label, v.intValue);
                    break;
                case NarrativeValueType.Float:
                    v.floatValue = EditorGUILayout.FloatField(label, v.floatValue);
                    break;
                case NarrativeValueType.String:
                    v.stringValue = EditorGUILayout.TextField(label, v.stringValue);
                    break;
                case NarrativeValueType.Vector3:
                    v.vector3Value = EditorGUILayout.Vector3Field(label, v.vector3Value);
                    break;
                case NarrativeValueType.ObjectKey:
                    v.objectKey = EditorGUILayout.TextField(label + " Key", v.objectKey);
                    break;
            }
        }
    }

    internal sealed class NarrativeGraphView : GraphView
    {
        public Action<NarrativeNode> OnNodeSelected;

        private NarrativeTreeAsset tree;
        private readonly Dictionary<string, NarrativeGraphNode> nodesById = new Dictionary<string, NarrativeGraphNode>();

        public NarrativeGraphView()
        {
            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

        public void Populate(NarrativeTreeAsset tree)
        {
            this.tree = tree;
            DeleteElements(graphElements.ToList());
            nodesById.Clear();

            if (tree == null || tree.root == null)
                return;

            // Build nodes
            int depth = 0;
            BuildNodeRecursive(tree.root, depth, 0);

            // Build edges
            ConnectRecursive(tree.root);
        }

        private int BuildNodeRecursive(NarrativeNode n, int depth, int siblingIndex)
        {
            var gn = new NarrativeGraphNode(n);
            gn.SetPosition(new Rect(40 + depth * 240, 40 + siblingIndex * 120, 200, 80));
            gn.OnSelectedCallback = () => OnNodeSelected?.Invoke(n);

            AddElement(gn);
            nodesById[n.id] = gn;

            int nextSibling = siblingIndex + 1;
            if (n is NarrativeSequenceNode seq && seq.children != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                    nextSibling = BuildNodeRecursive(seq.children[i], depth + 1, nextSibling);
            }
            else if (n is NarrativeSelectorNode sel && sel.children != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                    nextSibling = BuildNodeRecursive(sel.children[i], depth + 1, nextSibling);
            }

            return nextSibling;
        }

        private void ConnectRecursive(NarrativeNode n)
        {
            if (n == null) return;
            if (!nodesById.TryGetValue(n.id, out var parentNode)) return;

            if (n is NarrativeSequenceNode seq && seq.children != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                {
                    var child = seq.children[i];
                    if (child == null) continue;
                    Connect(parentNode, child);
                    ConnectRecursive(child);
                }
            }
            else if (n is NarrativeSelectorNode sel && sel.children != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                {
                    var child = sel.children[i];
                    if (child == null) continue;
                    Connect(parentNode, child);
                    ConnectRecursive(child);
                }
            }
        }

        private void Connect(NarrativeGraphNode parent, NarrativeNode child)
        {
            if (!nodesById.TryGetValue(child.id, out var childNode)) return;

            var edge = parent.output.ConnectTo(childNode.input);
            AddElement(edge);
        }
    }

    internal sealed class NarrativeGraphNode : Node
    {
        public NarrativeNode data;
        public Action OnSelectedCallback;

        public Port input;
        public Port output;

        public NarrativeGraphNode(NarrativeNode data)
        {
            this.data = data;
            title = data != null ? data.title : "Node";

            input = Port.Create<Edge>(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = "";
            inputContainer.Add(input);

            output = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = "";
            outputContainer.Add(output);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnSelectedCallback?.Invoke();
        }
    }
}
#endif

