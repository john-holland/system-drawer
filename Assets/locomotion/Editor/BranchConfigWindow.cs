#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class BranchConfigWindow : EditorWindow
{
    TreeGenConfig _config;
    Transform _plant;
    Consider _consider;
    int _branch;
    Vector2 _scroll;
    PreviewRenderUtility _preview;
    Mesh _previewMesh;
    Material _previewMat;

    [MenuItem("Locomotion/Branch Configuration")]
    public static void Open()
    {
        var w = GetWindow<BranchConfigWindow>("Branches");
        w.minSize = new Vector2(460, 560);
    }

    public static void Open(TreeGenConfig config)
    {
        var w = GetWindow<BranchConfigWindow>("Branches");
        w.minSize = new Vector2(460, 560);
        w._config = config;
        w.Show();
    }

    [MenuItem("Locomotion/Create Branch Configuration")]
    public static TreeGenConfig CreateAndOpen()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save tree gen config", "TreeGenConfig", "asset", "Creates a treegen config and opens the branch editor.");
        if (string.IsNullOrEmpty(path)) return null;
        var config = CreateInstance<TreeGenConfig>();
        config.branches.Add(TreeGenConfig.DefaultLeader());
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        Open(config);
        return config;
    }

    void OnDisable()
    {
        if (_preview != null)
        {
            _preview.Cleanup();
            _preview = null;
        }
        if (_previewMesh != null)
        {
            DestroyImmediate(_previewMesh);
            _previewMesh = null;
        }
        if (_previewMat != null)
        {
            DestroyImmediate(_previewMat);
            _previewMat = null;
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _config = (TreeGenConfig)EditorGUILayout.ObjectField("Tree gen", _config, typeof(TreeGenConfig), false);
        if (GUILayout.Button("Create and open"))
        {
            var created = CreateAndOpen();
            if (created != null)
                _config = created;
        }
        if (_config == null)
        {
            EditorGUILayout.HelpBox("Assign a TreeGenConfig, or create one.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();
        var organism = (PlantOrganismDef)EditorGUILayout.ObjectField(
            "Organism", _config.organism, typeof(PlantOrganismDef), false);
        if (organism != _config.organism)
        {
            _config.organism = organism;
            if (organism != null && (_config.branches == null || _config.branches.Count == 0))
                _config.PullFromOrganism();
        }
        _config.meshStages = (LotGrassPlantDef)EditorGUILayout.ObjectField(
            "Mesh stages", _config.meshStages, typeof(LotGrassPlantDef), false);
        _config.seed = EditorGUILayout.IntField("Seed", _config.seed);
        _plant = (Transform)EditorGUILayout.ObjectField("Plant", _plant, typeof(Transform), true);
        _consider = (Consider)EditorGUILayout.ObjectField("Consider", _consider, typeof(Consider), true);

        if (_config.branches == null)
            _config.branches = new List<PlantBranchDef>();
        if (_config.branches.Count == 0)
            _config.branches.Add(TreeGenConfig.DefaultLeader());
        _branch = Mathf.Clamp(_branch, 0, _config.branches.Count - 1);
        EditorGUILayout.BeginHorizontal();
        _branch = EditorGUILayout.IntSlider("Branch", _branch, 0, _config.branches.Count - 1);
        if (GUILayout.Button("Add", GUILayout.Width(48f)))
        {
            _config.branches.Add(TreeGenConfig.DefaultLeader());
            _branch = _config.branches.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        DrawBranch(_config.branches[_branch]);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_config);

        if (GUILayout.Button("Write branches to organism"))
        {
            _config.WriteToOrganism();
            if (_config.organism != null)
                EditorUtility.SetDirty(_config.organism);
            EditorUtility.SetDirty(_config);
        }
        if (GUILayout.Button("Bake ribbon"))
            BakeRibbon();
        if (GUILayout.Button("Show example"))
            ShowExample();

        Rect preview = GUILayoutUtility.GetRect(320f, 220f);
        DrawPreview(preview);
        EditorGUILayout.EndScrollView();
    }

    static void DrawBranch(PlantBranchDef branch)
    {
        if (branch == null) return;
        branch.branchTypeLabel = EditorGUILayout.TextField("Type", branch.branchTypeLabel);
        branch.lengthMin = EditorGUILayout.FloatField("Length min", branch.lengthMin);
        branch.lengthMax = EditorGUILayout.FloatField("Length max", branch.lengthMax);
        branch.grabberT01 = EditorGUILayout.Slider("Grabber", branch.grabberT01, 0f, 1f);
        branch.grabberRotationDeg = EditorGUILayout.FloatField("Grabber rotation", branch.grabberRotationDeg);
        branch.harvest = (HarvestIkKind)EditorGUILayout.EnumPopup("Harvest", branch.harvest);
        if (branch.harvestSave == null)
            branch.harvestSave = new HarvestInventorySave();
        branch.harvestSave.itemId = EditorGUILayout.TextField("Item id", branch.harvestSave.itemId);
        branch.harvestSave.itemName = EditorGUILayout.TextField("Item name", branch.harvestSave.itemName);
        branch.harvestSave.partName = EditorGUILayout.TextField("Part", branch.harvestSave.partName);
        branch.harvestSave.countMin = EditorGUILayout.IntField("Count min", branch.harvestSave.countMin);
        branch.harvestSave.countMax = EditorGUILayout.IntField("Count max", branch.harvestSave.countMax);
        EditorGUI.BeginChangeCheck();
        Vector3 euler = EditorGUILayout.Vector3Field("Origin rotation", branch.originRotation.eulerAngles);
        if (EditorGUI.EndChangeCheck())
            branch.originRotation = Quaternion.Euler(euler);

        if (branch.curvePoints == null)
            branch.curvePoints = new List<Vector3>();
        int remove = -1;
        for (int i = 0; i < branch.curvePoints.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            branch.curvePoints[i] = EditorGUILayout.Vector3Field("P" + i, branch.curvePoints[i]);
            if (GUILayout.Button("X", GUILayout.Width(22f)))
                remove = i;
            EditorGUILayout.EndHorizontal();
        }
        if (remove >= 0)
            branch.curvePoints.RemoveAt(remove);
        if (GUILayout.Button("Add curve point"))
            branch.curvePoints.Add(branch.curvePoints.Count == 0 ? Vector3.zero : branch.curvePoints[branch.curvePoints.Count - 1] + Vector3.forward);
    }

    void BakeRibbon()
    {
        _config.BakeAll();
        _config.WriteToOrganism();
        if (_config.organism != null)
            EditorUtility.SetDirty(_config.organism);
        if (_plant != null && _config.bakes != null)
        {
            var crown = _plant.Find("BranchCrown");
            if (crown == null)
            {
                var crownGo = new GameObject("BranchCrown");
                crownGo.transform.SetParent(_plant, false);
                crownGo.transform.localPosition = Vector3.up * 1.2f;
                crown = crownGo.transform;
            }
            for (int i = 0; i < _config.bakes.Count; i++)
            {
                string name = "BranchRibbon_" + i;
                var child = crown.Find(name);
                if (child == null)
                {
                    var go = new GameObject(name);
                    go.transform.SetParent(crown, false);
                    child = go.transform;
                }
                var ribbon = child.GetComponent<PlanarSplinePathLocomotion>()
                             ?? child.gameObject.AddComponent<PlanarSplinePathLocomotion>();
                _config.bakes[i].ApplyToRibbon(ribbon);
                EditorUtility.SetDirty(ribbon);
            }
        }
        if (_consider != null && _config.bakes != null && _config.branches != null)
        {
            if (_consider.grabPrebakes == null)
                _consider.grabPrebakes = new System.Collections.Generic.List<ConsiderGrabPrebake>();
            _consider.grabPrebakes.Clear();
            int n = Mathf.Min(_config.bakes.Count, _config.branches.Count);
            for (int i = 0; i < n; i++)
            {
                var branch = _config.branches[i];
                var clip = HarvestIkAnimation.CreateDefault(branch.harvest, branch.harvestSave);
                _consider.PrebakeGrab(_config.bakes[i], branch, null, clip);
            }
            EditorUtility.SetDirty(_consider);
        }
        EditorUtility.SetDirty(_config);
    }

    void ShowExample()
    {
        var go = new GameObject("TreeGenExample");
        var example = go.AddComponent<TreeGenExamplePlant>();
        example.config = _config;
        example.Refresh();
        Selection.activeGameObject = go;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    void DrawPreview(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (_preview == null)
        {
            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = 30f;
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 40f;
        }
        RebuildPreviewMesh();
        _preview.BeginPreview(rect, GUIStyle.none);
        _preview.camera.transform.position = new Vector3(1.6f, 1.8f, -2.4f);
        _preview.camera.transform.LookAt(new Vector3(0f, 0.9f, 0f));
        _preview.camera.clearFlags = CameraClearFlags.SolidColor;
        _preview.camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
        if (_previewMesh != null && _previewMesh.vertexCount > 0)
        {
            if (_previewMat == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                    _previewMat = new Material(shader);
            }
            if (_previewMat != null)
            {
                _previewMat.color = new Color(0.35f, 0.75f, 0.4f, 1f);
                _preview.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMat, 0);
            }
        }
        _preview.camera.Render();
        var tex = _preview.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
    }

    void RebuildPreviewMesh()
    {
        if (_previewMesh == null)
            _previewMesh = new Mesh { name = "BranchPreview" };
        var lines = new List<Vector3>();
        var colors = new List<Color>();
        var trunk = new Color(0.45f, 0.32f, 0.18f, 1f);
        var limb = new Color(0.35f, 0.85f, 0.45f, 1f);
        AddSegment(lines, colors, Vector3.zero, Vector3.up * 1.2f, trunk);
        if (_config?.branches != null)
        {
            for (int i = 0; i < _config.branches.Count; i++)
            {
                var bake = BranchPathBake.Bake(_config.branches[i], _config.seed + i);
                if (bake.positions == null) continue;
                Vector3 crown = Vector3.up * 1.2f;
                for (int p = 1; p < bake.positions.Length; p++)
                    AddSegment(lines, colors, crown + bake.positions[p - 1], crown + bake.positions[p], limb);
            }
        }
        _previewMesh.Clear();
        if (lines.Count < 2) return;
        var idx = new int[lines.Count];
        for (int i = 0; i < idx.Length; i++)
            idx[i] = i;
        _previewMesh.SetVertices(lines);
        _previewMesh.SetColors(colors);
        _previewMesh.SetIndices(idx, MeshTopology.Lines, 0);
    }

    static void AddSegment(List<Vector3> lines, List<Color> colors, Vector3 a, Vector3 b, Color color)
    {
        lines.Add(a);
        lines.Add(b);
        colors.Add(color);
        colors.Add(color);
    }
}

static class TreeGenExamplePlantGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    static void Draw(TreeGenExamplePlant plant, GizmoType type)
    {
        if (plant == null) return;
        Vector3 crown = plant.Crown;
        Gizmos.color = new Color(0.45f, 0.32f, 0.18f, 1f);
        Gizmos.DrawLine(plant.transform.position, crown);
        if (plant.bakes == null) return;
        for (int i = 0; i < plant.bakes.Count; i++)
        {
            var bake = plant.bakes[i];
            var branch = plant.branches != null && i < plant.branches.Count ? plant.branches[i] : null;
            if (bake?.positions == null) continue;
            Gizmos.color = new Color(0.3f, 0.85f, 0.45f, 1f);
            for (int p = 0; p < bake.positions.Length; p++)
            {
                Vector3 world = crown + bake.positions[p];
                Gizmos.DrawSphere(world, 0.035f);
                Vector3 tangent = p < bake.tangents.Length ? bake.tangents[p] : Vector3.forward;
                DrawArrow(world, tangent, 0.18f);
                if (p > 0)
                    Gizmos.DrawLine(crown + bake.positions[p - 1], world);
            }
            if (branch == null) continue;
            Vector3 origin = branch.originRotation * Vector3.forward;
            Gizmos.color = new Color(0.95f, 0.85f, 0.2f, 1f);
            DrawArrow(crown, origin, 0.35f);
            Vector3 grabber = crown + bake.GrabberPoint(branch.grabberT01);
            Vector3 gTangent = bake.GrabberTangent(branch.grabberT01);
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 1f);
            DrawArc(grabber, gTangent, branch.grabberRotationDeg, 0.12f);
        }
    }

    static void DrawArrow(Vector3 origin, Vector3 dir, float length)
    {
        if (dir.sqrMagnitude < 1e-8f) return;
        Vector3 tip = origin + dir.normalized * length;
        Gizmos.DrawLine(origin, tip);
        Vector3 side = Vector3.Cross(dir.normalized, Vector3.up);
        if (side.sqrMagnitude < 1e-6f)
            side = Vector3.Cross(dir.normalized, Vector3.right);
        side.Normalize();
        Gizmos.DrawLine(tip, tip - dir.normalized * 0.05f + side * 0.03f);
        Gizmos.DrawLine(tip, tip - dir.normalized * 0.05f - side * 0.03f);
    }

    static void DrawArc(Vector3 center, Vector3 tangent, float angleDeg, float radius)
    {
        Vector3 n = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(n, Vector3.up);
        if (side.sqrMagnitude < 1e-6f)
            side = Vector3.Cross(n, Vector3.right);
        side.Normalize();
        Vector3 binormal = Vector3.Cross(n, side);
        int steps = 16;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 prev = center + side * radius;
        for (int i = 1; i <= steps; i++)
        {
            float a = rad * (i / (float)steps);
            Vector3 p = center + (Mathf.Cos(a) * side + Mathf.Sin(a) * binormal) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
#endif
