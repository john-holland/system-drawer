using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene host: axle CenterPost, per-link PixelLight mounts, SPH pull bake.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Garage Chain Assembly")]
[ExecuteAlways]
public sealed class GarageChainAssembly : MonoBehaviour
{
    public GarageChainSpec spec;
    public RadialBuildHost radialHost;
    public PixelLightGridMountGameObject axleMount;
    public List<PixelLightGridMountGameObject> linkMounts = new List<PixelLightGridMountGameObject>();
    public GarageChainSphPullField pullField = new GarageChainSphPullField();
    public RopeSystem rope;

    public Transform AxleTransform
    {
        get
        {
            if (radialHost != null && radialHost.centerPost != null)
                return radialHost.centerPost.transform;
            return transform;
        }
    }

    public RadialBuildHost EnsureHost()
    {
        if (radialHost == null)
            radialHost = GetComponent<RadialBuildHost>() ?? gameObject.AddComponent<RadialBuildHost>();
        radialHost.EnsureCenterPost();
        if (spec != null)
        {
            spec.SyncRadialFromTeeth();
            radialHost.spec = spec.radialBuild;
            var post = radialHost.centerPost.transform;
            post.localPosition = spec.axleLocalPosition;
            post.localEulerAngles = spec.axleLocalEuler;
            post.localScale = Vector3.one * Mathf.Max(0.01f, spec.axleDiameterM);
        }
        return radialHost;
    }

    public void EnsureAxleMount()
    {
        if (axleMount != null) return;
        var go = new GameObject("AxlePixelLight");
        go.transform.SetParent(transform, false);
        axleMount = go.AddComponent<PixelLightGridMountGameObject>();
        if (spec != null)
        {
            axleMount.gridWidth = spec.pixelLightGridW;
            axleMount.gridHeight = spec.pixelLightGridH;
            axleMount.cellSize = spec.pixelLightCellSize;
        }
        axleMount.radialHost = EnsureHost();
    }

    public void RebuildLinkMounts()
    {
        if (spec == null) return;
        EnsureHost();
        int n = spec.LinkCount;
        while (linkMounts.Count < n)
        {
            var go = new GameObject("ChainLinkMount_" + linkMounts.Count);
            go.transform.SetParent(transform, false);
            var m = go.AddComponent<PixelLightGridMountGameObject>();
            m.gridWidth = spec.pixelLightGridW;
            m.gridHeight = spec.pixelLightGridH;
            m.cellSize = spec.pixelLightCellSize;
            m.radialHost = radialHost;
            m.radialBuild = spec.radialBuild;
            linkMounts.Add(m);
        }
        for (int i = linkMounts.Count - 1; i >= n; i--)
        {
            if (linkMounts[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(linkMounts[i].gameObject);
                else
                    DestroyImmediate(linkMounts[i].gameObject);
            }
            linkMounts.RemoveAt(i);
        }

        Vector3 center = AxleTransform.position;
        Vector3 axis = spec.radialBuild != null && spec.radialBuild.axis.sqrMagnitude > 1e-8f
            ? spec.radialBuild.axis
            : Vector3.right;
        float radius = Mathf.Max(0.02f, spec.pitchRadiusM);
        for (int i = 0; i < n && i < linkMounts.Count; i++)
        {
            var m = linkMounts[i];
            if (m == null) continue;
            m.transform.position = RadialSlotMath.PolarSlot(center, axis, radius, i, n, 0f, 360f);
        }
    }

    public void BakePull()
    {
        if (spec == null) return;
        pullField ??= new GarageChainSphPullField();
        pullField.Bake(spec);
    }

    public void ApplySteelToRope()
    {
        if (rope == null || spec == null) return;
        spec.steel.ApplyTo(rope.Config, spec.selectedKind);
        rope.Config.totalLengthM = spec.totalLengthM;
        rope.Config.segmentLengthM = spec.linkPitchM;
        rope.Config.mode = RopeMode.Spool;
    }
}
