using UnityEngine;

/// <summary>Collider trigger → optional BT and/or dialog-tree id for park signage.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Locomotion/Civil/Park/Park Signage Trigger")]
public sealed class ParkSignageTrigger : MonoBehaviour
{
    public string signageId = "park_sign";
    public string dialogTreeId;
    public BehaviorTree behaviorTree;
    public string narrativeActionId = "park_signage_read";
    public bool fireOncePerActor = true;
    public ParkRuntime park;

    readonly System.Collections.Generic.HashSet<int> _fired = new System.Collections.Generic.HashSet<int>();

    void Awake()
    {
        if (park == null)
            park = GetComponentInParent<ParkRuntime>();
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        int id = other.GetInstanceID();
        if (fireOncePerActor && _fired.Contains(id)) return;
        if (fireOncePerActor) _fired.Add(id);

        if (behaviorTree != null)
            behaviorTree.SendMessage("OnParkSignage", signageId, SendMessageOptions.DontRequireReceiver);
        if (!string.IsNullOrEmpty(dialogTreeId))
            SendMessage("OnDialogTree", dialogTreeId, SendMessageOptions.DontRequireReceiver);
        if (!string.IsNullOrEmpty(narrativeActionId))
            SendMessage("OnNarrativeSchedulerAction", narrativeActionId, SendMessageOptions.DontRequireReceiver);
        park?.SendMessage("OnParkSignage", this, SendMessageOptions.DontRequireReceiver);
    }

    public void ResetFired() => _fired.Clear();
}
