using UnityEngine;

/// <summary>Lane occupancy trigger feeding traffic-light ladder contacts.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Lane Sensor Volume")]
public sealed class LaneSensorVolume : MonoBehaviour
{
    public string approachId = "side";
    public bool occupied;
    public int occupantCount;
    public Collider sensorCollider;

    void Awake()
    {
        if (sensorCollider == null)
            sensorCollider = GetComponent<Collider>();
        if (sensorCollider != null)
            sensorCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        occupantCount++;
        occupied = true;
    }

    void OnTriggerExit(Collider other)
    {
        occupantCount = Mathf.Max(0, occupantCount - 1);
        occupied = occupantCount > 0;
    }
}
