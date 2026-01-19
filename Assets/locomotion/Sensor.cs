using System.Collections.Generic;
using UnityEngine;
using Locomotion.Senses;

/// <summary>
/// Individual sensor component for world sensing.
/// Supports different sensor types (Contact, Proximity, Visual, Auditory, Thermal, Smell).
/// </summary>
public class Sensor : MonoBehaviour
{
    [Header("Sensor Properties")]
    [Tooltip("Type of sensor")]
    public SensorType sensorType = SensorType.Proximity;

    [Tooltip("Detection range")]
    public float range = 10f;

    [Tooltip("Detection layer mask")]
    public LayerMask detectionLayer = -1;

    [Header("Visual Settings")]
    [Tooltip("Field of view in degrees for Visual sensors")]
    [Range(1f, 179f)]
    public float fieldOfViewDegrees = 120f;

    [Tooltip("If true, Visual sensors require a clear raycast to the target point")]
    public bool requireLineOfSight = true;

    [Tooltip("Max visual targets to report per frame (0 = no limit)")]
    public int maxVisualDetections = 16;

    [Tooltip("Optional override layer mask for line-of-sight raycasts (0 = use detectionLayer)")]
    public LayerMask lineOfSightMask = 0;

    [Header("Sensor Data")]
    [Tooltip("Current sensor data")]
    public SensorData sensorData = new SensorData();

    private void Update()
    {
        // Update sensor data
        sensorData = Detect();
    }

    /// <summary>
    /// Detect objects/events in sensor range.
    /// </summary>
    public SensorData Detect()
    {
        SensorData data = new SensorData
        {
            sensorType = sensorType.ToString(),
            timestamp = Time.time
        };

        switch (sensorType)
        {
            case SensorType.Contact:
                data = DetectContacts();
                break;

            case SensorType.Proximity:
                data = DetectProximity();
                break;

            case SensorType.Visual:
                data = DetectVisual();
                break;

            case SensorType.Auditory:
                data = DetectAuditory();
                break;

            case SensorType.Thermal:
                data = DetectThermal();
                break;

            case SensorType.Smell:
                data = DetectSmell();
                break;
        }

        return data;
    }

    /// <summary>
    /// Get current sensor data.
    /// </summary>
    public SensorData GetSensorData()
    {
        return sensorData;
    }

    private SensorData DetectContacts()
    {
        SensorData data = new SensorData
        {
            sensorType = SensorType.Contact.ToString(),
            timestamp = Time.time
        };

        // Detect contacts via collisions (would be populated by OnCollisionStay)
        // For now, return empty data
        return data;
    }

    private SensorData DetectProximity()
    {
        SensorData data = new SensorData
        {
            sensorType = SensorType.Proximity.ToString(),
            timestamp = Time.time
        };

        // Detect objects in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, detectionLayer);
        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject)
            {
                data.detectedObjects.Add(collider.gameObject);
            }
        }

        return data;
    }

    private SensorData DetectVisual()
    {
        SensorData data = new SensorData
        {
            sensorType = SensorType.Visual.ToString(),
            timestamp = Time.time
        };

        // Broadphase: sphere overlap
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, detectionLayer);
        if (colliders == null || colliders.Length == 0)
            return data;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        float cosHalfFov = Mathf.Cos((fieldOfViewDegrees * 0.5f) * Mathf.Deg2Rad);

        int losMask = (lineOfSightMask.value != 0) ? lineOfSightMask.value : detectionLayer.value;

        int reported = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
                continue;

            GameObject targetGo = c.gameObject;
            if (targetGo == gameObject)
                continue;

            Vector3 targetPoint = c.bounds.center;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
                continue;

            Vector3 toTargetDir = toTarget / distance;
            float alignment = Vector3.Dot(forward, toTargetDir);
            if (alignment < cosHalfFov)
                continue;

            float angle = Mathf.Acos(Mathf.Clamp(alignment, -1f, 1f)) * Mathf.Rad2Deg;

            bool los = true;
            RaycastHit hit;
            if (requireLineOfSight)
            {
                // Raycast to target center; treat hitting the same collider (or a child collider) as visible.
                los = !Physics.Raycast(origin, toTargetDir, out hit, distance, losMask, QueryTriggerInteraction.Ignore)
                      || hit.collider == c
                      || (hit.collider != null && hit.collider.transform.IsChildOf(c.transform));
            }
            else
            {
                hit = default;
            }

            var vd = new SensorData.VisualDetection
            {
                target = targetGo,
                targetPoint = targetPoint,
                distance = distance,
                angleFromForward = angle,
                hasLineOfSight = los,
                hitCollider = requireLineOfSight ? hit.collider : null,
                hitPoint = requireLineOfSight ? hit.point : Vector3.zero
            };

            data.visualDetections.Add(vd);

            if (los)
            {
                data.detectedObjects.Add(targetGo);
            }

            reported++;
            if (maxVisualDetections > 0 && reported >= maxVisualDetections)
                break;
        }

        return data;
    }

    private SensorData DetectAuditory()
    {
        // Auditory detection (simplified)
        SensorData data = new SensorData
        {
            sensorType = SensorType.Auditory.ToString(),
            timestamp = Time.time
        };

        return data;
    }

    private SensorData DetectThermal()
    {
        // Thermal detection (simplified)
        SensorData data = new SensorData
        {
            sensorType = SensorType.Thermal.ToString(),
            timestamp = Time.time
        };

        return data;
    }

    private SensorData DetectSmell()
    {
        // Delegate to optional smell sensor component so smell settings don't clutter base Sensor fields.
        SmellSensor smell = GetComponent<SmellSensor>();
        if (smell != null)
        {
            return smell.DetectSmell(this);
        }

        return new SensorData
        {
            sensorType = SensorType.Smell.ToString(),
            timestamp = Time.time
        };
    }
}

/// <summary>
/// Types of sensors.
/// </summary>
public enum SensorType
{
    Contact,
    Proximity,
    Visual,
    Auditory,
    Thermal,
    Smell
}
