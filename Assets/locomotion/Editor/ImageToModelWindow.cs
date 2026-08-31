#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using Locomotion.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Image / voxel mesh authoring: USC sync, Modly generate, bone↔vertex gizmos, named loop bounds.
/// Attaches <see cref="VoxelRagdollActor"/> and imports GLB via the project glTFast importer.
/// </summary>
public sealed class ImageToModelWindow : EditorWindow
{
    string _ucc = "http://127.0.0.1:5050";
    string _artworkId = "";
    Texture2D _image;
    Texture2D _mask;
    Texture2D _north;
    Texture2D _south;
    Texture2D _east;
    Texture2D _west;
    Texture2D _up;
    Texture2D _down;
    SkinnedMeshRenderer _smr;
    GranularitySettings _gran = GranularitySettings.Minecraft();
    string _assignment = "mediapipe";
    string _status = "";
    Vector2 _scroll;
    int _selectedBone;
    UnityWebRequest _pending;
    string _pendingKind;

    [MenuItem("Window/System Drawer/Mesh/Image to Model")]
    public static void OpenMenu()
    {
        var w = GetWindow<ImageToModelWindow>("Image to Model");
        w.minSize = new Vector2(420f, 520f);
        w.Show();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGui;
        if (_smr == null && Selection.activeGameObject != null)
            _smr = Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        if (_pending != null)
        {
            _pending.Dispose();
            _pending = null;
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("UCC / USC (Continuuuum Flask)", EditorStyles.boldLabel);
        _ucc = EditorGUILayout.TextField("UCC base", _ucc);
        _artworkId = EditorGUILayout.TextField("artworkId", _artworkId);
        _image = (Texture2D)EditorGUILayout.ObjectField("Source image", _image, typeof(Texture2D), false);
        _mask = (Texture2D)EditorGUILayout.ObjectField("Mask (optional)", _mask, typeof(Texture2D), false);
        if (GUILayout.Button("Sync media meta from USC"))
            StartGet("/api/image-to-model/media/" + _artworkId, "meta");
        if (GUILayout.Button("Generate / cache via Modly"))
            StartPostModly();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Granularity (spatial, not timeline)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Minecraft"))
            _gran = GranularitySettings.Minecraft();
        if (GUILayout.Button("Continuuuum"))
            _gran = GranularitySettings.Continuuuum();
        EditorGUILayout.EndHorizontal();
        _gran.pixelGrid = EditorGUILayout.IntSlider("pixelGrid", _gran.pixelGrid, 8, 64);
        float bpm = _gran.BlocksPerMeter;
        EditorGUI.BeginChangeCheck();
        float nextBm = EditorGUILayout.FloatField("blockMeters (m / block)", _gran.blockMeters);
        float nextBpm = EditorGUILayout.FloatField("blocksPerMeter", bpm);
        if (EditorGUI.EndChangeCheck())
        {
            if (!Mathf.Approximately(nextBm, _gran.blockMeters) && nextBm > 0f)
                _gran.blockMeters = nextBm;
            else if (!Mathf.Approximately(nextBpm, bpm) && nextBpm > 0f)
                _gran.BlocksPerMeter = nextBpm;
        }
        _gran.texelsPerMeter = EditorGUILayout.IntField("texelsPerMeter", _gran.texelsPerMeter);
        _gran.voxelCell = EditorGUILayout.Slider("voxelCell (blocks)", _gran.voxelCell, 1f / 64f, 1f);
        _gran.skinLayout = EditorGUILayout.TextField("skinLayout", _gran.skinLayout);
        _gran.maxBones = EditorGUILayout.IntField("maxBones", _gran.maxBones);
        _gran.snapToGrid = EditorGUILayout.Toggle("snapToGrid", _gran.snapToGrid);
        _gran.MarkCustomIfEdited();
        EditorGUILayout.LabelField("preset", _gran.preset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PixelLight faces", EditorStyles.boldLabel);
        _north = (Texture2D)EditorGUILayout.ObjectField("north", _north, typeof(Texture2D), false);
        _south = (Texture2D)EditorGUILayout.ObjectField("south", _south, typeof(Texture2D), false);
        _east = (Texture2D)EditorGUILayout.ObjectField("east", _east, typeof(Texture2D), false);
        _west = (Texture2D)EditorGUILayout.ObjectField("west", _west, typeof(Texture2D), false);
        _up = (Texture2D)EditorGUILayout.ObjectField("up", _up, typeof(Texture2D), false);
        _down = (Texture2D)EditorGUILayout.ObjectField("down", _down, typeof(Texture2D), false);

        EditorGUILayout.Space();
        _smr = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned mesh", _smr, typeof(SkinnedMeshRenderer), true);
        _assignment = EditorGUILayout.TextField("assignment source", _assignment);
        EditorGUILayout.HelpBox("mediapipe | mocapanything | custom. Auto-assign uses ArbitrarySkeletonFitter + Human:* / Animal: merge.", MessageType.Info);

        if (GUILayout.Button("Create named loop bounds (per bone)"))
            CreateLoopBounds();
        if (GUILayout.Button("Auto-assign bones"))
            AutoAssign();
        if (GUILayout.Button("Save BonedSkinnedAnimateableMeshRenderer"))
            SaveBoned();
        if (GUILayout.Button("Attach VoxelRagdollActor"))
            AttachVoxelRagdoll();

        DrawLists();
        EditorGUILayout.HelpBox(_status, MessageType.None);
        EditorGUILayout.EndScrollView();
        PollRequest();
    }

    void DrawLists()
    {
        if (_smr == null || _smr.sharedMesh == null)
            return;
        var mesh = _smr.sharedMesh;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("triangles", mesh.triangles != null ? (mesh.triangles.Length / 3).ToString() : "0");
        EditorGUILayout.LabelField("vertices", mesh.vertexCount.ToString());
        var bones = _smr.bones;
        if (bones == null)
            return;
        EditorGUILayout.LabelField("bones", bones.Length.ToString());
        var weights = mesh.boneWeights;
        _selectedBone = EditorGUILayout.IntSlider("gizmo bone", _selectedBone, 0, Mathf.Max(0, bones.Length - 1));
        if (_selectedBone < bones.Length && bones[_selectedBone] != null)
        {
            int vCount = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (HasBone(weights[i], _selectedBone))
                    vCount++;
            }
            EditorGUILayout.LabelField("vertices per bone", vCount.ToString());
        }
        EditorGUILayout.LabelField("Bones per vertex (first 8)");
        int shown = Mathf.Min(8, weights.Length);
        for (int v = 0; v < shown; v++)
        {
            var w = weights[v];
            EditorGUILayout.LabelField("v" + v, BoneLabel(bones, w.boneIndex0) + " " + w.weight0.ToString("0.00")
                + " | " + BoneLabel(bones, w.boneIndex1) + " " + w.weight1.ToString("0.00"));
        }
    }

    static string BoneLabel(Transform[] bones, int i)
    {
        if (bones == null || i < 0 || i >= bones.Length || bones[i] == null)
            return "-";
        return bones[i].name;
    }

    static bool HasBone(BoneWeight w, int bone)
    {
        return (w.boneIndex0 == bone && w.weight0 > 0.01f)
               || (w.boneIndex1 == bone && w.weight1 > 0.01f)
               || (w.boneIndex2 == bone && w.weight2 > 0.01f)
               || (w.boneIndex3 == bone && w.weight3 > 0.01f);
    }

    void CreateLoopBounds()
    {
        if (_smr == null || _smr.sharedMesh == null)
        {
            _status = "Assign a skinned mesh.";
            return;
        }
        var bones = _smr.bones;
        if (bones == null)
            return;
        Undo.RegisterFullObjectHierarchyUndo(_smr.gameObject, "Create loop bounds");
        int n = 0;
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;
            SkinnedMeshLoopSplitBounds.CreateUnderMesh(_smr.transform, _smr.sharedMesh, "bone-" + i, bones[i].name);
            n++;
        }
        _status = "Created " + n + " SkinnedMeshLoopSplitBounds (overlap = selection).";
        SceneView.RepaintAll();
    }

    void AutoAssign()
    {
        if (_smr == null || _smr.bones == null)
        {
            _status = "Assign a skinned mesh.";
            return;
        }
        var source = new List<string>();
        var parents = new List<int>();
        for (int i = 0; i < _smr.bones.Length; i++)
        {
            source.Add(_smr.bones[i] != null ? _smr.bones[i].name : "bone_" + i);
            parents.Add(-1);
        }
        var prefix = _assignment != null && _assignment.IndexOf("mocap", System.StringComparison.OrdinalIgnoreCase) >= 0
            ? "Animal"
            : "Human";
        var targets = new List<string>(GranularitySettings.MinecraftHumanoidTraits);
        var fit = ArbitrarySkeletonFitter.Fit(source, parents, targets, prefix);
        _status = "Fitted " + fit.pairs.Count + " pairs (" + prefix + ":). Unmatched " + fit.unmatchedSource.Count + ".";
        var boned = EnsureBoned();
        boned.assignmentSource = string.IsNullOrEmpty(_assignment) ? "custom" : _assignment;
        EditorUtility.SetDirty(boned);
    }

    void SaveBoned()
    {
        var boned = EnsureBoned();
        boned.artworkId = _artworkId;
        boned.granularity = _gran;
        boned.CaptureFromRenderer(_assignment);
        EditorUtility.SetDirty(boned);
        _status = "Saved BonedSkinnedAnimateableMeshRenderer source=" + boned.assignmentSource;
    }

    void AttachVoxelRagdoll()
    {
        GameObject root = _smr != null ? _smr.transform.root.gameObject : Selection.activeGameObject;
        if (root == null)
        {
            _status = "Select a mesh or actor root.";
            return;
        }
        var actor = root.GetComponent<VoxelRagdollActor>();
        if (actor == null)
            actor = Undo.AddComponent<VoxelRagdollActor>(root);
        actor.granularity = _gran;
        if (_smr != null)
            actor.bonedMesh = EnsureBoned();
        actor.north = _north;
        actor.south = _south;
        actor.east = _east;
        actor.west = _west;
        actor.up = _up;
        actor.down = _down;
        actor.ApplyBlockScale();
        if (_smr != null)
            actor.BindPixelLightToRenderer(_smr, "north");
        EditorUtility.SetDirty(actor);
        _status = "VoxelRagdollActor on " + root.name + " scale=" + _gran.blockMeters + " m/block";
    }

    BonedSkinnedAnimateableMeshRenderer EnsureBoned()
    {
        if (_smr == null)
            throw new System.InvalidOperationException("skinned mesh");
        var boned = _smr.GetComponent<BonedSkinnedAnimateableMeshRenderer>();
        if (boned == null)
            boned = Undo.AddComponent<BonedSkinnedAnimateableMeshRenderer>(_smr.gameObject);
        boned.skinned = _smr;
        return boned;
    }

    void OnSceneGui(SceneView view)
    {
        if (_smr == null || _smr.sharedMesh == null || _smr.bones == null)
            return;
        var bones = _smr.bones;
        var mesh = _smr.sharedMesh;
        var verts = mesh.vertices;
        var weights = mesh.boneWeights;
        Handles.color = Color.cyan;
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;
            float s = i == _selectedBone ? 0.06f : 0.03f;
            Handles.SphereHandleCap(0, bones[i].position, Quaternion.identity, s, EventType.Repaint);
        }
        if (_selectedBone < 0 || _selectedBone >= bones.Length || bones[_selectedBone] == null)
            return;
        Handles.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        int drawn = 0;
        for (int v = 0; v < weights.Length && drawn < 128; v++)
        {
            if (!HasBone(weights[v], _selectedBone))
                continue;
            Vector3 worldV = _smr.transform.TransformPoint(verts[v]);
            Handles.DrawLine(bones[_selectedBone].position, worldV);
            drawn++;
        }
        var boxes = _smr.GetComponentsInChildren<SkinnedMeshLoopSplitBounds>(true);
        Handles.color = new Color(0.4f, 1f, 0.5f, 0.25f);
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == null)
                continue;
            Handles.matrix = boxes[i].transform.localToWorldMatrix;
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
        }
        Handles.matrix = Matrix4x4.identity;
    }

    void StartGet(string path, string kind)
    {
        if (string.IsNullOrEmpty(_artworkId))
        {
            _status = "artworkId required";
            return;
        }
        DisposePending();
        _pending = UnityWebRequest.Get(_ucc.TrimEnd('/') + path);
        _pendingKind = kind;
        _pending.SendWebRequest();
        _status = "requesting…";
    }

    void StartPostModly()
    {
        if (string.IsNullOrEmpty(_artworkId))
        {
            _status = "artworkId required";
            return;
        }
        DisposePending();
        var json = "{\"artworkId\":\"" + _artworkId + "\",\"t\":-1,\"meshFormat\":\"glb\"}";
        _pending = new UnityWebRequest(_ucc.TrimEnd('/') + "/api/image-to-model/modly", "POST");
        _pending.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        _pending.downloadHandler = new DownloadHandlerBuffer();
        _pending.SetRequestHeader("Content-Type", "application/json");
        _pendingKind = "modly";
        _pending.SendWebRequest();
        _status = "Modly…";
    }

    void PollRequest()
    {
        if (_pending == null || !_pending.isDone)
            return;
        if (_pending.result != UnityWebRequest.Result.Success)
        {
            _status = _pending.error;
            DisposePending();
            Repaint();
            return;
        }
        if (_pendingKind == "modly")
        {
            string text = _pending.downloadHandler.text;
            bool ok = text != null && (text.IndexOf("\"ok\":true", System.StringComparison.Ordinal) >= 0
                                       || text.IndexOf("\"ok\": true", System.StringComparison.Ordinal) >= 0);
            DisposePending();
            _status = "modly: " + Trunc(text, 400);
            if (ok && !string.IsNullOrEmpty(_artworkId))
                StartGet("/api/image-to-model/media/" + _artworkId + "/generated_mesh?t=-1", "mesh");
            Repaint();
            return;
        }
        if (_pendingKind == "mesh")
        {
            byte[] bytes = _pending.downloadHandler.data;
            DisposePending();
            TryImportGlb(bytes);
            Repaint();
            return;
        }
        _status = _pendingKind + ": " + Trunc(_pending.downloadHandler.text, 400);
        DisposePending();
        Repaint();
    }

    void TryImportGlb(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            _status = "Modly mesh empty.";
            return;
        }
        string dir = Path.Combine(Application.dataPath, "locomotion", "GeneratedMeshes");
        Directory.CreateDirectory(dir);
        string safe = string.IsNullOrEmpty(_artworkId) ? "mesh" : _artworkId;
        foreach (char c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');
        string file = Path.Combine(dir, safe + ".glb");
        File.WriteAllBytes(file, bytes);
        string assetPath = "Assets/locomotion/GeneratedMeshes/" + Path.GetFileName(file);
        AssetDatabase.Refresh();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _smr = inst != null ? inst.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (_smr != null)
            {
                EnsureBoned().CaptureFromRenderer(_assignment);
                AttachVoxelRagdoll();
            }
            _status = "Imported GLB via glTFast (" + bytes.Length + " bytes) → " + assetPath;
        }
        else
            _status = "Wrote " + assetPath + " (" + bytes.Length + " bytes). Assign a SkinnedMeshRenderer if the importer did not produce a prefab.";
    }

    void DisposePending()
    {
        if (_pending != null)
        {
            _pending.Dispose();
            _pending = null;
        }
        _pendingKind = null;
    }

    static string Trunc(string s, int n)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= n)
            return s;
        return s.Substring(0, n) + "…";
    }
}
#endif
