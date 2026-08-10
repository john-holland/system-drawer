using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Solid waste mass with density + compaction; SPH-style particle buffer in hopper.</summary>
[Serializable]
public sealed class GarbageBagParticle
{
    public Vector3 localPos;
    public Vector3 velocity;
    public float mass = 0.05f;
}

[Serializable]
public sealed class GarbageBag
{
    public string bagConfigId = "random_garbage_bag";
    public float massKg = 10f;
    public float densityKgPerM3 = 250f;
    [Range(0f, 1f)] public float compaction01;
    public float compactionRate = 0.15f;
    public float maxCompactedDensity = 800f;
    public List<GarbageBagParticle> particles = new List<GarbageBagParticle>();
    public int maxParticles = 64;

    public float VolumeM3 => densityKgPerM3 > 1e-4f ? massKg / densityKgPerM3 : 0f;

    public void RebuildParticlesFromMass()
    {
        int n = Mathf.Clamp(Mathf.RoundToInt(massKg), 0, maxParticles);
        while (particles.Count < n)
        {
            particles.Add(new GarbageBagParticle
            {
                localPos = UnityEngine.Random.insideUnitSphere * 0.4f,
                mass = 0.05f
            });
        }
        while (particles.Count > n)
            particles.RemoveAt(particles.Count - 1);
    }

    /// <summary>SPH-ish viscosity step — pulls particles toward compacted center.</summary>
    public void TickSphCompaction(float dt)
    {
        if (particles.Count == 0)
            RebuildParticlesFromMass();
        compaction01 = Mathf.Clamp01(compaction01 + compactionRate * dt);
        densityKgPerM3 = Mathf.Lerp(densityKgPerM3, maxCompactedDensity, compaction01 * dt);
        Vector3 center = Vector3.zero;
        for (int i = 0; i < particles.Count; i++)
            center += particles[i].localPos;
        if (particles.Count > 0) center /= particles.Count;
        float stiff = 2f + compaction01 * 6f;
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            Vector3 to = center - p.localPos;
            p.velocity += to * stiff * dt;
            p.velocity *= Mathf.Clamp01(1f - 0.4f * dt);
            p.localPos += p.velocity * dt;
            particles[i] = p;
        }
    }

    public void AcceptMass(float kg)
    {
        massKg = Mathf.Max(0f, massKg + kg);
        RebuildParticlesFromMass();
    }
}

[CreateAssetMenu(fileName = "GarbageBagSpec", menuName = "Locomotion/Civil/Garbage Bag Spec")]
public sealed class GarbageBagSpec : ScriptableObject
{
    public string bagId = "random_garbage_bag";
    public string displayName = "Random Garbage Bag";
    public List<string> commodityKeys = new List<string> { "organic", "plastic", "paper" };
    public List<float> commodityWeights = new List<float> { 0.5f, 0.3f, 0.2f };
    public float defaultMassKg = 8f;
    public float defaultDensity = 220f;

    public GarbageBag CreateBag()
    {
        var bag = new GarbageBag
        {
            bagConfigId = bagId,
            massKg = defaultMassKg,
            densityKgPerM3 = defaultDensity
        };
        bag.RebuildParticlesFromMass();
        return bag;
    }
}
