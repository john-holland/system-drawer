using UnityEngine;

/// <summary>
/// Holds active eat animation group tag for IK training / ABT selection (Bite/Chew/Swallow categories).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Ingestion/Eating Animation Driver")]
public sealed class EatingAnimationDriver : MonoBehaviour
{
    public string activeAnimationGroupTag;
    public PhysicsIKTrainingCategory activeCategory = PhysicsIKTrainingCategory.Chew;

    public void PlayTag(string animationGroupTag, float durationSeconds = 1f)
    {
        activeAnimationGroupTag = animationGroupTag ?? "";
        activeCategory = CategoryForTag(activeAnimationGroupTag);
        _until = Time.time + Mathf.Max(0.05f, durationSeconds);
    }

    float _until;

    void Update()
    {
        if (_until > 0f && Time.time >= _until)
        {
            activeAnimationGroupTag = null;
            _until = 0f;
        }
    }

    public static PhysicsIKTrainingCategory CategoryForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return PhysicsIKTrainingCategory.Chew;
        string t = tag.ToLowerInvariant();
        if (t.Contains("bite")) return PhysicsIKTrainingCategory.Bite;
        if (t.Contains("swallow")) return PhysicsIKTrainingCategory.Swallow;
        return PhysicsIKTrainingCategory.Chew;
    }

    public static EatingAnimationDriver FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        return host.GetComponent<EatingAnimationDriver>() ?? host.AddComponent<EatingAnimationDriver>();
    }
}
