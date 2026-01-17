using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Organizes related muscles into groups for coordinated activation.
/// Allows simultaneous activation of multiple muscles for complex movements.
/// </summary>
public class MuscleGroup : MonoBehaviour
{
    [Header("Group Properties")]
    [Tooltip("Name of this muscle group (e.g., 'LeftArm', 'Legs')")]
    public string groupName = "MuscleGroup";

    [Tooltip("Execution priority (higher = more important)")]
    public int priority = 0;

    [Header("Muscles")]
    [Tooltip("Muscles in this group")]
    public List<Muscle> muscles = new List<Muscle>();

    [Header("Activation")]
    [Tooltip("Current group activation level (0-1)")]
    [Range(0f, 1f)]
    public float groupActivation = 0f;

    // Internal state
    private Dictionary<string, Muscle> muscleDict = new Dictionary<string, Muscle>();

    private void Awake()
    {
        // Auto-find muscles if not set
        if (muscles == null || muscles.Count == 0)
        {
            muscles = GetComponentsInChildren<Muscle>().ToList();
        }

        // Build muscle dictionary
        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                string muscleName = muscle.name;
                muscleDict[muscleName] = muscle;
            }
        }
    }

    /// <summary>
    /// Activate all muscles in this group with specified strength (0-1).
    /// </summary>
    public void ActivateGroup(float strength)
    {
        groupActivation = Mathf.Clamp01(strength);

        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                muscle.Activate(groupActivation);
            }
        }
    }

    /// <summary>
    /// Activate a specific muscle in this group by name.
    /// </summary>
    public void ActivateMuscle(string muscleName, float strength)
    {
        if (muscleDict.TryGetValue(muscleName, out Muscle muscle))
        {
            muscle.Activate(Mathf.Clamp01(strength));
        }
        else
        {
            Debug.LogWarning($"Muscle '{muscleName}' not found in group '{groupName}'");
        }
    }

    /// <summary>
    /// Get current group activation level (average of all muscles).
    /// </summary>
    public float GetGroupActivation()
    {
        if (muscles == null || muscles.Count == 0)
            return 0f;

        float totalActivation = 0f;
        int activeMuscles = 0;

        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                totalActivation += muscle.activation;
                activeMuscles++;
            }
        }

        return activeMuscles > 0 ? totalActivation / activeMuscles : 0f;
    }

    /// <summary>
    /// Get all muscle activations as a dictionary (muscle name -> activation).
    /// </summary>
    public Dictionary<string, float> GetMuscleActivations()
    {
        Dictionary<string, float> activations = new Dictionary<string, float>();

        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                activations[muscle.name] = muscle.activation;
            }
        }

        return activations;
    }

    /// <summary>
    /// Get all joint states from muscles in this group.
    /// </summary>
    public Dictionary<string, JointState> GetJointStates()
    {
        Dictionary<string, JointState> jointStates = new Dictionary<string, JointState>();

        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                jointStates[muscle.name] = muscle.GetJointState();
            }
        }

        return jointStates;
    }

    /// <summary>
    /// Deactivate all muscles in this group.
    /// </summary>
    public void DeactivateGroup()
    {
        groupActivation = 0f;

        foreach (var muscle in muscles)
        {
            if (muscle != null)
            {
                muscle.Deactivate();
            }
        }
    }

    /// <summary>
    /// Check if any muscles in this group are active.
    /// </summary>
    public bool IsActive(float threshold = 0.01f)
    {
        foreach (var muscle in muscles)
        {
            if (muscle != null && muscle.IsActive(threshold))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get a muscle by name from this group.
    /// </summary>
    public Muscle GetMuscle(string muscleName)
    {
        muscleDict.TryGetValue(muscleName, out Muscle muscle);
        return muscle;
    }

    /// <summary>
    /// Add a muscle to this group.
    /// </summary>
    public void AddMuscle(Muscle muscle)
    {
        if (muscle != null && !muscles.Contains(muscle))
        {
            muscles.Add(muscle);
            muscleDict[muscle.name] = muscle;
        }
    }

    /// <summary>
    /// Remove a muscle from this group.
    /// </summary>
    public void RemoveMuscle(Muscle muscle)
    {
        if (muscle != null && muscles.Contains(muscle))
        {
            muscles.Remove(muscle);
            muscleDict.Remove(muscle.name);
        }
    }
}
