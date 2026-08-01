using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds spatially placed actors to civil venue retinues (SG enumerate or scene scan).
/// </summary>
[AddComponentMenu("Locomotion/Persona/Spatial Retinue Wake Source")]
public sealed class SpatialRetinueWakeSource : MonoBehaviour
{
    public float bindRadiusM = 25f;
    public LayerMask actorMask = ~0;

    /// <summary>Ingest placed instances from SpatialGenerator.EnumeratePlacedInstances.</summary>
    public void IngestPlaced(CivilVenueNode venue, IEnumerable<GameObject> placed)
    {
        if (venue == null || placed == null) return;
        if (venue.retinue == null) venue.retinue = new List<RetinuePeckingEntry>();
        int peck = 40;
        foreach (var go in placed)
        {
            if (go == null) continue;
            if (Vector3.Distance(go.transform.position, venue.WorldPosition) > bindRadiusM)
                continue;
            if (HasActor(venue, go)) continue;
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
