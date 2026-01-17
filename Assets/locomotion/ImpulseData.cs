using UnityEngine;
using System;

/// <summary>
/// Data structures for impulses in the nervous system.
/// Impulses flow bidirectionally: Up (sensory) and Down (motor).
/// </summary>

/// <summary>
/// Impulse type determines direction and purpose.
/// </summary>
public enum ImpulseType
{
    Sensory,    // Up: Sensory data from body parts (contacts, events)
    Motor       // Down: Motor commands to muscles (activations, forces)
}

/// <summary>
/// Main impulse data structure for routing through nervous system.
/// </summary>
[System.Serializable]
public class ImpulseData
{
    [Tooltip("Type of impulse (Sensory or Motor)")]
    public ImpulseType impulseType;

    [Tooltip("Source of the impulse (body part, sensor, brain)")]
    public string source;

    [Tooltip("Target of the impulse (muscle group, brain, etc.)")]
    public string target;

    [Tooltip("Impulse data payload")]
    public object data;

    [Tooltip("Timestamp when impulse was created")]
    public float timestamp;

    [Tooltip("Priority of this impulse (higher = more important)")]
    public int priority;

    /// <summary>
    /// Create a new impulse.
    /// </summary>
    public ImpulseData(ImpulseType type, string source, string target, object data = null, int priority = 0)
    {
        this.impulseType = type;
        this.source = source;
        this.target = target;
        this.data = data;
        this.priority = priority;
        this.timestamp = Time.time;
    }

    /// <summary>
    /// Get typed data from impulse.
    /// </summary>
    public T GetData<T>()
    {
        if (data is T)
        {
            return (T)data;
        }
        return default(T);
    }
}

/// <summary>
/// Sensory impulse data (upward flow).
/// Contains information about world state, contacts, events.
/// </summary>
[System.Serializable]
public class SensoryData
{
    public Vector3 position;
    public Vector3 normal;
    public float force;
    public GameObject contactObject;
    public string sensorType;
    public object sensorPayload;

    public SensoryData(Vector3 position, Vector3 normal, float force, GameObject contactObject, string sensorType, object payload = null)
    {
        this.position = position;
        this.normal = normal;
        this.force = force;
        this.contactObject = contactObject;
        this.sensorType = sensorType;
        this.sensorPayload = payload;
    }
}

/// <summary>
/// Motor impulse data (downward flow).
/// Contains muscle activation commands.
/// </summary>
[System.Serializable]
public class MotorData
{
    public string muscleGroup;
    public float activation;
    public float duration;
    public AnimationCurve curve;
    public Vector3 forceDirection;
    public Vector3 torqueDirection;

    public MotorData(string muscleGroup, float activation, float duration = 0f, AnimationCurve curve = null)
    {
        this.muscleGroup = muscleGroup;
        this.activation = Mathf.Clamp01(activation);
        this.duration = duration;
        this.curve = curve;
        this.forceDirection = Vector3.zero;
        this.torqueDirection = Vector3.zero;
    }

    public MotorData(string muscleGroup, float activation, Vector3 forceDir, Vector3 torqueDir)
    {
        this.muscleGroup = muscleGroup;
        this.activation = Mathf.Clamp01(activation);
        this.duration = 0f;
        this.curve = null;
        this.forceDirection = forceDir;
        this.torqueDirection = torqueDir;
    }
}

/// <summary>
/// Event impulse data for arbitrary game events.
/// </summary>
[System.Serializable]
public class EventData
{
    public string eventType;
    public GameObject eventSource;
    public object eventPayload;
    public float intensity;

    public EventData(string eventType, GameObject eventSource, object payload = null, float intensity = 1f)
    {
        this.eventType = eventType;
        this.eventSource = eventSource;
        this.eventPayload = payload;
        this.intensity = intensity;
    }
}
