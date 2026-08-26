#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Locomotion.EditorTools;
using Locomotion.Rig;
using UnityEditor;
using UnityEngine;

/// <summary>Fit MoCapAnything / PoseTrack skeletons onto a non-human RagdollActor.</summary>
public sealed class AnimalAnimationFittingWizardWindow : EditorWindow
{
    GameObject actorRoot;
    BoneMap boneMap;
    string species = "";
    string bvhPath = "";
    WebcamAnimRecordingAsset recording;
    TextAsset poseTrackJson;
    PoseTrack track;
    SkeletonFitResult fit;
    float playheadMs;
    string status = "";
    Vector2 scroll;
    Vector2 setScroll;

    [MenuItem("Window/System Drawer/Animation/Animal Animation Fitting Wizard", false, 305)]
    public static void ShowWindow()
    {
        var w = GetWindow<AnimalAnimationFittingWizardWindow>("Animal Animation Fitting");
        w.minSize = new Vector2(520, 560);
        w.Show();
    }

    void OnEnable()
    {
        if (actorRoot == null && Selection.activeGameObject != null)
            actorRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Animal Animation Fitting Wizard", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Non-human ambulating actors. Maps a MoCapAnything BVH or PoseTrack onto Generic:/Animal: BoneMap rows, then bakes a RagdollAnimationSet. Animation catalog is availableAnimations — not every scene BT (use Impulse Viewer for that).",
            MessageType.Info);

        actorRoot = (GameObject)EditorGUILayout.ObjectField("Actor root", actorRoot, typeof(GameObject), true);
        if (actorRoot != null)
            boneMap = actorRoot.GetComponent<BoneMap>();
        boneMap = (BoneMap)EditorGUILayout.ObjectField("BoneMap", boneMap, typeof(BoneMap), true);

        using (new EditorGUI.DisabledScope(actorRoot == null || Application.isPlaying))
        {
            if (GUILayout.Button("Ensure animation roots"))
            {
                RagdollAutoWire.EnsureAnimationRoots(actorRoot);
                boneMap = actorRoot.GetComponent<BoneMap>() ?? RagdollAutoWire.EnsureBoneMap(actorRoot);
                status = "Animation managers wired";
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        species = EditorGUILayout.TextField("Species", species);
        recording = (WebcamAnimRecordingAsset)EditorGUILayout.ObjectField("Recording", recording, typeof(WebcamAnimRecordingAsset), false);
        poseTrackJson = (TextAsset)EditorGUILayout.ObjectField("PoseTrack JSON", poseTrackJson, typeof(TextAsset), false);
        EditorGUILayout.BeginHorizontal();
        bvhPath = EditorGUILayout.TextField("BVH path", bvhPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string p = EditorUtility.OpenFilePanel("BVH", "", "bvh");
            if (!string.IsNullOrEmpty(p))
                bvhPath = p;
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load + fit"))
            RunFit();

        if (fit != null)
        {
            EditorGUILayout.LabelField("Fit table", EditorStyles.miniBoldLabel);
            int show = Mathf.Min(16, fit.pairs.Count);
            for (int i = 0; i < show; i++)
            {
                var p = fit.pairs[i];
                EditorGUILayout.LabelField($"{p.sourceId} → {p.targetTraitId} ({p.confidence:0.00}{(p.inferred ? ", inferred" : "")})");
            }
            if (fit.offeredAnimalRows.Count > 0)
                EditorGUILayout.HelpBox("Unmatched joints offered as Animal: rows (never dropped).", MessageType.None);
        }

        playheadMs = WebcamAnimTimelineFields.DrawPlayheadMs(
            "Playhead ms", playheadMs, WebcamAnimTimelineFields.PlayheadMaxMs(recording, track));
        using (new EditorGUI.DisabledScope(track == null || boneMap == null))
        {
            if (GUILayout.Button("Preview"))
            {
                var remapped = fit != null ? track.RemapTraitIds(fit.ToRemap()) : track;
                int n = PoseTrackPlayer.Apply(remapped, boneMap, playheadMs);
                status = $"Applied {n} bones";
            }
            if (GUILayout.Button("Apply BoneMap rows + create animation set"))
                ApplyAndBake();
        }

        DrawAvailableAnimations();

        if (GUILayout.Button("Open Impulse Viewer (all animation BTs)"))
            EditorApplication.ExecuteMenuItem("Window/System Drawer/Physics/Nervous System Impulse Viewer");

        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    void RunFit()
    {
        track = LoadTrack();
        var ids = new List<string>();
        var parents = new List<int>();
        if (!string.IsNullOrEmpty(bvhPath) && File.Exists(bvhPath))
        {
            var joints = new List<BvhPoseTrackImporter.Joint>();
            BvhPoseTrackImporter.CollectJoints(File.ReadAllText(bvhPath), joints);
            for (int i = 0; i < joints.Count; i++)
            {
                ids.Add(joints[i].name);
                parents.Add(joints[i].parent);
            }
            if (track == null || track.Count == 0)
                track = BvhPoseTrackImporter.FromFile(bvhPath, "mocapanything@v2");
        }
        else if (track != null)
        {
            track.CollectTraitIds(ids);
            for (int i = 0; i < ids.Count; i++)
                parents.Add(-1);
        }
        if (boneMap == null && actorRoot != null)
            boneMap = actorRoot.GetComponent<BoneMap>();
        fit = ArbitrarySkeletonFitter.FitToBoneMap(ids, parents, boneMap, "Animal");
        status = track != null ? $"Loaded {track.Count} samples" : "No source";
    }

    PoseTrack LoadTrack()
    {
        if (recording != null)
        {
            if (!string.IsNullOrEmpty(recording.species) && string.IsNullOrEmpty(species))
                species = recording.species;
            if (recording.lastTrack != null && recording.lastTrack.Count > 0)
                return recording.lastTrack;
            var loaded = ContinuuuumRemotePoseAnimationDetector.TryLoadJson(recording.poseTrackPath);
            if (loaded != null)
                return loaded;
        }
        if (poseTrackJson != null)
            return PoseTrack.FromJson(poseTrackJson.text);
        if (!string.IsNullOrEmpty(bvhPath) && File.Exists(bvhPath))
            return BvhPoseTrackImporter.FromFile(bvhPath, "mocapanything@v2");
        return null;
    }

    void ApplyAndBake()
    {
        if (actorRoot == null || track == null)
            return;
        RagdollAutoWire.EnsureAnimationRoots(actorRoot);
        boneMap = actorRoot.GetComponent<BoneMap>() ?? RagdollAutoWire.EnsureBoneMap(actorRoot);
        if (fit != null)
            ArbitrarySkeletonFitter.ApplyOfferedRows(boneMap, fit);
        var remapped = fit != null ? track.RemapTraitIds(fit.ToRemap()) : track;
        var ik = actorRoot.GetComponent<RagdollIKAnimationManager>() ??
                 actorRoot.GetComponentInChildren<RagdollIKAnimationManager>();
        string name = !string.IsNullOrEmpty(species) ? species : "AnimalMotion";
        if (recording != null && !string.IsNullOrEmpty(recording.displayName))
            name = recording.displayName;
        int idx = PoseTrackClipBaker.BakeAndAddSet(ik, remapped, boneMap, actorRoot.transform, name);
        EditorUtility.SetDirty(boneMap);
        status = idx >= 0 ? $"BoneMap updated, set [{idx}] {name}" : "Bake failed";
    }

    void DrawAvailableAnimations()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("availableAnimations", EditorStyles.boldLabel);
        var ik = actorRoot != null
            ? actorRoot.GetComponent<RagdollIKAnimationManager>() ?? actorRoot.GetComponentInChildren<RagdollIKAnimationManager>()
            : null;
        if (ik == null || ik.availableAnimations == null || ik.availableAnimations.Count == 0)
        {
            EditorGUILayout.HelpBox("No animation sets on this actor yet.", MessageType.None);
            return;
        }
        setScroll = EditorGUILayout.BeginScrollView(setScroll, GUILayout.Height(120));
        for (int i = 0; i < ik.availableAnimations.Count; i++)
        {
            var set = ik.availableAnimations[i];
            EditorGUILayout.LabelField($"[{i}] {(set != null ? set.displayName : "(null)")}");
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
