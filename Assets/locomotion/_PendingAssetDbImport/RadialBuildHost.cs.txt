using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene host for radial build: CenterPost, custom wrap, start-post anchors, preview of solved joints.
/// </summary>
[AddComponentMenu("Locomotion/Build/Radial Build Host")]
[ExecuteAlways]
public sealed class RadialBuildHost : MonoBehaviour
{
    public const string StartPostAnchorName = "startPostAnchor";
    public const string StartPostBoundsName = "startPostBounds";

    public GameObject centerPost;
    public CustomRadialSideAsset customSide;
    public GameObject customAngleObject;
    public float customAngle;
    public RadialBuildSpec spec = new RadialBuildSpec();
    public Vector3 pieceSize = Vector3.one;
    public int previewConfigIndex = -1;
    public List<RadialSolvedConfig> solvedConfigs = new List<RadialSolvedConfig>();

    public Transform StartPostAnchor => FindChild(centerPost != null ? centerPost.transform : transform, StartPostAnchorName);
    public RadialStartPostBounds StartPostBounds =>
        FindChild(centerPost != null ? centerPost.transform : transform, StartPostBoundsName)
            ?.GetComponent<RadialStartPostBounds>();

    public GameObject EnsureCenterPost()
    {
        if (centerPost != null)
            return centerPost;
        var go = new GameObject("CenterPost");
        go.transform.SetParent(transform, false);
        centerPost = go;
        return go;
    }

    public void CreateAnchorObjects()
    {
        var post = EnsureCenterPost();
        var t = post.transform;
        if (FindChild(t, StartPostAnchorName) == null)
        {
            var a = new GameObject(StartPostAnchorName);
            a.transform.SetParent(t, false);
            a.transform.localPosition = Vector3.forward;
        }
        if (FindChild(t, StartPostBoundsName) == null)
        {
            var b = new GameObject(StartPostBoundsName);
            b.transform.SetParent(t, false);
            b.transform.localPosition = Vector3.forward;
            b.transform.localScale = Vector3.one * 0.25f;
            b.AddComponent<RadialStartPostBounds>();
            var col = b.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = Vector3.zero;
            col.size = Vector3.one;
        }
    }

    public void SnapStartPostFromBounds()
    {
        var bounds = StartPostBounds;
        var anchor = StartPostAnchor;
        if (bounds == null || anchor == null)
            return;
        Vector3 centroid = bounds.SelectedCentroid();
        Vector3 facing = bounds.FacingVector();
        anchor.position = centroid;
        if (facing.sqrMagnitude > 1e-8f)
            anchor.rotation = Quaternion.LookRotation(facing, spec != null && spec.axis.sqrMagnitude > 1e-8f ? spec.axis : Vector3.up);
    }

    public void RefreshSolved()
    {
        solvedConfigs.Clear();
        var pose = customSide != null ? customSide.ToPose() : default;
        if (customAngle > 0f)
            pose.customAngle = customAngle;
        if (customAngleObject != null)
        {
            pose.hasCustomAngleObject = true;
            pose.customAngleObjectWorld = customAngleObject.transform.position;
        }
        if (spec != null)
        {
            spec.useCustomSide = customSide != null;
            spec.customSide = pose;
            if (customAngle > 0f)
                spec.wrapAngleDeg = customAngle;
            if (centerPost != null)
                spec.centerPostPosition = centerPost.transform.position;
        }
        Vector3 center = centerPost != null ? centerPost.transform.position : transform.position;
        Vector3 axis = spec != null && spec.axis.sqrMagnitude > 1e-8f ? spec.axis : Vector3.up;
        var anchor = StartPostAnchor;
        bool hasStart = anchor != null;
        Vector3 startPos = hasStart ? anchor.position : Vector3.zero;
        Vector3 startFace = hasStart ? anchor.forward : Vector3.zero;
        var bounds = StartPostBounds;
        if (bounds != null)
            startFace = bounds.FacingVector();
        var join = spec != null ? spec.joinKind : RadialJoinKind.Natural;
        float off = spec != null ? spec.joinOffset : 0f;
        solvedConfigs.AddRange(RadialSlotMath.SolveWorkingJoints(
            pose, pieceSize, join, off, center, axis, hasStart, startPos, startFace));
        if (previewConfigIndex >= solvedConfigs.Count)
            previewConfigIndex = solvedConfigs.Count > 0 ? 0 : -1;
        if (previewConfigIndex >= 0 && previewConfigIndex < solvedConfigs.Count && spec != null)
            spec.ApplySolved(solvedConfigs[previewConfigIndex]);
    }

    public string[] PreviewLabels()
    {
        if (solvedConfigs == null || solvedConfigs.Count == 0)
            return System.Array.Empty<string>();
        var labels = new string[solvedConfigs.Count];
        for (int i = 0; i < solvedConfigs.Count; i++)
            labels[i] = solvedConfigs[i] != null ? solvedConfigs[i].DisplayLabel() : "(empty)";
        return labels;
    }

    static Transform FindChild(Transform root, string name)
    {
        if (root == null)
            return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c != null && c.name == name)
                return c;
        }
        return null;
    }
}
