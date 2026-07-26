using UnityEngine;

/// <summary>Samples nearby ambulating actors to set PathingAperture.crowdOccupancy01.</summary>
public static class ApertureCrowdSampler
{
    public static void Refresh(PathingAperture aperture, float radius)
    {
        if (aperture == null) return;
        float r = Mathf.Max(0.5f, radius > 0f ? radius : aperture.radius);
        int count = 0;
        var hits = Physics.OverlapSphere(aperture.transform.position, r, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (hits[i].GetComponentInParent<BaseAmbulatingActor>() != null ||
                hits[i].GetComponentInParent<TravelAgent>() != null)
                count++;
        }
        // Soft cap: 4 actors ≈ full
        aperture.crowdOccupancy01 = Mathf.Clamp01(count / 4f);
    }

    public static float GetOccupancy01(PathingAperture aperture) =>
        aperture != null ? Mathf.Clamp01(aperture.crowdOccupancy01) : 0f;
}
