using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pollinator steering on top of <see cref="BoidsCrowdLayer"/>. Separation still comes from the boids solve.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Crowd Behavioral Retinue")]
public sealed class CrowdBehavioralRetinue : MonoBehaviour
{
    public string pollinatorFlockId = "pollinator";
    public float attractWeight = 0.8f;
    public float neighborRadius = 4f;
    public float separationWeight = 1.1f;
    public float alignmentWeight = 0.6f;
    public float cohesionWeight = 0.45f;
    public float maxLateralM = 1.25f;
    public float binSeparationRadius = 0.15f;

    public static Vector3 Steer(
        Vector3 selfPos,
        Vector3 selfFwd,
        string flockGroupId,
        string pollinatorFlockId,
        IReadOnlyList<TravelAgent> peers,
        Vector3 flowerPos,
        bool flowerReceptive,
        float attractWeight,
        float neighborRadius,
        float sepW,
        float aliW,
        float cohW,
        float maxLateralM,
        float binSeparationRadius)
    {
        Vector3 boids = BoidsCrowdLayer.ComputeSteering(
            selfPos, selfFwd, flockGroupId, peers, neighborRadius, sepW, aliW, cohW, maxLateralM);
        if (string.IsNullOrEmpty(flockGroupId) || flockGroupId != pollinatorFlockId || !flowerReceptive)
            return boids;
        Vector3 to = flowerPos - selfPos;
        to.y = 0f;
        Vector3 attract = Vector3.zero;
        Vector3 binSep = Vector3.zero;
        if (to.sqrMagnitude > 1e-6f)
        {
            float dist = to.magnitude;
            attract = to / dist * attractWeight;
            if (dist < binSeparationRadius)
                binSep = -(to / dist) * attractWeight;
        }
        Vector3 v = boids + attract + binSep;
        v.y = 0f;
        if (v.sqrMagnitude > maxLateralM * maxLateralM)
            v = v.normalized * maxLateralM;
        return v;
    }

    void LateUpdate()
    {
        var peers = TravelAgentRegistry.All;
        Vector3 flowerPos = Vector3.zero;
        bool receptive = TryNearestReceptiveFlower(out flowerPos);
        for (int i = 0; i < peers.Count; i++)
        {
            var self = peers[i];
            if (self == null || self.flockGroupId != pollinatorFlockId) continue;
            Vector3 offset = Steer(
                self.transform.position,
                self.transform.forward,
                self.flockGroupId,
                pollinatorFlockId,
                peers,
                flowerPos,
                receptive,
                attractWeight,
                neighborRadius,
                separationWeight,
                alignmentWeight,
                cohesionWeight,
                maxLateralM,
                binSeparationRadius);
            if (offset.sqrMagnitude < 1e-6f) continue;
            self.transform.position += offset * Time.deltaTime;
        }
    }

    public static bool StepReceptive(FlowerDevStep step)
    {
        if (step == null || !step.attractsPollinators) return false;
        return step.kind == FlowerDevStepKind.Anthesis || step.kind == FlowerDevStepKind.PollenExchange;
    }

    bool TryNearestReceptiveFlower(out Vector3 pos)
    {
        pos = Vector3.zero;
        var flowers = Object.FindObjectsByType<FlowerTravelAgent>(FindObjectsSortMode.None);
        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < flowers.Length; i++)
        {
            var flower = flowers[i];
            if (flower == null || !StepReceptive(flower.SelectedStep)) continue;
            float d = (flower.transform.position - transform.position).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            pos = flower.transform.position;
            found = true;
        }
        return found;
    }
}
