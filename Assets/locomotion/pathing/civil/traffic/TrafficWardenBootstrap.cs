using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Warden Bootstrap")]
public sealed class TrafficWardenBootstrap : MonoBehaviour
{
    void Awake() => Ensure();

    public void Ensure()
    {
        if (CentralDispatchHub.Instance == null && FindFirstObjectByType<CentralDispatchHub>() == null)
        {
            var hubGo = new GameObject("CentralDispatchHub");
            hubGo.AddComponent<CentralDispatchHub>();
        }

        if (GetComponent<TrafficWarden>() == null)
            gameObject.AddComponent<TrafficWarden>();
        if (GetComponent<TrafficDispatchBioRhythm>() == null)
            gameObject.AddComponent<TrafficDispatchBioRhythm>();

        var warden = GetComponent<TrafficWarden>();
        warden.hub = CentralDispatchHub.Instance ?? FindFirstObjectByType<CentralDispatchHub>();
        warden.RefreshLights();
    }
}
