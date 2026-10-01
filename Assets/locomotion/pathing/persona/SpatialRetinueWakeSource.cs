using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds spatially placed actors to civil venue retinues (SG enumerate or scene scan).
/// Optional demographic weight sort uses samplePos (not spawn) when spatial gradient is set.
/// </summary>
[AddComponentMenu("Locomotion/Persona/Spatial Retinue Wake Source")]
public sealed class SpatialRetinueWakeSource : MonoBehaviour
{
    public float bindRadiusM = 25f;
    public LayerMask actorMask = ~0;
    public CivilianDemographics demographics;

    /// <summary>Ingest placed instances from SpatialGenerator.EnumeratePlacedInstances.</summary>
    public void IngestPlaced(CivilVenueNode venue, IEnumerable<GameObject> placed)
    {
        if (venue == null || placed == null) return;
        if (venue.retinue == null) venue.retinue = new List<RetinuePeckingEntry>();
        var pending = new List<(GameObject go, float weight)>();
        foreach (var go in placed)
        {
            if (go == null) continue;
            if (Vector3.Distance(go.transform.position, venue.WorldPosition) > bindRadiusM)
                continue;
            if (HasActor(venue, go)) continue;
            float w = DemographicWeight(go.transform.position, venue);
            pending.Add((go, w));
        }
        pending.Sort((a, b) => b.weight.CompareTo(a.weight));
        int peck = 40;
        for (int i = 0; i < pending.Count; i++)
        {
            var go = pending[i].go;
            venue.retinue.Add(new RetinuePeckingEntry
            {
                personaKey = go.name,
                role = venue.kind.ToString().ToLowerInvariant(),
                peckingOrder = peck,
                actor = go
            });
            peck += 10;
        }
    }

    float DemographicWeight(Vector3 spawnPos, CivilVenueNode venue)
    {
        var demo = demographics;
        if (demo == null)
        {
            var owner = venue?.contextOwner;
            if (owner != null)
            {
                var warden = owner.GetComponent<CareerWarden>() ?? owner.GetComponentInParent<CareerWarden>();
                if (warden != null) demo = warden.demographics;
            }
        }
        if (demo?.spatialGradient == null || !demo.spatialGradient.IsActive)
            return 0.5f;
        // Weight sort uses samplePos for demographics; actors stay at spawnPos
        Vector3 samplePos = demo.spatialGradient.SamplePosForDemographics(spawnPos, spawnPos.GetHashCode());
        return demo.spatialGradient.WeightAt(samplePos);
    }

    public void CollectNearby(CivilVenueNode venue)
    {
        if (venue == null) return;
        var hits = Physics.OverlapSphere(venue.WorldPosition, bindRadiusM, actorMask, QueryTriggerInteraction.Ignore);
        var list = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            var root = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
            if (!seen.Add(root)) continue;
            if (root == venue.contextOwner) continue;
            // Prefer ambulating / TravelAgent actors
            if (root.GetComponent<TravelAgent>() == null && root.GetComponent<PhysicsCardSolver>() == null)
                continue;
            list.Add(root);
        }
        IngestPlaced(venue, list);
    }

    static bool HasActor(CivilVenueNode venue, GameObject go)
    {
        if (venue.retinue == null) return false;
        for (int i = 0; i < venue.retinue.Count; i++)
            if (venue.retinue[i] != null && venue.retinue[i].actor == go)
                return true;
        return false;
    }
}
