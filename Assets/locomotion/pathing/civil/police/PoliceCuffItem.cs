using UnityEngine;

/// <summary>Inventory cuff tag with assignable key id.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Cuff Item")]
public sealed class PoliceCuffItem : MonoBehaviour
{
    public string cuffId;
    public string keyId;
    public bool locked = true;
    public GameObject restrainedActor;

    void Awake()
    {
        if (string.IsNullOrEmpty(cuffId))
            cuffId = gameObject.name;
        if (string.IsNullOrEmpty(keyId))
            keyId = cuffId + "_key";
    }

    public bool TryUnlock(string inventoryKeyId)
    {
        if (!locked) return true;
        if (string.IsNullOrEmpty(inventoryKeyId) || inventoryKeyId != keyId) return false;
        locked = false;
        return true;
    }

    public void ApplyTo(GameObject actor)
    {
        restrainedActor = actor;
        locked = true;
    }
}
