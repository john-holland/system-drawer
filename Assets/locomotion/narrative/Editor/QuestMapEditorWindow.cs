#if UNITY_EDITOR
using System.Collections;
using Locomotion.Narrative;
using SystemDrawer.Quest;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>Editor window: spatial tree, live map preview, objective inspector, compile/sync toolbar.</summary>
    public class QuestMapEditorWindow : EditorWindow
    {
        const string LittlePrinceFixture = @"
{P:quest|quest-set=little-prince-tour}""Explore the asteroid belt""
  {P:quest|objective=meet-fox|spatial4d=s4d-fox-vol|predicate4d=fox-met|completion4d=fox-dialogue-done}
    {P:quest|summary=Meet the fox on the equator|style=watercolor-storybook}
    {P:quest|travel-binding=fox-approach|map-layer=emergence|ui-bt=quest-journal-minimal}
{P:quest|end-block=little-prince-tour}";

        [MenuItem("Window/System Drawer/Quest Map")]
        public static void ShowWindow()
        {
            var win = GetWindow<QuestMapEditorWindow>("Quest Map");
            win.minSize = new Vector2(720f, 420f);
        }

        string _lemmaText = LittlePrinceFixture;
        string _setId = "little-prince-tour";
        string _spatial4dId = "s4d-fox-vol";
        string _compileStatus = "";
        Vector2 _treeScroll;
        Vector2 _inspectorScroll;
        QuestSpatialNodesResponse _nodes;
        QuestSpanParser.CompileResult _compiled;
        QuestMapRenderer _previewRenderer;
        RenderTexture _previewRt;

        void OnEnable()
        {
            _compiled = QuestSpanParser.Compile(_lemmaText, _setId);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Quest Map Editor", EditorStyles.boldLabel);
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawTreePanel();
            DrawMapPreview();
            DrawInspector();
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_compileStatus))
                EditorGUILayout.HelpBox(_compileStatus, MessageType.Info);
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _setId = EditorGUILayout.TextField("Set Id", _setId);
            _spatial4dId = EditorGUILayout.TextField("Spatial4D Id", _spatial4dId);
            if (GUILayout.Button("Compile Quest", GUILayout.Width(120)))
                CompileLocal();
            if (GUILayout.Button("Sync Spatial Nodes", GUILayout.Width(140)))
                EditorApplication.update += SyncSpatialOnce;
            if (GUILayout.Button("Refresh Map", GUILayout.Width(110)))
                RefreshPreview();
            EditorGUILayout.EndHorizontal();
        }

        void DrawTreePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220f));
            EditorGUILayout.LabelField("Spatial tree", EditorStyles.boldLabel);
            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll);
            if (_nodes?.nodes != null)
            {
                foreach (var n in _nodes.nodes)
                    EditorGUILayout.LabelField(n.label ?? n.id, EditorStyles.miniLabel);
            }
            else
                EditorGUILayout.LabelField("(Sync spatial nodes)", EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawMapPreview()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Map preview", EditorStyles.boldLabel);
            EnsurePreviewRenderer();
            if (_previewRt != null)
            {
                _previewRenderer.RenderSlice();
                var rect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(rect, _previewRt);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawInspector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260f));
            EditorGUILayout.LabelField("Lemma / objectives", EditorStyles.boldLabel);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll, GUILayout.ExpandHeight(true));
            _lemmaText = EditorGUILayout.TextArea(_lemmaText, GUILayout.MinHeight(180f));
            if (_compiled?.nodes != null)
            {
                foreach (var n in _compiled.nodes)
                    DrawNode(n, 0);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawNode(QuestNodeDto n, int depth)
        {
            EditorGUI.indentLevel = depth;
            string label = string.IsNullOrEmpty(n.objectiveId) ? n.kind : n.objectiveId;
            EditorGUILayout.LabelField(label, n.summary ?? n.text);
            if (n.children == null)
                return;
            foreach (var c in n.children)
                DrawNode(c, depth + 1);
        }

        void CompileLocal()
        {
            _compiled = QuestSpanParser.Compile(_lemmaText, _setId);
            int errors = 0;
            foreach (var issue in _compiled.issues)
                if (issue.level == "error") errors++;
            _compileStatus = errors == 0
                ? $"Compiled {_setId} ({_compiled.nodes.Count} root nodes)"
                : $"Compile failed: {errors} error(s)";
        }

        void SyncSpatialOnce()
        {
            EditorApplication.update -= SyncSpatialOnce;
            EditorCoroutineRunner.Run(FetchNodes());
        }

        IEnumerator FetchNodes()
        {
            QuestSpatialNodesResponse resp = null;
            yield return ContinuuuumQuestClient.FetchSpatialNodes(_spatial4dId, -1f, r => resp = r);
            _nodes = resp;
            Repaint();
        }

        void RefreshPreview()
        {
            EnsurePreviewRenderer();
            _previewRenderer?.RenderSlice();
            Repaint();
        }

        void EnsurePreviewRenderer()
        {
            if (_previewRenderer != null)
                return;
            var go = EditorUtility.CreateGameObjectWithHideFlags(
                "QuestMapPreview",
                HideFlags.HideAndDontSave,
                typeof(QuestMapRenderer));
            _previewRenderer = go.GetComponent<QuestMapRenderer>();
            _previewRenderer.profile = ScriptableObject.CreateInstance<QuestMapProfile>();
            _previewRt = new RenderTexture(256, 256, 0);
            _previewRenderer.outputTexture = _previewRt;
            var sliceGo = EditorUtility.CreateGameObjectWithHideFlags(
                "PreviewSlice",
                HideFlags.HideAndDontSave,
                typeof(Spatial4DQuestSliceSource));
            _previewRenderer.sliceSource = sliceGo.GetComponent<QuestSpatialSliceSource>();
        }

        void OnDisable()
        {
            if (_previewRenderer != null)
                DestroyImmediate(_previewRenderer.gameObject);
        }
    }

    static class EditorCoroutineRunner
    {
        public static void Run(IEnumerator routine)
        {
            void Tick()
            {
                if (!routine.MoveNext())
                    EditorApplication.update -= Tick;
            }
            EditorApplication.update += Tick;
        }
    }
}
#endif
