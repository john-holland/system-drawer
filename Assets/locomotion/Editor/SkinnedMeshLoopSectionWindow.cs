using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Vertex loop picker, multi-loop split preview, and prefab export for SkinnedMeshLoopSection.
/// </summary>
public sealed class SkinnedMeshLoopSectionWindow : EditorWindow
{
    SkinnedMeshLoopSection _section;
    Renderer _renderer;
    SkinnedMeshLoopSectionAsset _asset;
    int _activeLoop;
    int _cycleIndex;
    bool _cycleMode;
    readonly SkinnedMeshLoopCycleDebounce _debounce = new SkinnedMeshLoopCycleDebounce();
    readonly List<int> _zoneVerts = new List<int>();
    readonly List<int> _boundsOverlapVerts = new List<int>();
    int _hoverVertex = -1;
    int _hoverTriangle = -1;
    Mesh _baked;
    Vector2 _scroll;
    PreviewRenderUtility _preview;
    Vector2 _previewDrag;
    string _splitPreview = "";
    Material _previewMat;

    [MenuItem("Window/System Drawer/Mesh/Skinned Loop Section")]
    public static void OpenMenu() => Open(null);

    public static void Open(SkinnedMeshLoopSection section)
    {
        var win = GetWindow<SkinnedMeshLoopSectionWindow>("Skinned Loop Section");
        win.minSize = new Vector2(380f, 420f);
        if (section != null)
        {
            win._section = section;
            win._renderer = section.Renderer;
            win._asset = section.sectionAsset;
        }
        win.Show();
    }

    public static SkinnedMeshLoopSectionWindow FindOpen()
    {
        var windows = Resources.FindObjectsOfTypeAll<SkinnedMeshLoopSectionWindow>();
        if (windows == null)
            return null;
        for (int i = 0; i < windows.Length; i++)
        {
            if (windows[i] != null)
                return windows[i];
        }
        return null;
    }

    public static bool CanUpdateLoopTrianglesFromBounds(SkinnedMeshLoopSplitBounds box)
    {
        var win = FindOpen();
        return win != null && win.IsEditingBounds(box);
    }

    public static bool TryUpdateLoopTrianglesFromBounds(SkinnedMeshLoopSplitBounds box, out string error)
    {
        var win = FindOpen();
        if (win == null)
        {
            error = "Open the Skinned Loop Section window for this bounds.";
            return false;
        }
        return win.UpdateLoopTrianglesFromBounds(box, out error);
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGui;
        _preview = new PreviewRenderUtility();
        _preview.cameraFieldOfView = 30f;
        _preview.camera.nearClipPlane = 0.1f;
        _preview.camera.farClipPlane = 50f;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        if (_preview != null)
        {
            _preview.Cleanup();
            _preview = null;
        }
        if (_previewMat != null)
        {
            DestroyImmediate(_previewMat);
            _previewMat = null;
        }
        if (_baked != null)
        {
            DestroyImmediate(_baked);
            _baked = null;
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Skinned Mesh Loop Section", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign a SkinnedMeshRenderer or MeshRenderer. Select materials to auto break out along submesh triangles and material-boundary edges.",
            MessageType.None);

        _section = (SkinnedMeshLoopSection)EditorGUILayout.ObjectField(
            "Section", _section, typeof(SkinnedMeshLoopSection), true);
        if (_section != null)
        {
            _renderer = _section.Renderer;
            if (_asset == null)
                _asset = _section.sectionAsset;
        }
        _renderer = (Renderer)EditorGUILayout.ObjectField(
            "Renderer", _renderer, typeof(Renderer), true);
        if (_renderer != null && !(_renderer is SkinnedMeshRenderer) && !(_renderer is MeshRenderer))
            EditorGUILayout.HelpBox("Use a MeshRenderer or SkinnedMeshRenderer.", MessageType.Warning);
        _asset = (SkinnedMeshLoopSectionAsset)EditorGUILayout.ObjectField(
            "Section Asset", _asset, typeof(SkinnedMeshLoopSectionAsset), false);

        if (_section != null && _section.sectionAsset != _asset)
        {
            if (GUILayout.Button("Assign Asset To Component"))
            {
                Undo.RecordObject(_section, "Assign loop section asset");
                _section.sectionAsset = _asset;
                EditorUtility.SetDirty(_section);
            }
        }

        if (_section != null)
        {
            _section.RefreshMeshUpdated();
            if (_section.meshUpdated && !_section.useCached)
                EditorGUILayout.HelpBox("meshUpdated — loops skipped until useCached or overwrite.", MessageType.Error);
            else if (_section.meshUpdated && _section.useCached)
                EditorGUILayout.HelpBox("use cached", MessageType.Warning);
        }

        if (_asset == null)
        {
            if (GUILayout.Button("Create Section Asset"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Loop Section Asset", "SkinnedMeshLoopSection", "asset", "Save loop section asset");
                if (!string.IsNullOrEmpty(path))
                {
                    _asset = CreateInstance<SkinnedMeshLoopSectionAsset>();
                    if (_renderer != null)
                        _asset.CaptureOriginals(
                            SkinnedMeshLoopRendererUtil.SharedMesh(_renderer),
                            SkinnedMeshLoopHasher.CollectTextures(_renderer));
                    AssetDatabase.CreateAsset(_asset, path);
                    if (_section != null)
                    {
                        _section.sectionAsset = _asset;
                        EditorUtility.SetDirty(_section);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();
        _asset.splitMode = (SkinnedMeshLoopSplitMode)EditorGUILayout.EnumPopup("Split Mode", _asset.splitMode);
        _asset.zoneRadius = EditorGUILayout.Slider("Zone Radius", _asset.zoneRadius, 0.001f, 0.5f);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_asset);

        DrawLoops();
        DrawMaterials();
        DrawPreview();
        DrawActions();
        EditorGUILayout.EndScrollView();
    }

    void DrawLoops()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loops", EditorStyles.boldLabel);
        if (_asset.loops == null)
            _asset.loops = new List<SkinnedMeshLoopSectionAsset.LoopSection>();

        if (GUILayout.Button("Add Loop"))
        {
            Undo.RecordObject(_asset, "Add loop");
            _asset.AddLoop();
            _activeLoop = _asset.loops.Count - 1;
            EditorUtility.SetDirty(_asset);
        }

        for (int i = 0; i < _asset.loops.Count; i++)
        {
            var loop = _asset.loops[i];
            if (loop == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            bool active = i == _activeLoop;
            if (GUILayout.Toggle(active, "Active", GUILayout.Width(56)) && !active)
                _activeLoop = i;
            EditorGUI.BeginChangeCheck();
            loop.displayName = EditorGUILayout.TextField(loop.displayName);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_asset);
                SyncLoopNameToBounds(loop);
            }
            int bespoke = loop.vertexIndices != null ? loop.vertexIndices.Count : 0;
            EditorGUILayout.LabelField(bespoke + " bespoke", GUILayout.Width(72));
            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                Undo.RecordObject(_asset, "Remove loop");
                _asset.loops.RemoveAt(i);
                if (_activeLoop >= _asset.loops.Count)
                    _activeLoop = _asset.loops.Count - 1;
                EditorUtility.SetDirty(_asset);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
            if (i == _activeLoop)
            {
                EditorGUI.indentLevel++;
                loop.seedTriangle = EditorGUILayout.IntField("Seed Triangle", loop.seedTriangle);
                loop.boneName = EditorGUILayout.TextField("Bone Name", loop.boneName ?? "");
                loop.blendShapeNote = EditorGUILayout.TextField("Blend Shape", loop.blendShapeNote ?? "");
                EditorGUILayout.LabelField(
                    "Assigned tris: " + (loop.assignedTriangles != null ? loop.assignedTriangles.Count : 0));

                EditorGUI.BeginChangeCheck();
                var bounds = (SkinnedMeshLoopSplitBounds)EditorGUILayout.ObjectField(
                    "Split Bounds",
                    ResolveLoopBounds(loop),
                    typeof(SkinnedMeshLoopSplitBounds),
                    true);
                if (EditorGUI.EndChangeCheck())
                    BindLoopBounds(loop, bounds);

                using (new EditorGUI.DisabledScope(_renderer == null))
                {
                    if (GUILayout.Button("Create Split Bounds"))
                        CreateOrSelectSplitBounds(loop);
                }

                DrawBespokeVertices(loop);
                EditorGUI.indentLevel--;
            }
        }
    }

    void DrawMaterials()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Materials / Auto Break Out", EditorStyles.boldLabel);
        Mesh mesh = WorkingMesh();
        int subCount = mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 0;
        int matCount = 0;
        if (_renderer != null && _renderer.sharedMaterials != null)
            matCount = _renderer.sharedMaterials.Length;
        int n = Mathf.Max(subCount, matCount);
        if (n == 0)
        {
            EditorGUILayout.HelpBox("No materials or submeshes on the renderer.", MessageType.Info);
            return;
        }
        if (mesh != null && mesh.subMeshCount <= 1 && matCount > 1)
            EditorGUILayout.HelpBox(
                "Mesh has one submesh; extra materials share the same triangles. Break-out still extracts that island; add submeshes for per-material seams.",
                MessageType.Warning);

        if (_asset.breakoutMaterialIndices == null)
            _asset.breakoutMaterialIndices = new List<int>();

        for (int i = 0; i < n; i++)
        {
            bool on = _asset.breakoutMaterialIndices.Contains(i);
            string label = i + "  " + SkinnedMeshLoopMaterialBreakout.MaterialName(_renderer, i);
            if (mesh != null)
            {
                int tris = i < mesh.subMeshCount ? mesh.GetTriangles(i).Length / 3 : 0;
                int verts = SkinnedMeshLoopMaterialBreakout.VerticesOfSubmesh(mesh, i).Count;
                label += "  tris=" + tris + " verts=" + verts;
            }
            bool next = EditorGUILayout.ToggleLeft(label, on);
            if (next == on)
                continue;
            Undo.RecordObject(_asset, "Toggle breakout material");
            if (next)
                _asset.breakoutMaterialIndices.Add(i);
            else
                _asset.breakoutMaterialIndices.Remove(i);
            EditorUtility.SetDirty(_asset);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All Materials"))
        {
            Undo.RecordObject(_asset, "Select all materials");
            _asset.breakoutMaterialIndices.Clear();
            for (int i = 0; i < n; i++)
                _asset.breakoutMaterialIndices.Add(i);
            EditorUtility.SetDirty(_asset);
        }
        if (GUILayout.Button("Clear Material Selection"))
        {
            Undo.RecordObject(_asset, "Clear material selection");
            _asset.breakoutMaterialIndices.Clear();
            EditorUtility.SetDirty(_asset);
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(_asset.breakoutMaterialIndices.Count == 0 || mesh == null))
        {
            if (GUILayout.Button("Auto Break Out Selected Materials"))
            {
                Undo.RecordObject(_asset, "Auto break out materials");
                int written = SkinnedMeshLoopMaterialBreakout.ApplyToAsset(
                    mesh, _renderer, _asset, _asset.breakoutMaterialIndices);
                EditorUtility.SetDirty(_asset);
                _splitPreview = written + " material loop(s). NamedAssign from submesh triangles + boundary edges.\n"
                    + BuildSplitPreview();
            }
        }
    }

    void DrawPreview()
    {
        BakeIfNeeded();
        Rect rect = GUILayoutUtility.GetRect(256, 192);
        if (_preview == null || _baked == null || Event.current.type != EventType.Repaint)
            return;
        _preview.BeginPreview(rect, GUIStyle.none);
        var b = _baked.bounds;
        float dist = Mathf.Max(0.35f, b.extents.magnitude * 2.6f);
        _preview.camera.transform.position = b.center + Quaternion.Euler(_previewDrag.y, _previewDrag.x, 0f) * (Vector3.forward * -dist);
        _preview.camera.transform.LookAt(b.center);
        if (_previewMat == null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
                _previewMat = new Material(shader);
        }
        if (_previewMat != null)
            _preview.DrawMesh(_baked, Matrix4x4.identity, _previewMat, 0);
        _preview.camera.Render();
        GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.ScaleToFit, false);
    }

    void DrawActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Scene: click a highlighted vertex to append to the active loop. Shift+wheel cycles the zone, then click to commit. Close Loop snaps picks to mesh edges. Create Split Bounds parents a movable box to the mesh; overlapping verts blink as wireframe nodes.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Close Loop"))
            CloseActiveLoop();
        if (GUILayout.Button("Save Asset"))
        {
            if (_asset != null)
            {
                EditorUtility.SetDirty(_asset);
                if (_section != null)
                    EditorUtility.SetDirty(_section);
                AssetDatabase.SaveAssets();
            }
        }
        if (GUILayout.Button("Clear Active Loop Verts"))
        {
            var loop = ActiveLoop();
            if (loop != null)
            {
                Undo.RecordObject(_asset, "Clear loop verts");
                loop.vertexIndices.Clear();
                EditorUtility.SetDirty(_asset);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Capture Originals From Renderer"))
        {
            if (_renderer != null)
            {
                Undo.RecordObject(_asset, "Capture originals");
                _asset.CaptureOriginals(
                    SkinnedMeshLoopRendererUtil.SharedMesh(_renderer),
                    SkinnedMeshLoopHasher.CollectTextures(_renderer));
                if (_section != null)
                {
                    _section.meshUpdated = false;
                    _section.useCached = false;
                    EditorUtility.SetDirty(_section);
                }
                EditorUtility.SetDirty(_asset);
            }
        }

        using (new EditorGUI.DisabledScope(_section == null || !_section.meshUpdated || !_section.useCached))
        {
            if (GUILayout.Button("Overwrite & Update Saved Cache"))
            {
                Undo.RecordObject(_section, "Overwrite loop cache");
                Undo.RecordObject(_asset, "Overwrite loop cache");
                _section.OverwriteAndUpdateSavedCache();
                EditorUtility.SetDirty(_section);
                EditorUtility.SetDirty(_asset);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Split / Prefab", EditorStyles.boldLabel);
        if (GUILayout.Button("Split Preview"))
            _splitPreview = BuildSplitPreview();
        if (!string.IsNullOrEmpty(_splitPreview))
            EditorGUILayout.HelpBox(_splitPreview, MessageType.None);

        if (GUILayout.Button("Save Prefab…"))
            SavePrefab();
    }

    string BuildSplitPreview()
    {
        if (_section != null && !_section.CanApplyLoop)
            return "meshUpdated without useCached — cannot split.";
        Mesh mesh = WorkingMesh();
        if (mesh == null || _asset == null)
            return "No mesh.";
        SyncAllLoopBounds();
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, _asset, _renderer != null ? _renderer.transform : null);
        if (pieces == null || pieces.Count == 0)
            return "No pieces.";
        var sb = new System.Text.StringBuilder();
        sb.Append(pieces.Count).Append(" piece(s):\n");
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            int tris = p.sourceTriangleIndices != null ? p.sourceTriangleIndices.Length : 0;
            int verts = p.mesh != null ? p.mesh.vertexCount : 0;
            sb.Append(p.name).Append("  tris=").Append(tris).Append(" verts=").Append(verts).Append('\n');
            if (p.mesh != null && !AssetDatabase.Contains(p.mesh))
                DestroyImmediate(p.mesh);
        }
        return sb.ToString();
    }

    void SavePrefab()
    {
        if (_renderer == null || _asset == null)
        {
            EditorUtility.DisplayDialog("Skinned Loop Section", "Assign a renderer and section asset.", "OK");
            return;
        }
        if (_section != null && !_section.CanApplyLoop)
        {
            EditorUtility.DisplayDialog("Skinned Loop Section", "meshUpdated without useCached — cannot split.", "OK");
            return;
        }
        Mesh mesh = WorkingMesh();
        SyncAllLoopBounds();
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, _asset, _renderer.transform);
        if (pieces == null || pieces.Count == 0)
        {
            EditorUtility.DisplayDialog("Skinned Loop Section", "Split produced no pieces.", "OK");
            return;
        }
        string suggested = (_renderer.transform.root != null ? _renderer.transform.root.name : _renderer.name) + "_LoopSplit";
        SkinnedMeshLoopSplitBuilder.EnsureFolder(SkinnedMeshLoopSplitBuilder.DefaultFolder);
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Loop Split Prefab",
            suggested,
            "prefab",
            "Prefab with split mesh pieces");
        if (string.IsNullOrEmpty(path))
            return;
        string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder) || folder == "Assets")
            folder = SkinnedMeshLoopSplitBuilder.DefaultFolder + "/" + suggested;
        SkinnedMeshLoopSplitBuilder.EnsureFolder(folder);
        string saved = SkinnedMeshLoopSplitBuilder.SavePrefab(_renderer, _asset, pieces, path, folder);
        EditorUtility.DisplayDialog("Skinned Loop Section", "Saved " + saved, "OK");
    }

    void CloseActiveLoop()
    {
        var loop = ActiveLoop();
        Mesh mesh = WorkingMesh();
        if (loop == null || mesh == null)
            return;
        Undo.RecordObject(_asset, "Close loop");
        var box = ResolveLoopBounds(loop);
        loop.splitBounds = box;
        Vector3[] verts = mesh.vertices;
        Matrix4x4 l2w = _renderer != null ? _renderer.transform.localToWorldMatrix : Matrix4x4.identity;
        var combined = loop.CombinedVertexIndices(verts, l2w);
        var closed = SkinnedMeshLoopEdgePath.CloseLoop(mesh, combined);
        if (closed == null)
            closed = combined;
        if (box == null)
            loop.vertexIndices = closed;
        else
        {
            var overlap = new HashSet<int>();
            box.CollectOverlapping(verts, l2w, _boundsOverlapVerts);
            for (int i = 0; i < _boundsOverlapVerts.Count; i++)
                overlap.Add(_boundsOverlapVerts[i]);
            var bespoke = new List<int>();
            for (int i = 0; i < closed.Count; i++)
            {
                int v = closed[i];
                if (!overlap.Contains(v) && !bespoke.Contains(v))
                    bespoke.Add(v);
            }
            loop.vertexIndices = bespoke;
        }
        EditorUtility.SetDirty(_asset);
    }

    SkinnedMeshLoopSectionAsset.LoopSection ActiveLoop()
    {
        if (_asset == null || _asset.loops == null || _asset.loops.Count == 0)
            return null;
        if (_activeLoop < 0 || _activeLoop >= _asset.loops.Count)
            _activeLoop = 0;
        return _asset.loops[_activeLoop];
    }

    Mesh WorkingMesh()
    {
        if (_section != null && _section.TryGetWorkingMesh(out var mesh))
            return mesh;
        return _renderer != null
            ? SkinnedMeshLoopRendererUtil.SharedMesh(_renderer)
            : _asset != null ? _asset.originalMesh : null;
    }

    public bool IsEditingBounds(SkinnedMeshLoopSplitBounds box)
    {
        if (box == null || _asset == null || string.IsNullOrEmpty(box.loopId))
            return false;
        if (box.sectionAsset != null && box.sectionAsset != _asset)
            return false;
        if (_asset.GetLoop(box.loopId) == null)
            return false;
        if (_renderer == null)
            return false;
        return box.transform == _renderer.transform || box.transform.IsChildOf(_renderer.transform);
    }

    public bool UpdateLoopTrianglesFromBounds(SkinnedMeshLoopSplitBounds box, out string error)
    {
        error = null;
        if (!IsEditingBounds(box))
        {
            error = "This bounds is not the one in the open loop editor.";
            return false;
        }
        var loop = _asset.GetLoop(box.loopId);
        if (loop == null)
        {
            error = "Loop not found on the section asset.";
            return false;
        }
        BakeIfNeeded();
        Mesh topology = WorkingMesh();
        if (topology == null)
        {
            error = "No mesh on the open loop editor.";
            return false;
        }
        Vector3[] verts = _baked != null && _baked.vertexCount == topology.vertexCount
            ? _baked.vertices
            : topology.vertices;
        int[] tris = SkinnedMeshLoopEdgePath.AllTriangles(topology);
        Undo.RecordObject(_asset, "Update loop triangles from bounds");
        int count = box.ApplyOverlappingTriangles(
            loop, verts, tris, _renderer.transform.localToWorldMatrix);
        for (int i = 0; i < _asset.loops.Count; i++)
        {
            if (_asset.loops[i] != null && _asset.loops[i].id == box.loopId)
            {
                _activeLoop = i;
                break;
            }
        }
        EditorUtility.SetDirty(_asset);
        Repaint();
        SceneView.RepaintAll();
        error = count + " triangle(s)";
        return true;
    }

    void BakeIfNeeded()
    {
        if (_baked == null)
            _baked = new Mesh { name = "LoopBake" };
        if (_section != null && _section.useCached &&
            _section.sectionAsset != null && _section.sectionAsset.savedCacheMesh != null)
        {
            CopyMesh(_section.sectionAsset.savedCacheMesh, _baked);
            return;
        }
        SkinnedMeshLoopRendererUtil.TryBake(_renderer, _baked);
    }

    static void CopyMesh(Mesh src, Mesh dst)
    {
        dst.Clear();
        dst.vertices = src.vertices;
        dst.normals = src.normals;
        dst.uv = src.uv;
        dst.colors = src.colors;
        dst.triangles = src.triangles;
        dst.RecalculateBounds();
    }

    void OnSceneGui(SceneView view)
    {
        if (_renderer == null || _asset == null)
            return;
        if (!(_renderer is SkinnedMeshRenderer) && !(_renderer is MeshRenderer))
            return;
        BakeIfNeeded();
        if (_baked == null)
            return;

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        double now = EditorApplication.timeSinceStartup;
        bool freeze = _debounce.ShouldFreezeHover(mouse, now);
        bool editingBounds = IsSplitBoundsSelection();
        bool allowPick = AllowsVertexPicking(e, editingBounds);

        if (allowPick && e.type == EventType.ScrollWheel && e.shift)
        {
            if (_zoneVerts.Count > 0)
            {
                _cycleMode = true;
                _debounce.Begin(mouse, now);
                int delta = e.delta.y > 0 ? 1 : -1;
                _cycleIndex = (_cycleIndex + delta + _zoneVerts.Count * 8) % _zoneVerts.Count;
                _hoverVertex = _zoneVerts[_cycleIndex];
                e.Use();
                view.Repaint();
            }
        }

        if (allowPick && !freeze && e.type != EventType.Used)
            UpdateHover(HandleUtility.GUIPointToWorldRay(mouse));
        else if (!allowPick && !_cycleMode)
        {
            _hoverVertex = -1;
            _hoverTriangle = -1;
            _zoneVerts.Clear();
        }

        if (allowPick && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            bool picked = false;
            if (_asset.splitMode == SkinnedMeshLoopSplitMode.NamedAssign && _hoverTriangle >= 0)
            {
                AssignTriangle(_hoverTriangle);
                picked = true;
            }
            else if (_hoverVertex >= 0)
            {
                CommitVertex(_hoverVertex);
                picked = true;
            }
            if (picked)
                e.Use();
        }

        if (e.type == EventType.Repaint)
        {
            if (!editingBounds)
                DrawZoneGizmos();
            DrawAuthoredLoops();
            DrawSplitBoundsOverlap();
        }
        if (_cycleMode || HasSplitBounds())
            view.Repaint();
    }

    static bool IsSplitBoundsSelection()
    {
        var go = Selection.activeGameObject;
        return go != null && go.GetComponentInParent<SkinnedMeshLoopSplitBounds>() != null;
    }

    static bool AllowsVertexPicking(Event e, bool editingBounds)
    {
        if (editingBounds)
            return false;
        if (e == null || e.alt || Tools.viewToolActive)
            return false;
        if (GUIUtility.hotControl != 0)
            return false;
        if (HandleUtility.nearestControl != 0)
            return false;
        return true;
    }

    void UpdateHover(Ray ray)
    {
        if (!RaycastBaked(ray, out int tri, out Vector3 hit))
        {
            if (!_cycleMode)
            {
                _hoverVertex = -1;
                _hoverTriangle = -1;
                _zoneVerts.Clear();
            }
            return;
        }
        _hoverTriangle = tri;
        int nearest = NearestVertex(hit);
        RebuildZone(nearest);
        if (!_cycleMode)
            _hoverVertex = nearest;
        else if (_zoneVerts.Count > 0)
            _hoverVertex = _zoneVerts[_cycleIndex % _zoneVerts.Count];
    }

    void RebuildZone(int center)
    {
        Mesh src = _baked;
        if (src == null || center < 0)
        {
            _zoneVerts.Clear();
            return;
        }
        SkinnedMeshLoopZoneHighlight.CollectZone(
            center,
            src.vertices,
            _asset != null ? _asset.zoneRadius : 0.05f,
            SkinnedMeshLoopEdgePath.BuildAdjacency(src),
            _zoneVerts);
        _cycleIndex = Mathf.Clamp(_cycleIndex, 0, Mathf.Max(0, _zoneVerts.Count - 1));
    }

    void CommitVertex(int vertex)
    {
        var loop = ActiveLoop();
        if (loop == null)
        {
            Undo.RecordObject(_asset, "Add loop");
            loop = _asset.AddLoop();
            _activeLoop = _asset.loops.Count - 1;
        }
        Undo.RecordObject(_asset, "Pick loop vertex");
        if (loop.vertexIndices == null)
            loop.vertexIndices = new List<int>();
        if (!loop.vertexIndices.Contains(vertex))
            loop.vertexIndices.Add(vertex);
        _asset.lastPickedIndex = vertex;
        _cycleMode = false;
        EditorUtility.SetDirty(_asset);
    }

    void AssignTriangle(int tri)
    {
        var loop = ActiveLoop();
        if (loop == null)
            return;
        Undo.RecordObject(_asset, "Assign triangle");
        if (loop.assignedTriangles == null)
            loop.assignedTriangles = new List<int>();
        if (!loop.assignedTriangles.Contains(tri))
            loop.assignedTriangles.Add(tri);
        loop.seedTriangle = tri;
        EditorUtility.SetDirty(_asset);
    }

    void CreateOrSelectSplitBounds(SkinnedMeshLoopSectionAsset.LoopSection loop)
    {
        if (_renderer == null || loop == null)
            return;
        if (string.IsNullOrEmpty(loop.id))
            loop.id = System.Guid.NewGuid().ToString("N");
        Mesh mesh = WorkingMesh();
        GameObject prefab = ResolveMeshPrefab();
        var existing = SkinnedMeshLoopSplitBounds.FindForLoop(_renderer.transform, loop.id);
        if (existing != null)
        {
            BindLoopBounds(loop, existing);
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        var created = SkinnedMeshLoopSplitBounds.CreateUnderMesh(
            _renderer.transform, mesh, loop.id, loop.displayName, _asset, prefab);
        if (created == null)
            return;
        Undo.RegisterCreatedObjectUndo(created.gameObject, "Create split bounds");
        BindLoopBounds(loop, created);
        Undo.SetCurrentGroupName("Create split bounds");
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = created.gameObject;
        EditorGUIUtility.PingObject(created.gameObject);
    }

    void DrawBespokeVertices(SkinnedMeshLoopSectionAsset.LoopSection loop)
    {
        if (loop == null)
            return;
        int n = loop.vertexIndices != null ? loop.vertexIndices.Count : 0;
        loop.bespokeVertsExpanded = EditorGUILayout.Foldout(
            loop.bespokeVertsExpanded, "Bespoke selected vertices (" + n + ")", true);
        if (!loop.bespokeVertsExpanded)
            return;
        if (n == 0)
        {
            EditorGUILayout.HelpBox(
                "Click vertices in the Scene view to add extras beyond the split bounds.",
                MessageType.None);
            return;
        }
        for (int v = 0; v < loop.vertexIndices.Count; v++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vertex " + loop.vertexIndices[v]);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(_asset, "Remove bespoke vertex");
                loop.RemoveBespokeVertexAt(v);
                EditorUtility.SetDirty(_asset);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    SkinnedMeshLoopSplitBounds ResolveLoopBounds(SkinnedMeshLoopSectionAsset.LoopSection loop)
    {
        if (loop == null)
            return null;
        EnsureSectionRef();
        if (loop.splitBounds != null)
            return loop.splitBounds;
        if (_section != null)
        {
            var bound = _section.GetSplitBounds(loop.id);
            if (bound != null)
                return bound;
        }
        if (_renderer != null && !string.IsNullOrEmpty(loop.id))
            return SkinnedMeshLoopSplitBounds.FindForLoop(_renderer.transform, loop.id);
        return null;
    }

    void BindLoopBounds(SkinnedMeshLoopSectionAsset.LoopSection loop, SkinnedMeshLoopSplitBounds bounds)
    {
        if (loop == null)
            return;
        if (string.IsNullOrEmpty(loop.id))
            loop.id = System.Guid.NewGuid().ToString("N");
        EnsureSectionRef();
        if (_asset != null)
        {
            Undo.RecordObject(_asset, "Bind split bounds");
            loop.splitBounds = bounds;
            EditorUtility.SetDirty(_asset);
        }
        else
            loop.splitBounds = bounds;
        if (_section != null)
        {
            Undo.RecordObject(_section, "Bind split bounds");
            _section.SetSplitBounds(loop.id, bounds);
            EditorUtility.SetDirty(_section);
        }
        if (bounds == null)
            return;
        Undo.RecordObject(bounds, "Associate split bounds");
        bounds.Associate(loop.id, loop.displayName, _asset, ResolveMeshPrefab());
        EditorUtility.SetDirty(bounds);
    }

    void EnsureSectionRef()
    {
        if (_section != null || _renderer == null)
            return;
        _section = _renderer.GetComponent<SkinnedMeshLoopSection>()
            ?? _renderer.GetComponentInParent<SkinnedMeshLoopSection>();
    }

    void SyncAllLoopBounds()
    {
        if (_asset == null || _asset.loops == null)
            return;
        for (int i = 0; i < _asset.loops.Count; i++)
        {
            var loop = _asset.loops[i];
            if (loop == null)
                continue;
            loop.splitBounds = ResolveLoopBounds(loop);
        }
    }

    void SyncLoopNameToBounds(SkinnedMeshLoopSectionAsset.LoopSection loop)
    {
        var box = ResolveLoopBounds(loop);
        if (box == null)
            return;
        Undo.RecordObject(box, "Rename split bounds");
        box.Associate(loop.id, loop.displayName, _asset, ResolveMeshPrefab());
        EditorUtility.SetDirty(box);
    }

    GameObject ResolveMeshPrefab()
    {
        if (_renderer == null)
            return null;
        var go = _renderer.gameObject;
        var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        if (original != null)
            return original;
        var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        if (source != null)
            return source;
        return go;
    }

    bool HasSplitBounds()
    {
        return _renderer != null &&
               _renderer.GetComponentInChildren<SkinnedMeshLoopSplitBounds>(true) != null;
    }

    void DrawSplitBoundsOverlap()
    {
        if (_baked == null || _renderer == null)
            return;
        var boxes = _renderer.GetComponentsInChildren<SkinnedMeshLoopSplitBounds>(true);
        if (boxes == null || boxes.Length == 0)
            return;
        Vector3[] verts = _baked.vertices;
        Matrix4x4 l2w = _renderer.transform.localToWorldMatrix;
        Color albedo = SampleZoneAlbedo();
        Color contrast = SkinnedMeshLoopZoneHighlight.ContrastComplement(albedo);
        Color blink = SkinnedMeshLoopZoneHighlight.Blink(
            albedo, contrast, EditorApplication.timeSinceStartup);
        var active = ActiveLoop();
        for (int b = 0; b < boxes.Length; b++)
        {
            var box = boxes[b];
            if (box == null)
                continue;
            bool isActive = active != null && box.loopId == active.id;
            Handles.matrix = box.transform.localToWorldMatrix;
            Handles.color = isActive
                ? new Color(0.25f, 1f, 0.55f, 0.95f)
                : new Color(0.25f, 0.8f, 0.5f, 0.45f);
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
            Handles.matrix = Matrix4x4.identity;
            box.CollectOverlapping(verts, l2w, _boundsOverlapVerts);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = blink;
            for (int i = 0; i < _boundsOverlapVerts.Count; i++)
            {
                int vi = _boundsOverlapVerts[i];
                if (vi < 0 || vi >= verts.Length)
                    continue;
                Vector3 w = l2w.MultiplyPoint3x4(verts[vi]);
                DrawWireVertexNode(w);
            }
        }
    }

    void DrawZoneGizmos()
    {
        if (_baked == null || _zoneVerts.Count == 0)
            return;
        Vector3[] verts = _baked.vertices;
        Matrix4x4 l2w = _renderer.transform.localToWorldMatrix;
        Color albedo = SampleZoneAlbedo();
        Color contrast = SkinnedMeshLoopZoneHighlight.ContrastComplement(albedo);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = contrast;
        int[] tris = _baked.triangles;
        var used = new HashSet<int>(_zoneVerts);
        for (int i = 0; i + 2 < tris.Length; i += 3)
        {
            if (!used.Contains(tris[i]) && !used.Contains(tris[i + 1]) && !used.Contains(tris[i + 2]))
                continue;
            Vector3 a = l2w.MultiplyPoint3x4(verts[tris[i]]);
            Vector3 b = l2w.MultiplyPoint3x4(verts[tris[i + 1]]);
            Vector3 c = l2w.MultiplyPoint3x4(verts[tris[i + 2]]);
            Handles.DrawAAPolyLine(2f, a, b, c, a);
        }

        if (_hoverVertex >= 0 && _hoverVertex < verts.Length)
        {
            Color blink = _cycleMode
                ? SkinnedMeshLoopZoneHighlight.Blink(albedo, contrast, EditorApplication.timeSinceStartup)
                : contrast;
            Handles.color = blink;
            DrawWireVertexNode(l2w.MultiplyPoint3x4(verts[_hoverVertex]));
        }
    }

    static void DrawWireVertexNode(Vector3 world)
    {
        float s = HandleUtility.GetHandleSize(world) * 0.05f;
        Handles.DrawAAPolyLine(1.5f, world + Vector3.right * s, world - Vector3.right * s);
        Handles.DrawAAPolyLine(1.5f, world + Vector3.up * s, world - Vector3.up * s);
        Handles.DrawAAPolyLine(1.5f, world + Vector3.forward * s, world - Vector3.forward * s);
    }

    void DrawAuthoredLoops()
    {
        if (_baked == null || _asset.loops == null)
            return;
        Vector3[] verts = _baked.vertices;
        Matrix4x4 l2w = _renderer.transform.localToWorldMatrix;
        for (int li = 0; li < _asset.loops.Count; li++)
        {
            var loop = _asset.loops[li];
            if (loop == null || loop.vertexIndices == null || loop.vertexIndices.Count == 0)
                continue;
            Handles.color = li == _activeLoop ? Color.cyan : new Color(0.4f, 0.8f, 1f, 0.6f);
            var pts = new List<Vector3>();
            for (int i = 0; i < loop.vertexIndices.Count; i++)
            {
                int vi = loop.vertexIndices[i];
                if (vi >= 0 && vi < verts.Length)
                    pts.Add(l2w.MultiplyPoint3x4(verts[vi]));
            }
            int nodeCount = pts.Count;
            if (pts.Count >= 2)
            {
                pts.Add(pts[0]);
                Handles.DrawAAPolyLine(3f, pts.ToArray());
            }
            for (int i = 0; i < nodeCount; i++)
                DrawWireVertexNode(pts[i]);
        }
    }

    Color SampleZoneAlbedo()
    {
        Color matColor = Color.gray;
        Texture2D mainTex = null;
        if (_renderer != null && _renderer.sharedMaterial != null)
        {
            matColor = _renderer.sharedMaterial.color;
            mainTex = _renderer.sharedMaterial.mainTexture as Texture2D;
        }
        if (_baked == null)
            return matColor;
        return SkinnedMeshLoopZoneHighlight.ZoneAverageAlbedo(
            _zoneVerts,
            _baked.colors,
            _baked.uv,
            mainTex,
            matColor);
    }

    bool RaycastBaked(Ray worldRay, out int tri, out Vector3 worldHit)
    {
        tri = -1;
        worldHit = Vector3.zero;
        Vector3[] verts = _baked.vertices;
        int[] tris = _baked.triangles;
        Matrix4x4 l2w = _renderer.transform.localToWorldMatrix;
        float best = float.MaxValue;
        for (int t = 0; t + 2 < tris.Length; t += 3)
        {
            Vector3 a = l2w.MultiplyPoint3x4(verts[tris[t]]);
            Vector3 b = l2w.MultiplyPoint3x4(verts[tris[t + 1]]);
            Vector3 c = l2w.MultiplyPoint3x4(verts[tris[t + 2]]);
            if (!Intersect(worldRay, a, b, c, out float dist))
                continue;
            if (dist < best)
            {
                best = dist;
                tri = t / 3;
                worldHit = worldRay.GetPoint(dist);
            }
        }
        return tri >= 0;
    }

    int NearestVertex(Vector3 worldHit)
    {
        Vector3[] verts = _baked.vertices;
        Matrix4x4 l2w = _renderer.transform.localToWorldMatrix;
        float best = float.MaxValue;
        int idx = -1;
        for (int i = 0; i < verts.Length; i++)
        {
            float d = (l2w.MultiplyPoint3x4(verts[i]) - worldHit).sqrMagnitude;
            if (d < best)
            {
                best = d;
                idx = i;
            }
        }
        return idx;
    }

    static bool Intersect(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float t)
    {
        t = 0f;
        Vector3 e1 = b - a;
        Vector3 e2 = c - a;
        Vector3 p = Vector3.Cross(ray.direction, e2);
        float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-8f)
            return false;
        float inv = 1f / det;
        Vector3 tv = ray.origin - a;
        float u = Vector3.Dot(tv, p) * inv;
        if (u < 0f || u > 1f)
            return false;
        Vector3 q = Vector3.Cross(tv, e1);
        float v = Vector3.Dot(ray.direction, q) * inv;
        if (v < 0f || u + v > 1f)
            return false;
        t = Vector3.Dot(e2, q) * inv;
        return t >= 0f;
    }
}
