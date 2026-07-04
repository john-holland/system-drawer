#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative.Music;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>State machine editor for sectional music composition with green overlay rewiring.</summary>
    public sealed class MusicSummaryStateMachineEditorWindow : EditorWindow
    {
        MusicCompositionPlanAsset _plan;
        MusicSectionLibrary _library;
        Vector2 _scroll;
        readonly HashSet<string> _selectedNodes = new HashSet<string>();
        bool _showBaseline = true;
        string _wireFromNodeId;
        string _wireToNodeId;
        MusicStemRole _wireLane = MusicStemRole.Background;
        Rect _graphArea = new Rect(20, 120, 600, 280);

        [MenuItem("Window/System Drawer/Music/Composition Summary", false, 350)]
        public static void OpenWindow()
        {
            var w = GetWindow<MusicSummaryStateMachineEditorWindow>();
            w.titleContent = new GUIContent("Music Composition Summary");
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Music Composition Summary", EditorStyles.boldLabel);

            _plan = (MusicCompositionPlanAsset)EditorGUILayout.ObjectField("Composition Plan", _plan, typeof(MusicCompositionPlanAsset), false);
            _library = (MusicSectionLibrary)EditorGUILayout.ObjectField("Section Library", _library, typeof(MusicSectionLibrary), false);
            _showBaseline = EditorGUILayout.Toggle("Show Procedural Baseline", _showBaseline);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Selected", GUILayout.Width(120)))
                ResetSelected();
            if (GUILayout.Button("Clear Selection", GUILayout.Width(120)))
                _selectedNodes.Clear();
            EditorGUILayout.EndHorizontal();

            if (_plan == null)
            {
                EditorGUILayout.HelpBox("Assign a MusicCompositionPlanAsset.", MessageType.Info);
                return;
            }

            DrawSummaryPanel();
            EditorGUILayout.Space();
            DrawGraph();
            DrawWireControls();
        }

        void DrawSummaryPanel()
        {
            EditorGUILayout.LabelField("Causality span", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"{_plan.causalityFromLeaf} → {_plan.causalityToLeaf}", EditorStyles.wordWrappedLabel);
            if (_plan.proceduralSnapshot?.nodes != null)
                EditorGUILayout.LabelField($"Procedural nodes: {_plan.proceduralSnapshot.nodes.Count}", EditorStyles.miniLabel);
        }

        void DrawGraph()
        {
            GUILayout.Label("State machines (green = composition overlay)", EditorStyles.miniBoldLabel);
            _graphArea = GUILayoutUtility.GetRect(640, 300, GUILayout.ExpandWidth(true));

            GUI.Box(_graphArea, GUIContent.none);

            var nodes = _plan.nodes.Count > 0 ? _plan.nodes : _plan.proceduralSnapshot?.nodes;
            if (nodes == null) return;

            Dictionary<string, Vector2> positions = MusicCompositionGraphDrawer.LayoutNodes(nodes, _graphArea);

            if (_showBaseline && _plan.proceduralSnapshot?.baselineEdges != null)
            {
                for (int i = 0; i < _plan.proceduralSnapshot.baselineEdges.Count; i++)
                {
                    MusicCompositionOverlayEdge e = _plan.proceduralSnapshot.baselineEdges[i];
                    if (positions.TryGetValue(e.fromNodeId, out Vector2 a) &&
                        positions.TryGetValue(e.toNodeId, out Vector2 b))
                        MusicCompositionGraphDrawer.DrawEdge(a, b, e.kind, overlay: false);
                }
            }

            for (int i = 0; i < _plan.overlayEdges.Count; i++)
            {
                MusicCompositionOverlayEdge e = _plan.overlayEdges[i];
                if (positions.TryGetValue(e.fromNodeId, out Vector2 a) &&
                    positions.TryGetValue(e.toNodeId, out Vector2 b))
                    MusicCompositionGraphDrawer.DrawEdge(a, b, e.kind, overlay: true);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                MusicBehaviorNode n = nodes[i];
                if (!positions.TryGetValue(n.nodeId, out Vector2 p)) continue;
                bool sel = _selectedNodes.Contains(n.nodeId);
                var clickRect = new Rect(p.x - 24, p.y - 24, 48, 48);
                MusicCompositionGraphDrawer.DrawNode(p, n, sel, suspended: n.exitCut == MusicPointCutMode.SuspendForReturn);
                if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.control)
                    {
                        if (!_selectedNodes.Add(n.nodeId))
                            _selectedNodes.Remove(n.nodeId);
                    }
                    else
                    {
                        _selectedNodes.Clear();
                        _selectedNodes.Add(n.nodeId);
                        _wireFromNodeId = n.nodeId;
                    }
                    Repaint();
                }
            }
        }

        void DrawWireControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rewire (green overlay)", EditorStyles.boldLabel);
            _wireFromNodeId = EditorGUILayout.TextField("From Node Id", _wireFromNodeId);
            _wireToNodeId = EditorGUILayout.TextField("To Node Id", _wireToNodeId);
            _wireLane = (MusicStemRole)EditorGUILayout.EnumPopup("Lane", _wireLane);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Forward Edge"))
            {
                if (!string.IsNullOrEmpty(_wireFromNodeId) && !string.IsNullOrEmpty(_wireToNodeId))
                {
                    _plan.AddOrReplaceOverlayEdge(new MusicCompositionOverlayEdge
                    {
                        fromNodeId = _wireFromNodeId,
                        toNodeId = _wireToNodeId,
                        kind = MusicOverlayEdgeKind.Forward,
                        lane = _wireLane
                    });
                    EditorUtility.SetDirty(_plan);
                }
            }
            if (GUILayout.Button("Add Return Edge"))
            {
                if (!string.IsNullOrEmpty(_wireFromNodeId) && !string.IsNullOrEmpty(_wireToNodeId))
                {
                    _plan.AddOrReplaceOverlayEdge(new MusicCompositionOverlayEdge
                    {
                        fromNodeId = _wireFromNodeId,
                        toNodeId = _wireToNodeId,
                        kind = MusicOverlayEdgeKind.Return,
                        lane = _wireLane
                    });
                    EditorUtility.SetDirty(_plan);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedNodes.Count == 1)
            {
                foreach (string id in _selectedNodes)
                {
                    MusicBehaviorNode node = _plan.FindNode(id);
                    if (node == null) continue;
                    node.exitCut = (MusicPointCutMode)EditorGUILayout.EnumPopup("Exit Point Cut", node.exitCut);
                    node.enterCut = (MusicPointCutMode)EditorGUILayout.EnumPopup("Enter Point Cut", node.enterCut);
                }
            }
        }

        void ResetSelected()
        {
            if (_selectedNodes.Count == 0)
            {
                EditorUtility.DisplayDialog("Reset Selected", "Select one or more nodes first.", "OK");
                return;
            }

            bool downstreamOnly = EditorUtility.DisplayDialog(
                "Reset Selected",
                "Reset selected nodes to procedural snapshot along green dependencies?",
                "Reset",
                "Cancel");

            if (!downstreamOnly) return;

            MusicCompositionResetResult result = _plan.ResetSelected(_selectedNodes, downstreamOnly: false);
            EditorUtility.SetDirty(_plan);
            Debug.Log($"[MusicComposition] Reset {result.resetNodeIds.Count} nodes, removed {result.removedEdges.Count} green edges.");
        }
    }
}
#endif
