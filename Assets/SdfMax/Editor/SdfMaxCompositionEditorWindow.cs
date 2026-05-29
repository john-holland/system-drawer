#if UNITY_EDITOR
using System.Collections.Generic;
using SpatialVolumes;
using SdfMax;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace SdfMax.Editor
{
    public sealed class SdfMaxCompositionEditorWindow : EditorWindow
    {
        SpatialVolumeProvider _provider;
        SdfMaxCompositionAsset _composition;
        SerializedObject _compositionSo;
        SerializedProperty _nodesProp;
        ReorderableList _nodeList;
        int _selectedNode = -1;

        Scene _previewScene;
        Camera _previewCamera;
        GameObject _previewRoot;
        RenderTexture _previewRt;
        float _orbitYaw = 25f;
        float _orbitPitch = 18f;
        float _orbitDistance = 4f;
        Vector3 _previewPivot = Vector3.zero;
        int _dragNodeIndex = -1;
        bool _showSurfaceMesh = true;
        GameObject _previewSurfaceGo;
        Vector2 _leftScroll;
        const int PreviewSize = 512;
        const string PreviewSceneName = "SdfMaxPreview_Scene";

        [MenuItem("Window/System Drawer/SDF Max Composition Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<SdfMaxCompositionEditorWindow>(false, "SDF Max", true);
            w.minSize = new Vector2(720, 480);
        }

        public static void ShowWindow(SpatialVolumeProvider provider, SdfMaxCompositionAsset composition = null)
        {
            var w = GetWindow<SdfMaxCompositionEditorWindow>(false, "SDF Max", true);
            w.Bind(provider, composition);
            w.Show();
        }

        void OnEnable()
        {
            EnsurePreviewScene();
        }

        void OnDisable()
        {
            CleanupPreview();
        }

        void Bind(SpatialVolumeProvider provider, SdfMaxCompositionAsset composition)
        {
            _provider = provider;
            _composition = composition != null ? composition : provider != null ? provider.composition : null;
            RebindSerialized();
            RefreshPreviewMesh();
        }

        void RebindSerialized()
        {
            _compositionSo = _composition != null ? new SerializedObject(_composition) : null;
            _nodesProp = _compositionSo != null ? _compositionSo.FindProperty("nodes") : null;
            BuildNodeList();
        }

        void BuildNodeList()
        {
            if (_nodesProp == null)
            {
                _nodeList = null;
                return;
            }

            _nodeList = new ReorderableList(_compositionSo, _nodesProp, true, true, true, true);
            _nodeList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Composition nodes");
            _nodeList.drawElementCallback = (rect, index, active, focused) =>
            {
                var el = _nodesProp.GetArrayElementAtIndex(index);
                var op = el.FindPropertyRelative("op");
                string label = op != null ? ((SdfMaxOp)op.enumValueIndex).ToString() : "Node";
                EditorGUI.LabelField(rect, $"[{index}] {label}");
            };
            _nodeList.onSelectCallback = list => _selectedNode = list.index;
            _nodeList.onAddCallback = list =>
            {
                list.serializedProperty.arraySize++;
                var el = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                el.FindPropertyRelative("op").enumValueIndex = (int)SdfMaxOp.PrimitiveLeaf;
                el.FindPropertyRelative("primitiveType").enumValueIndex = (int)SdfPrimitiveType.Box;
            };
        }

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("SDF Max Composition Editor", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newProvider = (SpatialVolumeProvider)EditorGUILayout.ObjectField("Provider", _provider, typeof(SpatialVolumeProvider), true);
            var newComposition = (SdfMaxCompositionAsset)EditorGUILayout.ObjectField("Composition", _composition, typeof(SdfMaxCompositionAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                Bind(newProvider, newComposition);
            }

            if (_provider != null)
            {
                _provider.SyncSDFTreeShape = EditorGUILayout.Toggle("Sync SDF Tree Shape", _provider.SyncSDFTreeShape);
                EditorGUILayout.LabelField("Backend", _provider.backend.ToString());
                _showSurfaceMesh = EditorGUILayout.Toggle("Show surface mesh", _showSurfaceMesh);
                if (GUILayout.Button("Rebuild Surface Mesh", GUILayout.Height(22)))
                {
                    var meshSurface = _provider.GetComponent<SdfMaxMeshSurface>();
                    if (meshSurface != null)
                        meshSurface.RebuildSurfaceMesh();
                    var skinned = _provider.GetComponent<SdfMaxSkinnedMeshSurface>();
                    if (skinned != null)
                        skinned.RebuildSurfaceMesh();
                    RefreshPreviewMesh();
                }
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_provider == null))
            {
                if (GUILayout.Button("Auto Calculate", GUILayout.Height(26)))
                {
                    if (_provider.backend == VolumeBackend.MeshConvexTree)
                        SdfMaxEditorUndo.ApplyAutoCalculate(_provider, _previewRoot != null ? _previewRoot.transform : _provider.transform);
                    else
                        SdfMaxEditorUndo.ApplyAutoCalculate(_provider, _previewRoot != null ? _previewRoot.transform : _provider.transform);
                    RebindSerialized();
                    RefreshPreviewMesh();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_provider != null && _provider.backend == VolumeBackend.MeshConvexTree)
            {
                EditorGUILayout.HelpBox("Mesh Convex Tree backend: Auto Calculate rebuilds mesh cache. Open Convex Mesh Collider Debug for scene gizmos.", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawPreviewPanel();
            EditorGUILayout.EndHorizontal();

            DrawSelectedNodeInspector();
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.ExpandHeight(true));
            if (_compositionSo != null)
            {
                _compositionSo.Update();
                if (_nodeList != null)
                    _nodeList.DoLayoutList();
                _compositionSo.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a composition asset or run Auto Calculate.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, 320, GUILayout.ExpandWidth(true));
            EnsurePreviewScene();
            RefreshPreviewMesh();

            if (_previewRt != null && _previewCamera != null)
            {
                UpdateCamera();
                DrawPreviewGizmos();
                if (Event.current.type == EventType.Repaint)
                    _previewCamera.Render();
                EditorGUI.DrawPreviewTexture(previewRect, _previewRt, null, ScaleMode.ScaleToFit);
                HandlePreviewInput(previewRect);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.17f));
            }
            EditorGUILayout.EndVertical();
        }

        void DrawSelectedNodeInspector()
        {
            if (_compositionSo == null || _selectedNode < 0 || _nodesProp == null || _selectedNode >= _nodesProp.arraySize)
                return;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Selected node", EditorStyles.boldLabel);
            _compositionSo.Update();
            var el = _nodesProp.GetArrayElementAtIndex(_selectedNode);
            EditorGUILayout.PropertyField(el, true);
            _compositionSo.ApplyModifiedProperties();
            if (_provider != null && _provider.SyncSDFTreeShape)
                SpatialVolumeCacheRegistry.EnsureBuilt(_provider, force: true);
        }

        void HandlePreviewInput(Rect previewRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseUp)
            {
                _dragNodeIndex = -1;
            }

            if (!previewRect.Contains(e.mousePosition))
                return;

            if (e.alt && e.type == EventType.MouseDrag)
            {
                _orbitYaw += e.delta.x * 0.5f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - e.delta.y * 0.5f, -89f, 89f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (_composition != null && _composition.nodes != null && _selectedNode >= 0 && _selectedNode < _composition.nodes.Count)
                {
                    if (PreviewScreenToWorld.TryHitPlane(
                            previewRect, e.mousePosition, new Vector2(_previewRt.width, _previewRt.height),
                            _previewCamera, _previewPivot, Vector3.up, out Vector3 hit))
                    {
                        Undo.RecordObject(_composition, "Move SDF Node");
                        var node = _composition.nodes[_selectedNode];
                        if (_previewRoot != null)
                            node.localPosition = _previewRoot.transform.InverseTransformPoint(hit);
                        else
                            node.localPosition = hit;
                        EditorUtility.SetDirty(_composition);
                        _dragNodeIndex = _selectedNode;
                        if (_provider != null)
                            SpatialVolumeCacheRegistry.EnsureBuilt(_provider, force: true);
                        e.Use();
                    }
                }
            }

            if (_dragNodeIndex >= 0 && e.type == EventType.MouseDrag && _composition != null)
            {
                if (PreviewScreenToWorld.TryHitPlane(
                        previewRect, e.mousePosition, new Vector2(_previewRt.width, _previewRt.height),
                        _previewCamera, _previewPivot, Vector3.up, out Vector3 hit))
                {
                    var node = _composition.nodes[_dragNodeIndex];
                    if (_previewRoot != null)
                        node.localPosition = _previewRoot.transform.InverseTransformPoint(hit);
                    else
                        node.localPosition = hit;
                    EditorUtility.SetDirty(_composition);
                    if (_provider != null)
                        SpatialVolumeCacheRegistry.EnsureBuilt(_provider, force: true);
                    e.Use();
                    Repaint();
                }
            }

            if (e.type == EventType.ScrollWheel)
            {
                _orbitDistance = Mathf.Clamp(_orbitDistance + e.delta.y * 0.15f, 1f, 30f);
                e.Use();
                Repaint();
            }
        }

        void DrawPreviewGizmos()
        {
            if (_previewRoot == null || _composition == null)
                return;

            var graph = new SdfMaxExpressionGraph(_composition, _provider != null ? _provider.profile : null, _previewRoot.transform.localToWorldMatrix);
            Bounds wb = graph.ComputeWorldBounds();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wb.center, wb.size);

            if (_provider != null && SpatialVolumeCacheRegistry.TryGetBackend(_provider, out var backend))
            {
                var leaves = new List<SpatialVolumeLeaf>();
                backend.CollectLeaves(wb, 0f, leaves);
                Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
                for (int i = 0; i < leaves.Count; i++)
                    Gizmos.DrawWireCube(leaves[i].Bounds.center, leaves[i].Bounds.size);
            }
        }

        void EnsurePreviewScene()
        {
            if (_previewScene.IsValid() && _previewScene.isLoaded && _previewCamera != null)
                return;

            _previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _previewScene.name = PreviewSceneName;

            var camGo = new GameObject("SdfMaxPreviewCamera");
            _previewCamera = camGo.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            SceneManager.MoveGameObjectToScene(camGo, _previewScene);

            _previewRoot = new GameObject("SdfMaxPreviewRoot");
            SceneManager.MoveGameObjectToScene(_previewRoot, _previewScene);

            if (_previewRt == null || !_previewRt.IsCreated())
            {
                _previewRt = new RenderTexture(PreviewSize, PreviewSize, 24);
                _previewRt.Create();
            }
            _previewCamera.targetTexture = _previewRt;
        }

        void RefreshPreviewMesh()
        {
            if (_previewRoot == null)
                return;

            for (int i = _previewRoot.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(_previewRoot.transform.GetChild(i).gameObject);
            _previewSurfaceGo = null;

            if (_provider == null)
                return;

            if (_showSurfaceMesh && _provider.composition != null &&
                _provider.backend == VolumeBackend.SdfMaxComposition)
            {
                var profile = _provider.profile;
                var graph = new SdfMaxExpressionGraph(
                    _provider.composition,
                    profile,
                    _previewRoot.transform.localToWorldMatrix);
                var eval = new SdfMaxEvaluator(graph);
                Bounds localBounds = eval.WorldBounds;
                localBounds.center = _previewRoot.transform.InverseTransformPoint(localBounds.center);
                localBounds.extents = _previewRoot.transform.InverseTransformVector(localBounds.extents);
                int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(profile, _provider.composition);
                var data = SdfMaxSurfaceMesher.Build(
                    eval,
                    localBounds,
                    _previewRoot.transform.localToWorldMatrix,
                    profile != null ? profile.surfaceIsoLevel : 0f,
                    profile != null ? profile.surfaceGridRes : 24,
                    ver,
                    true);
                if (data.IsValid)
                {
                    _previewSurfaceGo = new GameObject("PreviewSurfaceMesh");
                    _previewSurfaceGo.transform.SetParent(_previewRoot.transform, false);
                    var mf = _previewSurfaceGo.AddComponent<MeshFilter>();
                    var mesh = new Mesh { name = "SdfMaxPreviewSurface" };
                    data.ApplyToMesh(mesh, true);
                    mf.sharedMesh = mesh;
                    var mr = _previewSurfaceGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = new Material(Shader.Find("Standard"));
                    _previewPivot = localBounds.center;
                }
            }

            if (_previewSurfaceGo == null)
            {
                MeshCollider mc = _provider.meshCollider;
                if (mc == null)
                    mc = _provider.GetComponent<MeshCollider>();
                if (mc != null && mc.sharedMesh != null)
                {
                    var child = new GameObject("PreviewMesh");
                    child.transform.SetParent(_previewRoot.transform, false);
                    var mf = child.AddComponent<MeshFilter>();
                    mf.sharedMesh = mc.sharedMesh;
                    var mr = child.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = new Material(Shader.Find("Standard"));
                    _previewPivot = child.transform.position;
                }
                else
                    _previewPivot = Vector3.zero;
            }

            if (_provider.composition != null)
                SpatialVolumeCacheRegistry.EnsureBuilt(_provider, force: false);
        }

        void UpdateCamera()
        {
            if (_previewCamera == null)
                return;
            Quaternion rot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -_orbitDistance);
            _previewCamera.transform.position = _previewPivot + offset;
            _previewCamera.transform.LookAt(_previewPivot);
        }

        void CleanupPreview()
        {
            if (_previewCamera != null)
                _previewCamera.targetTexture = null;
            if (_previewRt != null && _previewRt.IsCreated())
                _previewRt.Release();
            _previewRt = null;
            if (_previewScene.IsValid() && _previewScene.isLoaded)
                EditorSceneManager.CloseScene(_previewScene, true);
        }
    }
}
#endif
