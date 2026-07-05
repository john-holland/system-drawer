#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Open;
using Locomotion.Open.Topology;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Locomotion.Open.Editor
{
    public sealed class OpenCloseTopologyEditorWindow : EditorWindow
    {
        OpenCloseTopologyAsset _topology;
        GameObject _sceneRoot;
        int _selectedNodeIndex;
        List<OpenCloseTopologyNode> _flat = new List<OpenCloseTopologyNode>();
        Vector2 _treeScroll;
        Vector2 _compileScroll;
        string _compilePreview = "";
        bool _useActiveScene;
        Scene _previewScene;
        UnityEngine.Camera _previewCamera;
        RenderTexture _previewRt;
        GameObject _previewRoot;
        const int PreviewSize = 384;

        [MenuItem("Window/Locomotion/Open-Close Topology Preview")]
        public static void ShowWindow()
        {
            var w = GetWindow<OpenCloseTopologyEditorWindow>("Open-Close Topology");
            w.minSize = new Vector2(520, 480);
        }

        void OnEnable() => EnsurePreviewScene();
        void OnDisable() => CleanupPreview();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Open/Close Topology", EditorStyles.boldLabel);
            _topology = (OpenCloseTopologyAsset)EditorGUILayout.ObjectField("Topology Asset", _topology, typeof(OpenCloseTopologyAsset), false);
            _sceneRoot = (GameObject)EditorGUILayout.ObjectField("Scene Root", _sceneRoot, typeof(GameObject), true);
            _useActiveScene = EditorGUILayout.Toggle("Preview in active scene", _useActiveScene);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan hierarchy"))
                ScanHierarchy();
            if (GUILayout.Button("Bake default poses"))
                BakeDefaults();
            if (GUILayout.Button("Compile BT"))
                CompilePreview();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play full sequence"))
                PlaySequence(linearOnly: false);
            if (GUILayout.Button("Play linear only"))
                PlaySequence(linearOnly: true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll, GUILayout.Width(260));
            DrawTree();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical();
            DrawPreview();
            DrawSelectedNodeInspector();
            if (!string.IsNullOrEmpty(_compilePreview))
            {
                EditorGUILayout.LabelField("Compiled BT", EditorStyles.boldLabel);
                _compileScroll = EditorGUILayout.BeginScrollView(_compileScroll, GUILayout.Height(120));
                EditorGUILayout.TextArea(_compilePreview);
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void DrawTree()
        {
            RebuildFlat();
            for (int i = 0; i < _flat.Count; i++)
            {
                var n = _flat[i];
                if (n == null) continue;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedNodeIndex == i, n.nodeId ?? "?", EditorStyles.miniButton))
                    _selectedNodeIndex = i;
                n.enabledInGameplay = EditorGUILayout.Toggle(n.enabledInGameplay, GUILayout.Width(18));
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawSelectedNodeInspector()
        {
            RebuildFlat();
            if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _flat.Count)
                return;
            var n = _flat[_selectedNodeIndex];
            EditorGUILayout.LabelField($"Stop: {n.nodeId}", EditorStyles.boldLabel);
            n.arrivalBlendCoefficient = EditorGUILayout.Slider("Arrival blend", n.arrivalBlendCoefficient, 0f, 1f);
            n.reachRadiusMeters = EditorGUILayout.FloatField("Reach radius (m)", n.reachRadiusMeters);
            n.requireFacingTarget = EditorGUILayout.Toggle("Require facing", n.requireFacingTarget);
            n.autoCloseBt = (AutoCloseBtMode)EditorGUILayout.EnumPopup("Auto close BT", n.autoCloseBt);
            n.beatProfile = (OpenCloseBeatProfile)EditorGUILayout.ObjectField("Beat profile", n.beatProfile, typeof(OpenCloseBeatProfile), false);
            if (n.hasApproachAnchor)
                EditorGUILayout.Vector3Field("Approach anchor", n.approachAnchorWorld);
        }

        void DrawPreview()
        {
            var rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(true));
            if (_previewCamera != null && _previewRt != null)
            {
                _previewCamera.Render();
                EditorGUI.DrawPreviewTexture(rect, _previewRt);
            }
            else
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        void ScanHierarchy()
        {
            if (_topology == null)
            {
                _topology = CreateInstance<OpenCloseTopologyAsset>();
                AssetDatabase.CreateAsset(_topology, "Assets/locomotion/open/OpenCloseTopology.asset");
            }
            var root = _sceneRoot != null ? _sceneRoot.transform : Selection.activeTransform;
            if (root == null)
            {
                Debug.LogWarning("Select a scene root or assign Scene Root.");
                return;
            }
            _topology.rootTarget = root.gameObject;
            _topology.ClearTopology();
            _topology.Root = new OpenCloseTopologyNode { nodeId = root.name };
            ConcaveExposeScanner.ScanHierarchy(root, _topology, _topology.Root, _topology.defaultAutoCloseBt);
            EditorUtility.SetDirty(_topology);
            RebuildFlat();
        }

        void BakeDefaults()
        {
            if (_topology?.Root == null) return;
            foreach (var n in _topology.EnumerateDepthFirst())
            {
                if (n.beatProfile == null) continue;
                n.beatProfile.CopyFromNode(n);
            }
        }

        void CompilePreview()
        {
            if (_topology == null) return;
            var result = OpenCloseTopologyCompiler.CompilePreview(_topology);
            _compilePreview = string.Join("\n", result.previewLines);
            Repaint();
        }

        void PlaySequence(bool linearOnly)
        {
            if (_topology == null) return;
            _topology.linearOnly = linearOnly;
            var host = _sceneRoot != null ? _sceneRoot : _previewRoot;
            if (host == null) return;
            OpenCloseTopologyCompiler.BakeToScene(_topology, host);
        }

        void RebuildFlat()
        {
            _flat.Clear();
            if (_topology?.Root == null) return;
            foreach (var n in _topology.EnumerateDepthFirst())
                _flat.Add(n);
        }

        void EnsurePreviewScene()
        {
            if (_useActiveScene) return;
            if (_previewScene.IsValid() && _previewScene.isLoaded) return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _previewScene = SceneManager.GetActiveScene();
            var camGo = new GameObject("PreviewCamera");
            _previewCamera = camGo.AddComponent<UnityEngine.Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.22f);
            SceneManager.MoveGameObjectToScene(camGo, _previewScene);
            _previewRoot = new GameObject("PreviewRoot");
            SceneManager.MoveGameObjectToScene(_previewRoot, _previewScene);
            _previewRt = new RenderTexture(PreviewSize, PreviewSize, 24);
            _previewRt.Create();
            _previewCamera.targetTexture = _previewRt;
        }

        void CleanupPreview()
        {
            if (_previewRt != null)
            {
                _previewCamera.targetTexture = null;
                _previewRt.Release();
                DestroyImmediate(_previewRt);
            }
            if (_previewScene.IsValid() && _previewScene.isLoaded)
                EditorSceneManager.CloseScene(_previewScene, true);
        }
    }
}
#endif
