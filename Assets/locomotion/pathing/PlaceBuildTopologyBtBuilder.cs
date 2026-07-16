using System.Collections.Generic;
using UnityEngine;

/// <summary>Bakes place/build topology into BT-friendly segment lists / child actions.</summary>
public static class PlaceBuildTopologyBtBuilder
{
    public static List<string> BuildStepIds(PlaceBuildTopologyAsset asset)
    {
        var ids = new List<string>();
        if (asset?.nodes == null) return ids;
        for (int i = 0; i < asset.nodes.Count; i++)
        {
            if (asset.nodes[i] == null) continue;
            ids.Add("Find_" + asset.nodes[i].nodeId);
            ids.Add("CarryOrClimb_" + asset.nodes[i].nodeId);
            ids.Add("Place_" + asset.nodes[i].nodeId);
            if (asset.nodes[i].turnInChair)
                ids.Add("Turn_" + asset.nodes[i].nodeId);
            ids.Add("Occupy_" + asset.nodes[i].nodeId);
            if (asset.nodes[i].beat != null && asset.nodes[i].beat.autoClose != PlaceBuildAutoCloseMode.None)
                ids.Add("PlaceClose_" + asset.nodes[i].nodeId);
        }
        return ids;
    }

    public static GameObject FindGrabbable(Vector3 origin, float radius, SeatStandBridgeSpec bridge)
    {
        string[] tags = bridge?.grabbableSynonyms ?? new[] { "grabbable", "chair", "box" };
        Collider[] hits = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            string n = hits[i].gameObject.name.ToLowerInvariant();
            for (int t = 0; t < tags.Length; t++)
            {
                if (string.IsNullOrEmpty(tags[t])) continue;
                if (n.Contains(tags[t].ToLowerInvariant()) || hits[i].CompareTag(tags[t]))
                    return hits[i].attachedRigidbody != null
                        ? hits[i].attachedRigidbody.gameObject
                        : hits[i].gameObject;
            }
        }
        return null;
    }
}
