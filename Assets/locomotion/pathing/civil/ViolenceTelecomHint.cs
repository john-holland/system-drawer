using System.Collections.Generic;
using UnityEngine;

/// <summary>Thin reporter: violent-action hints → patrol goal seeds for TravelAgentCard.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Violence Telecom Hint")]
public sealed class ViolenceTelecomHint : MonoBehaviour
{
    public static ViolenceTelecomHint Instance { get; private set; }

    public readonly List<Vector3> recentViolenceHints = new List<Vector3>();
    public int maxHints = 32;

    void Awake() => Instance = this;
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ReportViolentAction(Vector3 worldPos, string causalityLeafId = null)
    {
        recentViolenceHints.Add(worldPos);
        while (recentViolenceHints.Count > maxHints)
            recentViolenceHints.RemoveAt(0);
        // Soft telecom notify if bridge present
        SendMessage("NotifyVisual", $"violence:{causalityLeafId ?? "anon"}@{worldPos}", SendMessageOptions.DontRequireReceiver);
    }

    public TravelAgentCard MakePatrolCardFromLatest()
    {
        if (recentViolenceHints.Count == 0)
            return TravelAgentCard.GeneratePatrol(transform.position);
        return TravelAgentCard.GeneratePatrol(recentViolenceHints[recentViolenceHints.Count - 1]);
    }
}
