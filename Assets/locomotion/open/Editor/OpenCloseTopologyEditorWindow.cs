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
    /// <summary>Object open/close topology authoring: scan, edit beats, bake durable BT steps onto a PlanNode host.</summary>
    public sealed class OpenCloseTopologyEditorWindow : EditorWindow
    {
        OpenCloseTopologyAsset _topology;
        GameObject _sceneRoot;
        ObjectOpenCloseTopologyPlanNode _planHost;
        int _selectedNodeIndex;
        List<OpenCloseTopologyNode> _flat = new List<OpenCloseTopologyNode>();
        Vector2 _treeScroll;
        Vector2 _inspectorScroll;
        Vector2 _compileScroll;
        string _compilePreview = "";
        bool _useActiveScene;
        Scene _previewScene;
        UnityEngine.Camera _previewCamera;
        RenderTexture _previewRt;
        GameObject _previewRoot;
        const int PreviewSize = 320;

        [MenuItem("Window/Locomotion/Open-Close Topology Preview")]
        [MenuItem("Window/Locomotion/Object Open-Close Topology")]
        public static void ShowWindow()
        {
            var w = GetWindow<OpenCloseTopologyEditorWindow>("Object Open/Close");
            w.minSize = new Vector2(640, 520);
        }

        void OnEnable() => EnsurePreviewScene();
        void OnDisable() => CleanupPreview();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Object Open/Close Topology", EditorStyles.boldLabel);
            _topology = (OpenCloseTopologyAsset)EditorGUILayout.ObjectField("Topology Asset", _topology, typeof(OpenCloseTopologyAsset), false);
            _sceneRoot = (GameObject)EditorGUILayout.ObjectField("Scene Root", _sceneRoot, typeof(GameObject), true);
            _planHost = (ObjectOpenCloseTopologyPlanNode)EditorGUILayout.ObjectField(
                "PlanNode Host", _planHost, typeof(ObjectOpenCloseTopologyPlanNode), true);
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
            if (GUILayout.Button("Bake Steps to BT"))
                BakeStepsToBt();
            if (GUILayout.Button("Play full sequence"))
                PlaySequence(linearOnly: false);
            if (GUILayout.Button("Play linear only"))
                PlaySequence(linearOnly: true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll, GUILayout.Width(240));
            DrawTree();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical();
            DrawPreview();
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll, GUILayout.Height(280));
            DrawSelectedNodeInspector();
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(_compilePreview))
            {
                EditorGUILayout.LabelField("Compiled BT", EditorStyles.boldLabel);
                _compileScroll = EditorGUILayout.BeginScrollView(_compileScroll, GUILayout.Height(100));
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
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField($"Stop: {n.nodeId}", EditorStyles.boldLabel);
            n.jointKind = (OpenCloseJointKind)EditorGUILayout.EnumPopup("Joint kind", n.jointKind);
            n.arrivalBlendCoefficient = EditorGUILayout.Slider("Arrival blend", n.arrivalBlendCoefficient, 0f, 1f);
            n.reachRadiusMeters = EditorGUILayout.FloatField("Reach radius (m)", n.reachRadiusMeters);
            n.requireFacingTarget = EditorGUILayout.Toggle("Require facing", n.requireFacingTarget);
            n.autoCloseBt = (AutoCloseBtMode)EditorGUILayout.EnumPopup("Auto close BT", n.autoCloseBt);
            n.beatProfile = (OpenCloseBeatProfile)EditorGUILayout.ObjectField("Beat profile", n.beatProfile, typeof(OpenCloseBeatProfile), false);
            if (n.hasApproachAnchor)
                EditorGUILayout.Vector3Field("Approach anchor", n.approachAnchorWorld);

            var p = n.beatProfile;
            if (p != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Drive / animation", EditorStyles.boldLabel);
                p.openAngleDeg = EditorGUILayout.FloatField("Open angle (deg)", p.openAngleDeg);
                p.driveMode = (OpenCloseDriveMode)EditorGUILayout.EnumPopup("Drive mode", p.driveMode);
                p.openAnimationRef = EditorGUILayout.TextField("Open animation ref", p.openAnimationRef);
                p.closeAnimationRef = EditorGUILayout.TextField("Close animation ref", p.closeAnimationRef);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Beat messages", EditorStyles.boldLabel);
                p.soundOpen = (AudioClip)EditorGUILayout.ObjectField("Sound open", p.soundOpen, typeof(AudioClip), false);
                p.soundClose = (AudioClip)EditorGUILayout.ObjectField("Sound close", p.soundClose, typeof(AudioClip), false);
                p.dialogueSpanRef = EditorGUILayout.TextField("Dialogue span ref", p.dialogueSpanRef);
                p.questHintKind = (OpenCloseQuestHintKind)EditorGUILayout.EnumPopup("Quest hint", p.questHintKind);
                p.questObjectiveId = EditorGUILayout.TextField("Quest objective id", p.questObjectiveId);
                p.uiMessageId = EditorGUILayout.TextField("UI message id", p.uiMessageId);
                p.uiMessageText = EditorGUILayout.TextField("UI message text", p.uiMessageText);
                p.uiCloseMessageText = EditorGUILayout.TextField("UI close text", p.uiCloseMessageText);
                p.playMusicOnOpen = EditorGUILayout.Toggle("Play music on open", p.playMusicOnOpen);
                p.musicIdleLeafId = EditorGUILayout.TextField("Music idle leaf", p.musicIdleLeafId);
                p.musicActiveLeafId = EditorGUILayout.TextField("Music active leaf", p.musicActiveLeafId);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Beat Profile to edit drive mode and message events.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (_topology != null)
                    EditorUtility.SetDirty(_topology);
                if (p != null)
                    EditorUtility.SetDirty(p);
            }
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
                EditorUtility.SetDirty(n.beatProfile);
            }
        }

        void BakeStepsToBt()
        {
            if (_topology == null)
            {
                Debug.LogWarning("Assign a topology asset before baking steps.");
                return;
            }

            var hostGo = ResolvePlanHostGameObject();
            if (hostGo == null)
            {
                Debug.LogWarning("Assign PlanNode Host or Scene Root to bake BT steps.");
                return;
            }

            var plan = OpenCloseTopologyCompiler.BakePlanToScene(_topology, hostGo);
            _planHost = plan;
            EditorUtility.SetDirty(hostGo);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(hostGo.scene);
            Debug.Log($"Baked {_topology.name} → {hostGo.name} ({(plan != null ? plan.children.Count : 0)} stop children).");
        }

        GameObject ResolvePlanHostGameObject()
        {
            if (_planHost != null)
                return _planHost.gameObject;
            if (_sceneRoot != null)
                return _sceneRoot;
            return _previewRoot;
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
            var host = ResolvePlanHostGameObject();
            if (host == null) return;
            OpenCloseTopologyCompiler.BakePlanToScene(_topology, host);
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
