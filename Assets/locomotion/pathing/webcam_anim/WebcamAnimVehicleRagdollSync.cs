using Locomotion.Rig;
using UnityEngine;

/// <summary>Applies recording pose + projected chassis pose onto the vehicle ragdoll at a playhead.</summary>
public static class WebcamAnimVehicleRagdollSync
{
    public static int Apply(
        WebcamAnimRecordingAsset asset,
        float timeMs,
        VehicleActor vehicle,
        Transform occupant,
        BoneMap map,
        VehicleProjectionResult projection)
    {
        int n = 0;
        if (asset != null && asset.lastTrack != null && map != null)
            n += PoseTrackPlayer.Apply(asset.lastTrack, map, timeMs);

        if (vehicle != null && VehicleTrackProjector.TrySample(projection, timeMs, out var wp))
        {
            vehicle.transform.position = wp.world;
            if (wp.tangent.sqrMagnitude > 1e-6f)
                vehicle.transform.rotation = Quaternion.LookRotation(wp.tangent, Vector3.up);
            n++;
        }

        if (occupant != null && vehicle != null)
        {
            var seating = vehicle.GetComponent<VehicleSeating>() ?? vehicle.GetComponentInChildren<VehicleSeating>();
            if (seating != null && seating.occupantAnchors != null && seating.occupantAnchors.Length > 0 && seating.occupantAnchors[0] != null)
            {
                occupant.SetPositionAndRotation(seating.occupantAnchors[0].position, seating.occupantAnchors[0].rotation);
                n++;
            }
        }

        var ragdoll = occupant != null
            ? occupant.GetComponentInParent<RagdollSystem>() ?? occupant.GetComponentInChildren<RagdollSystem>()
            : null;
        if (ragdoll != null)
            RagdollPoseUtility.ZeroRagdollVelocities(ragdoll);
        return n;
    }
}
