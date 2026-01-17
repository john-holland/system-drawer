using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World interaction system for sensing world state, generating interaction impulses,
/// handling arbitrary game events, and providing context for behavior trees.
/// </summary>
public class WorldInteraction : MonoBehaviour
{
    [Header("Sensors")]
    [Tooltip("Active sensors for world sensing")]
    public List<Sensor> sensors = new List<Sensor>();

    [Header("Interaction Range")]
    [Tooltip("Range for interactions")]
    public float interactionRange = 10f;

    [Header("Event Handlers")]
    [Tooltip("Custom event handlers")]
    public Dictionary<string, System.Action<GameEvent>> eventHandlers = new Dictionary<string, System.Action<GameEvent>>();

    // References
    private NervousSystem nervousSystem;

    private void Awake()
    {
        nervousSystem = GetComponentInParent<NervousSystem>();

        // Auto-find sensors if not set
        if (sensors == null || sensors.Count == 0)
        {
            sensors = new List<Sensor>(GetComponentsInChildren<Sensor>());
        }
    }

    private void Update()
    {
        // Sense world
        SenseWorld();
    }

    /// <summary>
    /// Collect sensor data from all sensors.
    /// </summary>
    public void SenseWorld()
    {
        foreach (var sensor in sensors)
        {
            if (sensor != null)
            {
                SensorData data = sensor.Detect();
                if (data != null)
                {
                    GenerateImpulse(data);
                }
            }
        }
    }

    /// <summary>
    /// Generate impulse from sensor data.
    /// </summary>
    public void GenerateImpulse(SensorData data)
    {
        if (data == null || nervousSystem == null)
            return;

        // Create sensory impulse
        SensoryData sensoryData = new SensoryData(
            data.detectedObjects.Count > 0 ? data.detectedObjects[0].transform.position : transform.position,
            Vector3.up,
            1f,
            data.detectedObjects.Count > 0 ? data.detectedObjects[0] : null,
            data.sensorType.ToString(),
            data
        );

        ImpulseData impulse = new ImpulseData(
            ImpulseType.Sensory,
            "WorldInteraction",
            "NervousSystem",
            sensoryData
        );

        // Send impulse up through nervous system
        nervousSystem.SendImpulseUp("Spinal", impulse);
    }

    /// <summary>
    /// Handle a game event.
    /// </summary>
    public void HandleGameEvent(GameEvent gameEvent)
    {
        if (gameEvent == null)
            return;

        // Check if we have a handler for this event type
        if (eventHandlers.TryGetValue(gameEvent.eventType, out System.Action<GameEvent> handler))
        {
            handler?.Invoke(gameEvent);
        }
        else
        {
            // Default handling: generate impulse
            GenerateImpulseFromEvent(gameEvent);
        }
    }

    /// <summary>
    /// Get available interaction targets in range.
    /// </summary>
    public List<GameObject> GetInteractionTargets()
    {
        List<GameObject> targets = new List<GameObject>();

        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.gameObject.transform != transform.root)
            {
                targets.Add(collider.gameObject);
            }
        }

        return targets;
    }

    private void GenerateImpulseFromEvent(GameEvent gameEvent)
    {
        if (nervousSystem == null)
            return;

        EventData eventData = new EventData(
            gameEvent.eventType,
            gameEvent.eventSource,
            gameEvent.eventPayload,
            gameEvent.intensity
        );

        ImpulseData impulse = new ImpulseData(
            ImpulseType.Sensory,
            "WorldInteraction",
            "NervousSystem",
            eventData
        );

        nervousSystem.SendImpulseUp("Spinal", impulse);
    }
}

/// <summary>
/// Game event structure for arbitrary game events.
/// </summary>
[System.Serializable]
public class GameEvent
{
    public string eventType;
    public GameObject eventSource;
    public object eventPayload;
    public float intensity = 1f;
}
