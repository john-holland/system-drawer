#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Audio;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Audio.EditorTools
{
    /// <summary>Nested physical/digital audio equipment timeline (PerfTrace-style breadcrumb).</summary>
    public sealed class AudioEquipmentTimelineWindow : EditorWindow
    {
        DigitalEffectsMachine _machine;
        AnalogReferenceMachine _analog;
        readonly List<AudioEquipmentTraceNode> _focus = new List<AudioEquipmentTraceNode>();
        Vector2 _scroll;
        string _selectedId;

        [MenuItem("Window/System Drawer/Music/Audio Equipment Timeline", false, 351)]
        public static void Open()
        {
            var w = GetWindow<AudioEquipmentTimelineWindow>("Audio Equipment");
            w.minSize = new Vector2(520, 360);
            w.Show();
        }

        public static void OpenFor(DigitalEffectsMachine machine)
        {
            var w = GetWindow<AudioEquipmentTimelineWindow>("Audio Equipment");
            w._machine = machine;
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Audio Equipment Timeline", EditorStyles.boldLabel);
            _machine = (DigitalEffectsMachine)EditorGUILayout.ObjectField("Digital Machine", _machine, typeof(DigitalEffectsMachine), true);
            _analog = (AnalogReferenceMachine)EditorGUILayout.ObjectField("Analog Machine", _analog, typeof(AnalogReferenceMachine), true);

            if (_machine == null)
            {
                EditorGUILayout.HelpBox("Assign a DigitalEffectsMachine (physical rack GameObject).", MessageType.Info);
                return;
            }

            if (_machine.powerBudget != null && _machine.UnrealisticWarning)
                EditorGUILayout.HelpBox(_machine.powerBudget.WarningMessage, MessageType.Warning);

            DrawBreadcrumb();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add effect before"))
                AddEffect(before: true);
            if (GUILayout.Button("Add effect after"))
                AddEffect(before: false);
            if (GUILayout.Button("Open state machine editor"))
                EditorApplication.ExecuteMenuItem("Window/System Drawer/Music/Composition Summary");
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var focus = _focus.Count > 0 ? _focus[_focus.Count - 1] : _machine.equipmentTrace.root;
            DrawNodeChildren(focus);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("DSP graph order", EditorStyles.boldLabel);
            for (int i = 0; i < _machine.graph.Count; i++)
            {
                var n = _machine.graph[i];
                if (n == null) continue;
                EditorGUILayout.BeginHorizontal("box");
                bool sel = _selectedId == n.id;
                if (GUILayout.Toggle(sel, $"{i}: {n.label} ({n.kind})", "Button"))
                    _selectedId = n.id;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawBreadcrumb()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Root", EditorStyles.miniButton, GUILayout.Width(48)))
                _focus.Clear();
            for (int i = 0; i < _focus.Count; i++)
            {
                int idx = i;
                if (GUILayout.Button(_focus[i].label, EditorStyles.miniButton))
                {
                    while (_focus.Count > idx + 1)
                        _focus.RemoveAt(_focus.Count - 1);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawNodeChildren(AudioEquipmentTraceNode node)
        {
            if (node?.children == null) return;
            for (int i = 0; i < node.children.Count; i++)
            {
                var c = node.children[i];
                if (c == null) continue;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(c.label, GUILayout.ExpandWidth(true)))
                {
                    _selectedId = c.id;
                    _focus.Add(c);
                }
                EditorGUILayout.LabelField(c.kind.ToString(), GUILayout.Width(110));
                EditorGUILayout.EndHorizontal();
            }
        }

        void AddEffect(bool before)
        {
            var node = new DigitalEffectNode
            {
                label = before ? "Insert Before" : "Insert After",
                kind = DigitalEffectKind.LowPass
            };
            string sel = string.IsNullOrEmpty(_selectedId) && _machine.graph.Count > 0
                ? _machine.graph[0].id
                : _selectedId;
            if (before) _machine.InsertBefore(sel, node);
            else _machine.InsertAfter(sel, node);
            _selectedId = node.id;
            EditorUtility.SetDirty(_machine);
        }
    }
}
#endif
