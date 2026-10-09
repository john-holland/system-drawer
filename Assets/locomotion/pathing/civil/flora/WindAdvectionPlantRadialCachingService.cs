using System;
using System.Collections.Generic;
using UnityEngine;
using Weather;
using Weather.Lod;

/// <summary>One plant in a copse. Branch and fruit control points move only inside the weather egg.</summary>
[Serializable]
public sealed class PlantWindSubject
{
    public Vector3 worldPosition;
    public TreeGrowthTravelAgent tree;
    public FlowerTravelAgent flower;
    public PlantBranchDef branch;
    public BranchPathBake branchBake;
    public Vector3[] branchRest;
    public PlanarSplinePathLocomotion branchRibbon;
    public BranchPathBake fruitBake;
    public Vector3[] fruitRest;
    public PlanarSplinePathLocomotion fruitRibbon;
    public Vector3 flowerCentroid;
    public Vector3 limbTangent = Vector3.forward;
    public float limbLength = 1f;
    public PlantRadialWindCache cache;
    public SdfSealedClothFragment petalFragment;
    public float petalTear01;
    public Vector3 petalNormal = Vector3.up;
    public Vector3[] petalCurve;
    public FlowerDevStep petalStep;
    public Vector3[] fruitPoints;
    public BranchPathBake[] liveBranches;
    public float fruitClearRadius = 0.08f;
    public float woodClearRadius = 0.05f;
    public bool lodged;
    public int capsuleId;
    public bool capsulePushed;
}

/// <summary>
/// Prebakes a turbulence ring from WeatherSystem wind and the manifold cell velocity.
/// A copse reduces downstream bend where canopies share a manifold cell.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Wind Advection Plant Radial Caching Service")]
public sealed class WindAdvectionPlantRadialCachingService : MonoBehaviour
{
    public WeatherSystem weather;
    public PlayerWeatherEggZone egg;
    public bool cacheRealtimeOnEggMove = true;
    public float eggMoveThreshold = 1f;
    [Range(0f, 1f)] public float shelterFactor = 0.45f;
    public float gustAmplitude = 0.15f;
    public int sectors = PlantRadialWindCache.DefaultSectors;
    public int turbulenceSeed = 1;
    public List<PlantWindSubject> copse = new List<PlantWindSubject>();
    public PlantWindCapsuleBuffer passCapsules = new PlantWindCapsuleBuffer();
    public float limbRadius = 0.04f;
    public float capsuleExtraSize = 0.03f;

    Vector3 _bakedEggCenter;
    bool _eggBaked;

    void Update()
    {
        if (!cacheRealtimeOnEggMove || egg == null)
            return;
        TickEggCenter(egg.Center, turbulenceSeed);
    }

    public bool IsInsideEgg(Vector3 world)
    {
        return egg != null && WeatherEggBounds.Contains(egg.Center, egg.Radii, world);
    }

    bool IsInside(Vector3 world, Vector3 center)
    {
        return egg != null && WeatherEggBounds.Contains(center, egg.Radii, world);
    }

    public float ControlPointDelta(float forceAcross, float length, float stiffnessEI, float limit)
    {
        return PlantRadialWindCache.CantileverDelta(forceAcross, length, stiffnessEI, limit);
    }

    public void RebakeAll(int seed)
    {
        turbulenceSeed = seed;
        for (int i = 0; i < copse.Count; i++)
            BakeSubject(copse[i], seed);
        ApplyCopseShelter();
    }

    public void TickEggCenter(Vector3 center, int seed)
    {
        if (!cacheRealtimeOnEggMove)
            return;
        if (_eggBaked && (center - _bakedEggCenter).sqrMagnitude < eggMoveThreshold * eggMoveThreshold)
            return;
        _bakedEggCenter = center;
        _eggBaked = true;
        turbulenceSeed = seed;
        for (int i = 0; i < copse.Count; i++)
        {
            PlantWindSubject subject = copse[i];
            if (subject == null || !IsInside(subject.worldPosition, center))
                continue;
            BakeSubject(subject, seed);
        }
        ApplyCopseShelter();
    }

    public PlantRadialWindCache BakeSubject(PlantWindSubject subject, int seed)
    {
        if (subject == null)
            return null;
        var cache = subject.cache ?? new PlantRadialWindCache();
        cache.sectors = Mathf.Max(4, sectors);
        cache.worldPosition = subject.worldPosition;
        cache.branches.Clear();
        cache.leaves.Clear();
        cache.petals.Clear();
        cache.fruits.Clear();
        cache.omitBend = false;
        cache.petalOmitted = false;
        cache.clearedPetalCurve = Array.Empty<Vector3>();

        if (subject.branch != null && subject.branchBake != null)
            subject.branchBake.broken = subject.branch.broken;
        bool broken = (subject.branch != null && subject.branch.broken)
                      || (subject.branchBake != null && subject.branchBake.broken);
        bool torn = PlantWindClearance.PetalOmitted(subject.petalFragment, subject.petalTear01);

        float treeCap = PlantRadialWindCache.TreeBendCap(subject.tree);
        float treeEI = PlantRadialWindCache.TreeStiffness(subject.tree);
        float branchCap = subject.branch != null
            ? PlantRadialWindCache.ResolveBendCap(subject.branch.windLimit, treeCap)
            : treeCap;
        float fruitCap = branchCap;
        FlowerDevStep step = subject.flower != null ? subject.flower.SelectedStep : null;
        float flowerCap = PlantRadialWindCache.FlowerBendCap(step);
        float flowerEI = PlantRadialWindCache.FlowerStiffness(step);
        float length = subject.branchBake != null && subject.branchBake.length > 1e-4f
            ? subject.branchBake.length
            : Mathf.Max(0.01f, subject.limbLength);

        cache.tree = new PlantRadialWindRing
        {
            kind = PlantWindPartKind.Tree,
            parentIndex = -1,
            length = length,
            limit = treeCap,
            bins = new PlantWindBin[cache.sectors]
        };
        PlantRadialWindRing branchRing = null;
        if (!broken)
        {
            branchRing = cache.AddRing(PlantWindPartKind.Branch, 0, length, treeCap, branchCap);
            cache.AddRing(PlantWindPartKind.Leaf, 0, length, branchRing.limit, flowerCap);
            if (!torn)
                cache.AddRing(PlantWindPartKind.Petal, 0, length, branchRing.limit, flowerCap);
            cache.AddRing(PlantWindPartKind.Fruit, 0, length, branchRing.limit, fruitCap);
        }

        float impulse = subject.branch != null ? subject.branch.windLimit.impulseCap : 0f;
        Vector3 tangent = subject.limbTangent.sqrMagnitude > 1e-8f ? subject.limbTangent : Vector3.forward;
        for (int s = 0; s < cache.sectors; s++)
        {
            float az = (s + 0.5f) / cache.sectors * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Sin(az), 0f, Mathf.Cos(az));
            Vector3 wind = SampleWind(subject.worldPosition + dir);
            float gust = SeededGust(seed, s, gustAmplitude);
            wind *= 1f + gust;
            float force = PlantRadialWindCache.ForceAcross(wind, tangent, impulse);
            Vector3 axis = PlantRadialWindCache.BendAxis(wind, tangent);
            cache.FillBin(cache.tree, s, force, treeEI, axis);
            if (branchRing == null)
                continue;
            cache.FillBin(branchRing, s, force, treeEI, axis);
            cache.FillBin(cache.leaves[cache.leaves.Count - 1], s, force, flowerEI, axis);
            if (cache.petals.Count > 0)
                cache.FillBin(cache.petals[cache.petals.Count - 1], s, force, flowerEI, axis);
            cache.FillBin(cache.fruits[cache.fruits.Count - 1], s, force, treeEI, axis);
        }

        if (broken)
        {
            cache.omitBend = true;
            SyncBrokenCapsule(subject);
        }
        else if (!torn)
            cache.clearedPetalCurve = BuildClearedPetal(subject);
        else
            cache.petalOmitted = true;

        cache.Upload(branchRing != null ? branchRing : cache.tree);
        cache.rebakeStamp++;
        subject.cache = cache;
        return cache;
    }

    void SyncBrokenCapsule(PlantWindSubject subject)
    {
        Vector3[] positions = subject.branchBake != null ? subject.branchBake.positions : null;
        if (positions == null || positions.Length == 0)
            return;
        var capsule = new PlantWindCapsule
        {
            a = subject.worldPosition + positions[0],
            b = subject.worldPosition + positions[positions.Length - 1],
            radius = Mathf.Max(0f, limbRadius),
            extraSize = Mathf.Max(0f, capsuleExtraSize),
            intent = subject.lodged ? PlantWindCapsuleIntent.Lodge : PlantWindCapsuleIntent.Fall
        };
        if (passCapsules == null)
            passCapsules = new PlantWindCapsuleBuffer();
        if (!subject.capsulePushed)
        {
            subject.capsuleId = passCapsules.Push(capsule);
            subject.capsulePushed = true;
            return;
        }
        if (subject.lodged)
            passCapsules.SetIntent(subject.capsuleId, PlantWindCapsuleIntent.Lodge);
    }

    static Vector3[] BuildClearedPetal(PlantWindSubject subject)
    {
        Vector3[] curve = subject.petalCurve;
        if ((curve == null || curve.Length == 0) && subject.petalFragment?.curve != null)
            curve = subject.petalFragment.curve;
        if (curve == null || curve.Length == 0)
            return Array.Empty<Vector3>();
        var fruit = new List<Vector3>();
        if (subject.fruitPoints != null)
            fruit.AddRange(subject.fruitPoints);
        PlantWindClearance.CollectFruit(subject.petalStep, fruit);
        var live = new List<BranchPathBake>();
        if (subject.liveBranches != null)
        {
            for (int i = 0; i < subject.liveBranches.Length; i++)
            {
                BranchPathBake bake = subject.liveBranches[i];
                if (bake != null && !bake.broken)
                    live.Add(bake);
            }
        }
        if (subject.branchBake != null && !subject.branchBake.broken)
            live.Add(subject.branchBake);
        return PlantWindClearance.ClearPetalCurve(
            curve,
            subject.petalNormal,
            fruit,
            subject.fruitClearRadius,
            live,
            subject.woodClearRadius);
    }

    public Vector3 SampleWind(Vector3 world)
    {
        Vector3 wind = Vector3.zero;
        bool any = false;
        if (weather != null && weather.wind != null)
        {
            wind = weather.wind.GetWindAtPosition(world, world.y);
            any = true;
        }
        WeatherPhysicsManifold manifold = weather != null ? weather.weatherPhysicsManifold : null;
        if (manifold != null)
        {
            Vector3 cell = manifold.GetDataAtPosition(world).velocity;
            if (cell.sqrMagnitude > 1e-8f)
            {
                wind = any ? wind + cell : cell;
                any = true;
            }
        }
        if (!any)
            wind = new Vector3(0.3f, 0f, 0f);
        return wind;
    }

    public static float SeededGust(int seed, int sector, float amplitude)
    {
        var rng = new System.Random(unchecked(seed * 397) ^ sector);
        return ((float)rng.NextDouble() * 2f - 1f) * amplitude;
    }

    public void ApplyCopseShelter()
    {
        WeatherPhysicsManifold manifold = weather != null ? weather.weatherPhysicsManifold : null;
        if (manifold == null || copse.Count < 2)
            return;
        for (int i = 0; i < copse.Count; i++)
        {
            PlantWindSubject down = copse[i];
            if (down?.cache?.tree?.bins == null)
                continue;
            for (int j = 0; j < copse.Count; j++)
            {
                if (i == j)
                    continue;
                PlantWindSubject up = copse[j];
                if (up?.cache == null)
                    continue;
                if (!ShareManifoldCell(manifold, up.worldPosition, down.worldPosition))
                    continue;
                Vector3 travel = down.worldPosition - up.worldPosition;
                Vector3 wind = SampleWind(up.worldPosition);
                if (wind.sqrMagnitude < 1e-8f || Vector3.Dot(travel, wind) <= 0f)
                    continue;
                int sector = PlantRadialWindCache.SectorForDirection(wind, down.cache.sectors);
                ShelterRing(down.cache.tree, sector, down);
                if (down.cache.branches.Count > 0)
                    ShelterRing(down.cache.branches[0], sector, down);
                if (down.cache.fruits.Count > 0)
                    ShelterRing(down.cache.fruits[0], sector, down);
            }
        }
    }

    void ShelterRing(PlantRadialWindRing ring, int sector, PlantWindSubject subject)
    {
        if (ring?.bins == null || sector < 0 || sector >= ring.bins.Length)
            return;
        PlantWindBin bin = ring.bins[sector];
        float sheltered = bin.baseForce * Mathf.Clamp01(shelterFactor);
        float ei = ring.kind == PlantWindPartKind.Petal || ring.kind == PlantWindPartKind.Leaf
            ? PlantRadialWindCache.FlowerStiffness(subject.flower != null ? subject.flower.SelectedStep : null)
            : PlantRadialWindCache.TreeStiffness(subject.tree);
        float bend = PlantRadialWindCache.CantileverDelta(sheltered, ring.length, ei, ring.limit);
        bin.advectedWind = sheltered;
        bin.bend = bend;
        bin.effectiveLimit = ring.limit;
        ring.bins[sector] = bin;
    }

    public static bool ShareManifoldCell(WeatherPhysicsManifold manifold, Vector3 a, Vector3 b)
    {
        if (manifold == null)
            return false;
        return ManifoldCell(manifold, a) == ManifoldCell(manifold, b);
    }

    public static Vector3Int ManifoldCell(WeatherPhysicsManifold manifold, Vector3 world)
    {
        Vector3 local = world - manifold.worldBounds.min;
        Vector3 size = manifold.worldBounds.size;
        Vector3Int count = manifold.cellCount;
        int x = size.x > 0f ? Mathf.FloorToInt(local.x / size.x * count.x) : 0;
        int y = size.y > 0f ? Mathf.FloorToInt(local.y / size.y * count.y) : 0;
        int z = size.z > 0f ? Mathf.FloorToInt(local.z / size.z * count.z) : 0;
        x = Mathf.Clamp(x, 0, Mathf.Max(0, count.x - 1));
        y = Mathf.Clamp(y, 0, Mathf.Max(0, count.y - 1));
        z = Mathf.Clamp(z, 0, Mathf.Max(0, count.z - 1));
        return new Vector3Int(x, y, z);
    }

    public void ApplyBranchPoints(BranchPathBake bake, Vector3[] rest, PlantWindBin bin, bool insideEgg)
    {
        if (bake == null || rest == null)
            return;
        if (bake.positions == null || bake.positions.Length != rest.Length)
            bake.positions = (Vector3[])rest.Clone();
        for (int i = 0; i < rest.Length; i++)
        {
            Vector3 point = rest[i];
            if (insideEgg && rest.Length > 1)
            {
                float along = i / (float)(rest.Length - 1);
                Color texel = PlantRadialWindCache.EncodeTexel(bin, along);
                float delta = PlantRadialWindCache.ShaderDelta(texel);
                point += PlantRadialWindCache.VertexOffset(delta, bin.bendAxis, bin.advectedWind);
            }
            bake.positions[i] = point;
        }
    }

    public Vector3 ApplyFlowerCentroid(Vector3 restCentroid, bool insideEgg)
    {
        return restCentroid;
    }

    public void ApplyLod(PlantWindSubject subject, bool insideEgg)
    {
        if (subject?.cache == null)
            return;
        bool broken = (subject.branch != null && subject.branch.broken)
                      || (subject.branchBake != null && subject.branchBake.broken);
        bool move = insideEgg && !broken;
        PlantWindBin branchBin = TipBin(subject.cache.branches.Count > 0 ? subject.cache.branches[0] : subject.cache.tree);
        PlantWindBin fruitBin = TipBin(subject.cache.fruits.Count > 0 ? subject.cache.fruits[0] : null);
        ApplyBranchPoints(subject.branchBake, subject.branchRest, branchBin, move);
        ApplyBranchPoints(subject.fruitBake, subject.fruitRest, fruitBin, move);
        subject.branchBake?.ApplyToRibbon(subject.branchRibbon);
        subject.fruitBake?.ApplyToRibbon(subject.fruitRibbon);
        subject.flowerCentroid = ApplyFlowerCentroid(subject.flowerCentroid, insideEgg);
    }

    static PlantWindBin TipBin(PlantRadialWindRing ring)
    {
        if (ring?.bins == null || ring.bins.Length == 0)
            return default;
        PlantWindBin best = ring.bins[0];
        for (int i = 1; i < ring.bins.Length; i++)
        {
            if (ring.bins[i].bend > best.bend)
                best = ring.bins[i];
        }
        return best;
    }
}
