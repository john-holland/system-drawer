using UnityEngine;

/// <summary>Police inventory spike trap — deploy stub as road hazard.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Spike Trap Item")]
public sealed class SpikeTrapItem : MonoBehaviour
{
    public string itemId = "spike_trap";
    public bool deployed;
    public Vector3 deployWorld;
    public float hazardRadiusM = 3f;

    public void Deploy(Vector3 worldPos)
    {
        deployed = true;
        deployWorld = worldPos;
        transform.position = worldPos;
        SendMessage("OnSpikeTrapDeployed", this, SendMessageOptions.DontRequireReceiver);
        // Soft road-hazard broadcast for TravelAgents nearby.
        var agents = FindObjectsByType<TravelAgent>(FindObjectsSortMode.None);
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] == null) continue;
            if ((agents[i].transform.position - worldPos).sqrMagnitude > hazardRadiusM * hazardRadiusM)
                continue;
            agents[i].SendMessage("OnRoadHazard", this, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void PackUp()
    {
        deployed = false;
    }
}
