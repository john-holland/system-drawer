using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sensor data structure for world sensing.
/// Contains detected objects, contacts, events, and timestamp.
/// </summary>
[System.Serializable]
public class SensorData
{
    [Tooltip("Type of sensor that generated this data")]
    public string sensorType;

    [Tooltip("Objects detected by sensor")]
    public List<GameObject> detectedObjects = new List<GameObject>();

    [Tooltip("Physics contacts")]
    public List<ContactPoint> contacts = new List<ContactPoint>();

    [Tooltip("Events detected")]
    public List<GameEvent> events = new List<GameEvent>();

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
