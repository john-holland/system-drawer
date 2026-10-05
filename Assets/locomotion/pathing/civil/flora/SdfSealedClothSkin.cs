using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cloth yield for a sealed shell. Tear follows weave strain; the shell stays a closed volume until it splits.
/// </summary>
[Serializable]
public sealed class DelicateClothlikeMaterial
{
    public float stiffness = 8f;
    public float damping = 4f;
    public float yieldPressure = 0.35f;
    [Min(0.0001f)] public float muddleDistance = 0.02f;
    [Min(0f)] public float paintYieldPerMeter = 0.0015f;
    public ClothElasticProperties elastic = new ClothElasticProperties
    {
        stiffness = 8f,
        damping = 4f,
        recovery01 = 0.35f
    };

    public static DelicateClothlikeMaterial Fallback => new DelicateClothlikeMaterial();

    public float YieldProduct(float strain01)
    {
        float weave = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(strain01));
        return Mathf.Max(1e-5f, yieldPressure) * Mathf.Max(0.0001f, muddleDistance) * weave;
    }

    public float PaintAmount(float pressure, float muddle, float strain01)
    {
        float load = Mathf.Max(0f, pressure) * Mathf.Max(0f, muddle);
        float yield = YieldProduct(strain01);
        if (load <= yield) return 0f;
        return (load - yield) / yield * Mathf.Max(0f, paintYieldPerMeter);
    }
}

[Serializable]
public sealed class SdfSealedClothFragment
{
    public string id;
    public Vector3 centroid;
    public Vector3[] curve = Array.Empty<Vector3>();
    public bool broken;
}

/// <summary>
/// Tracks one deformable cloth piece as a sealed SDF shell and splits it into curved reduced sections.
/// </summary>
[AddComponentMenu("Locomotion/Civil/SDF Sealed Cloth Skin")]
public sealed class SdfSealedClothSkin : MonoBehaviour
{
    public DelicateClothlikeMaterial material = new DelicateClothlikeMaterial();
    public List<SdfSealedClothFragment> fragments = new List<SdfSealedClothFragment>();
    public float muddleDistanceAccum;
    [Range(0f, 4f)] public float pressure01;
    [Range(0f, 1f)] public float strain01;
    [Range(0f, 1f)] public float tear01;
    public float lastPaintAmount;
    public bool enclosed;
    public Transform enclosedActor;
    public ClothingDamageLayer damage;
    public PaintTransferDecal decal;
    public int fragmentSerial;

    public void AccumulateStrain(ClothUvStretchCache cache, Vector2 uv)
    {
        if (cache == null) return;
        strain01 = Mathf.Max(strain01, cache.Sample(uv).r);
    }

    public bool ApplyMuddle(float pressure, float muddleDelta, Vector3 point, Vector3 normal, Collider stainTarget = null)
    {
        pressure01 = Mathf.Max(0f, pressure);
        muddleDistanceAccum += Mathf.Max(0f, muddleDelta);
        var mat = material ?? DelicateClothlikeMaterial.Fallback;
        lastPaintAmount = mat.PaintAmount(pressure01, muddleDistanceAccum, strain01);
        if (lastPaintAmount <= 0f) return false;

        var tangent = Vector3.Cross(normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up, Vector3.right);
        if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.forward;
        tangent.Normalize();
        fragmentSerial++;
        var section = new SdfSealedClothFragment
        {
            id = "section_" + fragmentSerial,
            centroid = point + tangent * 0.5f,
            curve = CurvedSection(point, tangent, lastPaintAmount),
            broken = true
        };
        fragments.Add(section);
        tear01 = Mathf.Clamp01(fragments.Count / 4f);
        if (damage != null)
            damage.ApplyTear(tear01);
        if (decal != null && stainTarget != null)
            decal.TryApply(stainTarget, point, normal.sqrMagnitude > 1e-6f ? normal : Vector3.up, PetalStainColor(lastPaintAmount));
        return true;
    }

    public void EncloseActor(Transform actor)
    {
        enclosedActor = actor;
        enclosed = actor != null;
        if (actor != null)
            transform.position = actor.position;
    }

    public static Vector3[] CurvedSection(Vector3 origin, Vector3 tangent, float amount)
    {
        Vector3 n = Vector3.Cross(tangent, Vector3.up);
        if (n.sqrMagnitude < 1e-6f)
            n = Vector3.Cross(tangent, Vector3.right);
        n.Normalize();
        float bow = Mathf.Max(0.01f, amount);
        return new[]
        {
            origin,
            origin + tangent * 0.5f + n * bow,
            origin + tangent
        };
    }

    public static Color PetalStainColor(float paintAmount)
    {
        float a = Mathf.Clamp01(0.25f + paintAmount * 4f);
        return new Color(0.55f, 0.12f, 0.28f, a);
    }
}
