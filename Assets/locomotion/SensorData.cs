using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sensor data structure for world sensing.
/// Contains detected objects, contacts, events, and timestamp.
/// </summary>
[System.Serializable]
public class SensorData
{
    [System.Serializable]
    public class VisualDetection
    {
        public GameObject target;
        public Vector3 targetPoint;
        public float distance;
        public float angleFromForward;
        public bool hasLineOfSight;
        public Collider hitCollider;
        public Vector3 hitPoint;
    }

    [System.Serializable]
    public class SmellDetection
    {
        public GameObject emitter;
        public string signature;
        public float perceivedIntensity;
        public float distance;
        public float downwindAlignment;
        public Vector3 windVector;
    }

    [Tooltip("Type of sensor that generated this data")]
    public string sensorType;

    [Tooltip("Objects detected by sensor")]
    public List<GameObject> detectedObjects = new List<GameObject>();

    [Tooltip("Physics contacts")]
    public List<ContactPoint> contacts = new List<ContactPoint>();

    [Tooltip("Events detected")]
    public List<GameEvent> events = new List<GameEvent>();

    [Header("Extended Sensor Payloads")]
    [Tooltip("Visual detections (if sensorType == Visual)")]
    public List<VisualDetection> visualDetections = new List<VisualDetection>();

    [Tooltip("Smell detections (if sensorType == Smell)")]
    public List<SmellDetection> smellDetections = new List<SmellDetection>();

    [Tooltip("Timestamp when data was collected")]
    public float timestamp;

    /// <summary>
    /// Add a detected object.
    /// </summary>
    public void AddDetectedObject(GameObject obj)
    {
        if (obj != null && !detectedObjects.Contains(obj))
        {
            detectedObjects.Add(obj);
        }
    }

    /// <summary>
    /// Add a contact point.
    /// </summary>
    public void AddContact(ContactPoint contact)
    {
        contacts.Add(contact);
    }

    /// <summary>
    /// Add an event.
    /// </summary>
    public void AddEvent(GameEvent gameEvent)
    {
        if (gameEvent != null)
        {
            events.Add(gameEvent);
        }
    }
}
