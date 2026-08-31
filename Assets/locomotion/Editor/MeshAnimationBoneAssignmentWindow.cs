#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.EditorTools;
using Locomotion.Rig;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fitting-wizard UX: map source joints onto mesh bones / named loop bounds.
/// Associations parent under AnimationBehaviorTree, not the input BehaviorTree.
/// </summary>
public sealed class MeshAnimationBoneAssignmentWindow : EditorWindow
{
    GameObject _actorRoot;
    BonedSkinnedAnimateableMeshRenderer _boned;
    AnimationBehaviorTree _animationTree;
    BoneMap _boneMap;
    string _sourceKind = "mediapipe";
    Vector2 _scroll;
    readonly List<Row> _rows = new List<Row>();
    string _status = "";

    sealed class Row
    {
        public string sourceJointId;
        public Transform meshBone;
        public SkinnedMeshLoopSplitBounds bounds;
    }

    [MenuItem("Window/System Drawer/Animation/Mesh animation bone assignment")]
    public static void OpenMenu()
    {
        var w = GetWindow<MeshAnimationBoneAssignmentWindow>("Mesh bone assignment");
        w.minSize = new Vector2(520f, 480f);
        w.Show();
    }

    void OnEnable()
    {
        if (_actorRoot == null && Selection.activeGameObject != null)
            Prefill(Selection.activeGameObject);
    }

    void Prefill(GameObject go)
    {
        _actorRoot = go;
        _boned = go.GetComponentInChildren<BonedSkinnedAnimateableMeshRenderer>(true);
        var rs = go.GetComponentInChildren<RagdollSystem>(true);
        _animationTree = rs != null ? rs.animationTree : go.GetComponentInChildren<AnimationBehaviorTree>(true);
        _boneMap = go.GetComponentInChildren<BoneMap>(true);
        RebuildRows();
    }

    void RebuildRows()
    {
        _rows.Clear();
        var traits = new List<string>();
        if (_boneMap != null && _boneMap.entries != null)
        {
            for (int i = 0; i < _boneMap.entries.Count; i++)
            {
                var e = _boneMap.entries[i];
                if (e != null && !string.IsNullOrEmpty(e.traitId))
                    traits.Add(e.traitId);
            }
        }
        if (traits.Count == 0)
        {
            if (_sourceKind == "mocapanything")
            {
                traits.Add("Animal:Root");
                traits.Add("Animal:Spine");
                traits.Add("Animal:Head");
            }
            else
            {
                traits.AddRange(GranularitySettings.MinecraftHumanoidTraits);
            }
        }
        var smr = _boned != null ? _boned.Renderer : (_actorRoot != null ? _actorRoot.GetComponentInChildren<SkinnedMeshRenderer>(true) : null);
        var boneByName = new Dictionary<string, Transform>();
        if (smr != null && smr.bones != null)
        {
            for (int i = 0; i < smr.bones.Length; i++)
            {
                if (smr.bones[i] != null && !boneByName.ContainsKey(smr.bones[i].name))
                    boneByName.Add(smr.bones[i].name, smr.bones[i]);
            }
        }
        var source = new List<string>(traits);
        var parents = new List<int>();
        for (int i = 0; i < source.Count; i++)
            parents.Add(-1);
        var targets = new List<string>(boneByName.Keys);
        var prefix = _sourceKind == "mocapanything" ? "Animal" : "Human";
        var fit = ArbitrarySkeletonFitter.Fit(source, parents, targets, prefix);
        var remap = fit.ToRemap();
        var boxes = smr != null ? smr.GetComponentsInChildren<SkinnedMeshLoopSplitBounds>(true) : System.Array.Empty<SkinnedMeshLoopSplitBounds>();
        for (int i = 0; i < traits.Count; i++)
        {
            var row = new Row { sourceJointId = traits[i] };
            if (remap.TryGetValue(traits[i], out var boneName) && boneByName.TryGetValue(boneName, out var t))
                row.meshBone = t;
            for (int b = 0; b < boxes.Length; b++)
            {
                if (boxes[b] != null && row.meshBone != null && boxes[b].loopName == row.meshBone.name)
                {
                    row.bounds = boxes[b];
                    break;
                }
            }
            _rows.Add(row);
        }
        _status = "Mapped " + fit.pairs.Count + " joints via ArbitrarySkeletonFitter.";
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Actor / AnimationBehaviorTree", EditorStyles.boldLabel);
        var next = (GameObject)EditorGUILayout.ObjectField("Actor root", _actorRoot, typeof(GameObject), true);
        if (next != _actorRoot && next != null)
            Prefill(next);
        _boned = (BonedSkinnedAnimateableMeshRenderer)EditorGUILayout.ObjectField(
            "Boned mesh", _boned, typeof(BonedSkinnedAnimateableMeshRenderer), true);
        _animationTree = (AnimationBehaviorTree)EditorGUILayout.ObjectField(
            "Animation tree", _animationTree, typeof(AnimationBehaviorTree), true);
        _boneMap = (BoneMap)EditorGUILayout.ObjectField("Bone map", _boneMap, typeof(BoneMap), true);
        _sourceKind = EditorGUILayout.TextField("assignment source", _sourceKind);
        EditorGUILayout.HelpBox(
            "Associations parent under RagdollSystem.animationTree (Default_animation_tree). Not the input BehaviorTree controller.",
            MessageType.Info);
        if (GUILayout.Button("Rebuild mapping"))
            RebuildRows();
        if (GUILayout.Button("Create ABT child associations"))
            CreateAssociations();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(row.sourceJointId, GUILayout.Width(180));
            row.meshBone = (Transform)EditorGUILayout.ObjectField(row.meshBone, typeof(Transform), true);
            row.bounds = (SkinnedMeshLoopSplitBounds)EditorGUILayout.ObjectField(row.bounds, typeof(SkinnedMeshLoopSplitBounds), true);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.HelpBox(_status, MessageType.None);
    }

    AnimationBehaviorTree ResolveTree()
    {
        if (_animationTree != null)
            return _animationTree;
        if (_actorRoot == null)
            return null;
        var rs = _actorRoot.GetComponentInChildren<RagdollSystem>(true);
        if (rs != null && rs.animationTree != null)
            return rs.animationTree;
        return _actorRoot.GetComponentInChildren<AnimationBehaviorTree>(true);
    }

    void CreateAssociations()
    {
        var tree = ResolveTree();
        if (tree == null)
        {
            _status = "No AnimationBehaviorTree (RagdollSystem.animationTree / Default_animation_tree).";
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(tree.gameObject, "Mesh bone associations");
        var existing = tree.GetComponentsInChildren<MeshBoneAssociation>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null)
                Undo.DestroyObjectImmediate(existing[i].gameObject);
        }
        int n = 0;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (row.meshBone == null && row.bounds == null)
                continue;
            var go = new GameObject("MeshBone_" + Sanitize(row.sourceJointId));
            Undo.RegisterCreatedObjectUndo(go, "Mesh bone association");
            go.transform.SetParent(tree.transform, false);
            var assoc = go.AddComponent<MeshBoneAssociation>();
            assoc.sourceJointId = row.sourceJointId;
            assoc.meshBone = row.meshBone;
            assoc.meshBoneName = row.meshBone != null ? row.meshBone.name : "";
            assoc.loopBounds = row.bounds;
            assoc.loopBoundName = row.bounds != null ? row.bounds.loopName : "";
            assoc.assignmentSource = _sourceKind;
            n++;
        }
        if (_boned != null)
        {
            _boned.assignmentSource = _sourceKind;
            EditorUtility.SetDirty(_boned);
        }
        _status = "Created " + n + " MeshBoneAssociation children under " + tree.name + ".";
    }

    static string Sanitize(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "Joint";
        return id.Replace(':', '_').Replace(' ', '_');
    }
}
#endif
