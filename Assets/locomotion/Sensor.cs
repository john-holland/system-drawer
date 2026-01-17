using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Individual sensor component for world sensing.
/// Supports different sensor types (Contact, Proximity, Visual, Auditory, Thermal).
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
        // Visual detection (simplified)
        return DetectProximity(); // For now, use proximity detection
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
    Thermal
}
