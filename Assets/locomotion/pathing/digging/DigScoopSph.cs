using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>Simplified dry-particle SPH through a scoop surface; subtracts a precomputed SdfMax expression.</summary>
public sealed class DigScoopSph
{
    public List<GarbageBagParticle> particles = new List<GarbageBagParticle>();
    public float thresholdSize = 0.12f;
    public bool createOwnTree;
    public float lastCapacity;
    public float lastLoad01 = 1f;

    public void SeedFill(int count, float radius = 0.5f)
    {
        particles.Clear();
        int n = Mathf.Max(0, count);
        for (int i = 0; i < n; i++)
        {
            particles.Add(new GarbageBagParticle
            {
                localPos = Random.insideUnitSphere * radius,
                mass = 0.05f
            });
        }
    }

    public float Scoop(AnimationCurve shovelDescend, float distance, float dt, float rotationDeg)
    {
        shovelDescend ??= AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
        float curvatureDrop = Mathf.Max(0f, shovelDescend.Evaluate(0f) - shovelDescend.Evaluate(1f));
        int moved = Mathf.Clamp(Mathf.RoundToInt(curvatureDrop * particles.Count), 0, particles.Count);
        lastCapacity = ScoopCapacityEstimator.Estimate(moved, distance, dt);
        lastLoad01 = TipMinimumSimulator.TipOff(moved / Mathf.Max(1f, (float)particles.Count), rotationDeg);
        for (int i = 0; i < moved && particles.Count > 0; i++)
            particles.RemoveAt(particles.Count - 1);
        return lastCapacity;
    }

    public SdfMaxNode BuildSubtractNode(Vector3 contact, float amount)
    {
        return new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            primitiveType = SdfPrimitiveType.Sphere,
            localPosition = contact,
            radius = Mathf.Max(0.05f, amount),
            sphereRadius = Mathf.Max(0.05f, amount)
        };
    }

    public bool UseRigidBody(float size) => size >= thresholdSize;
}
