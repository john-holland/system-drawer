using UnityEngine;

/// <summary>Lock chamber: ship size, water content, gate constraints.</summary>
[CreateAssetMenu(fileName = "CanalLock", menuName = "Locomotion/Civil/Canal Lock")]
public sealed class CanalLockSpec : ScriptableObject
{
    public Vector3 chamberSizeM = new Vector3(24f, 8f, 80f);
    public float waterContentMinM3 = 2000f;
    public float waterContentMaxM3 = 16000f;
    public float maxShipLengthM = 70f;
    public float maxShipBeamM = 18f;
    public float maxShipDraftM = 5.5f;
    public string jointId = "canal_lock_gate";

    public bool AllowsShip(float lengthM, float beamM, float draftM)
    {
        return lengthM <= maxShipLengthM + 1e-4f
               && beamM <= maxShipBeamM + 1e-4f
               && draftM <= maxShipDraftM + 1e-4f;
    }

    public bool WaterInLimits(float volumeM3) =>
        volumeM3 >= waterContentMinM3 - 1e-3f && volumeM3 <= waterContentMaxM3 + 1e-3f;
}
