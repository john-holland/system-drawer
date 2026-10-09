using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlantWindPartKind
{
    Tree = 0,
    Branch = 1,
    Leaf = 2,
    Petal = 3,
    Fruit = 4
}

/// <summary>Impulse and bend caps for a branch. Fruit uses this until a fruit editor exists.</summary>
[Serializable]
public struct PlantPartWindLimit
{
    public float impulseCap;
    [Min(0f)] public float bendCap;
}

public struct PlantWindBin
{
    public float bend;
    public float advectedWind;
    public float baseForce;
    public float effectiveLimit;
    public Vector3 bendAxis;
}

public sealed class PlantRadialWindRing
{
    public PlantWindPartKind kind;
    public int parentIndex = -1;
    public float length = 1f;
    public float limit;
    public PlantWindBin[] bins = Array.Empty<PlantWindBin>();
}

/// <summary>
/// Nested azimuth rings. The tree ring is the parent. Each branch is a child ring.
/// Leaves, petals, and fruit are children of a branch. A bin stores bend, advected wind,
/// and the effective limit, which is also written into the cache texture.
/// </summary>
public sealed class PlantRadialWindCache
{
    public const int DefaultSectors = 16;

    public Vector3 worldPosition;
    public int sectors = DefaultSectors;
    public int rebakeStamp;
    public bool omitBend;
    public bool petalOmitted;
    public Vector3[] clearedPetalCurve = Array.Empty<Vector3>();
    public PlantRadialWindRing tree;
    public readonly List<PlantRadialWindRing> branches = new List<PlantRadialWindRing>();
    public readonly List<PlantRadialWindRing> leaves = new List<PlantRadialWindRing>();
    public readonly List<PlantRadialWindRing> petals = new List<PlantRadialWindRing>();
    public readonly List<PlantRadialWindRing> fruits = new List<PlantRadialWindRing>();
    public Texture2D texture;

    public static float MinPositive(float[] values, float fallback)
    {
        float best = float.MaxValue;
        bool any = false;
        if (values != null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 1e-4f)
                    continue;
                any = true;
                if (values[i] < best)
                    best = values[i];
            }
        }
        return any ? best : fallback;
    }

    public static float SlackStiffness(float[] current, float[] limits)
    {
        float slack = 1f;
        int n = Math.Min(current != null ? current.Length : 0, limits != null ? limits.Length : 0);
        bool any = false;
        for (int i = 0; i < n; i++)
        {
            if (limits[i] <= 1e-4f)
                continue;
            any = true;
            slack = Mathf.Min(slack, Mathf.Max(0f, current[i] - limits[i]));
        }
        if (!any)
            return 1f;
        return Mathf.Max(0.05f, slack);
    }

    public static float TreeBendCap(TreeGrowthTravelAgent tree)
    {
        if (tree == null)
            return 1f;
        return MinPositive(tree.RedLimit01(), 1f);
    }

    public static float TreeStiffness(TreeGrowthTravelAgent tree)
    {
        if (tree == null)
            return 1f;
        return SlackStiffness(tree.DashedWhiteActive01(), tree.RedLimit01());
    }

    public static float FlowerBendCap(FlowerDevStep step)
    {
        if (step == null)
            return 1f;
        return MinPositive(new[] { step.limitWater01, step.limitSun01, step.limitGround01, step.limitRetinue01 }, 1f);
    }

    public static float FlowerStiffness(FlowerDevStep step)
    {
        if (step == null)
            return 1f;
        return SlackStiffness(
            new[] { step.water01, step.sun01, step.ground01, 1f },
            new[] { step.limitWater01, step.limitSun01, step.limitGround01, step.limitRetinue01 });
    }

    public static float ResolveBendCap(PlantPartWindLimit limit, float fallback)
    {
        return limit.bendCap > 1e-4f ? limit.bendCap : fallback;
    }

    /// <summary>A part never exceeds its parent. Leaf and fruit caps are the min of tree, branch, and their own cap.</summary>
    public static float EffectiveLimit(float treeLimit, float branchLimit, float ownLimit)
    {
        return Mathf.Min(Mathf.Max(0f, treeLimit), Mathf.Min(Mathf.Max(0f, branchLimit), Mathf.Max(0f, ownLimit)));
    }

    public static float CantileverDelta(float forceAcross, float length, float stiffnessEI, float limit)
    {
        float ei = Mathf.Max(1e-4f, stiffnessEI);
        float span = Mathf.Max(0f, length);
        float force = Mathf.Max(0f, forceAcross);
        float delta = (force * span * span * span) / (3f * ei);
        return Mathf.Min(delta, Mathf.Max(0f, limit));
    }

    public static float AlongDelta(float tipDelta, float along01, float limit)
    {
        float u = Mathf.Clamp01(along01);
        float delta = tipDelta * u * u * u;
        return Mathf.Min(delta, Mathf.Max(0f, limit));
    }

    public static Vector3 BendAxis(Vector3 wind, Vector3 tangent)
    {
        Vector3 t = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.forward;
        Vector3 across = Vector3.ProjectOnPlane(wind, t);
        if (across.sqrMagnitude < 1e-8f)
            across = Vector3.Cross(t, Vector3.up);
        if (across.sqrMagnitude < 1e-8f)
            across = Vector3.right;
        return across.normalized;
    }

    public static float ForceAcross(Vector3 wind, Vector3 tangent, float impulseCap)
    {
        Vector3 t = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.forward;
        float across = Vector3.ProjectOnPlane(wind, t).magnitude;
        if (impulseCap > 1e-4f)
            across = Mathf.Min(across, impulseCap);
        return across;
    }

    public static int SectorForDirection(Vector3 direction, int sectorCount)
    {
        int count = Mathf.Max(4, sectorCount);
        float az = Mathf.Atan2(direction.x, direction.z);
        float u = Mathf.Repeat(az / (Mathf.PI * 2f), 1f);
        return Mathf.Clamp(Mathf.FloorToInt(u * count), 0, count - 1);
    }

    public static int Sector(float azimuth01, int sectorCount)
    {
        int count = Mathf.Max(4, sectorCount);
        int s = Mathf.FloorToInt(Mathf.Repeat(azimuth01, 1f) * count);
        return Mathf.Clamp(s, 0, count - 1);
    }

    /// <summary>RGBAFloat texel. R is δ, G is advected wind, A is the effective limit.</summary>
    public static Color EncodeTexel(PlantWindBin bin, float along01)
    {
        float delta = AlongDelta(bin.bend, along01, bin.effectiveLimit);
        return new Color(delta, bin.advectedWind, 0.5f, bin.effectiveLimit);
    }

    /// <summary>CPU mirror of PlantWindBend.shader: offset length is min(R, A).</summary>
    public static float ShaderDelta(Color texel)
    {
        return Mathf.Min(texel.r, texel.a);
    }

    public static Vector3 ApplyWindVertex(Vector3 vertex, bool omit, float delta, Vector3 bendAxis, float advectedWind)
    {
        if (omit)
            return vertex;
        return vertex + VertexOffset(delta, bendAxis, advectedWind);
    }

    public static Vector3 VertexOffset(float delta, Vector3 bendAxis, float advectedWind)
    {
        Vector3 axis = bendAxis.sqrMagnitude > 1e-8f ? bendAxis.normalized : Vector3.right;
        return axis * (delta * advectedWind);
    }

    public PlantRadialWindRing AddRing(PlantWindPartKind kind, int parentIndex, float length, float parentLimit, float ownLimit)
    {
        var ring = new PlantRadialWindRing
        {
            kind = kind,
            parentIndex = parentIndex,
            length = Mathf.Max(0.01f, length),
            limit = kind == PlantWindPartKind.Tree ? Mathf.Max(0f, ownLimit) : EffectiveLimit(parentLimit, parentLimit, ownLimit),
            bins = new PlantWindBin[Mathf.Max(4, sectors)]
        };
        ListFor(kind).Add(ring);
        return ring;
    }

    public void FillBin(PlantRadialWindRing ring, int sector, float forceAcross, float stiffnessEI, Vector3 axis)
    {
        if (ring == null || ring.bins == null || sector < 0 || sector >= ring.bins.Length)
            return;
        float bend = CantileverDelta(forceAcross, ring.length, stiffnessEI, ring.limit);
        float force = Mathf.Max(0f, forceAcross);
        ring.bins[sector] = new PlantWindBin
        {
            bend = bend,
            advectedWind = force,
            baseForce = force,
            effectiveLimit = ring.limit,
            bendAxis = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.right
        };
    }

    public Texture2D Upload(PlantRadialWindRing ring, int alongSamples = 8)
    {
        if (ring == null || ring.bins == null || ring.bins.Length == 0)
            return texture;
        int width = ring.bins.Length;
        int height = Mathf.Max(2, alongSamples);
        if (texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true)
            {
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "PlantRadialWindCache"
            };
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float along = height == 1 ? 1f : y / (float)(height - 1);
                texture.SetPixel(x, y, EncodeTexel(ring.bins[x], along));
            }
        }
        texture.Apply(false, false);
        return texture;
    }

    List<PlantRadialWindRing> ListFor(PlantWindPartKind kind)
    {
        switch (kind)
        {
            case PlantWindPartKind.Branch: return branches;
            case PlantWindPartKind.Leaf: return leaves;
            case PlantWindPartKind.Petal: return petals;
            case PlantWindPartKind.Fruit: return fruits;
            default: return branches;
        }
    }
}
