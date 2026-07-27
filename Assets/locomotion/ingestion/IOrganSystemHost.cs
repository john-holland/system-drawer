using System;
using UnityEngine;

/// <summary>Weak-hostable organ system for ragdolls and vehicles (cute buck-toothed trucks welcome).</summary>
public interface IOrganSystemHost
{
    GameObject HostObject { get; }
    bool TryGetOrganRuntime<T>(out T runtime) where T : class;
}

/// <summary>Bowel + bladder fill for digest → toilet / free excrete.</summary>
[AddComponentMenu("Locomotion/Ingestion/Bowel Bladder Runtime")]
public sealed class BowelBladderRuntime : MonoBehaviour, IOrganSystemHost
{
    [Range(0f, 1f)] public float bowelFill01;
    [Range(0f, 1f)] public float bladderFill01;
    public bool preferToiletWhenAvailable = true;
    public ToiletStation preferredToilet;

    public GameObject HostObject => gameObject;

    public bool TryGetOrganRuntime<T>(out T runtime) where T : class
    {
        runtime = this as T;
        return runtime != null;
    }

    public void AddBowelFill(float delta) => bowelFill01 = Mathf.Clamp01(bowelFill01 + delta);
    public void AddBladderFill(float delta) => bladderFill01 = Mathf.Clamp01(bladderFill01 + delta);

    public void QueueToiletOrFreeExcrete()
    {
        var bt = GetComponent<BehaviorTree>();
        if (bt == null) return;
        var toilet = preferredToilet != null
            ? preferredToilet
            : FindFirstObjectByType<ToiletStation>();
        if (preferToiletWhenAvailable && toilet != null)
        {
            bt.SetGoal(new BehaviorTreeGoal
            {
                goalName = "use_toilet",
                type = GoalType.Sit,
                target = toilet.gameObject,
                priority = 7
            });
        }
        else
        {
            bt.SetGoal(new BehaviorTreeGoal
            {
                goalName = "free_excrete",
                type = GoalType.Interaction,
                priority = 6
            });
        }
    }

    public static BowelBladderRuntime FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        var existing = host.GetComponent<BowelBladderRuntime>();
        if (existing != null) return existing;
        return host.AddComponent<BowelBladderRuntime>();
    }
}

/// <summary>Weak reference wrapper so vehicles can share organ runtimes without hard ownership.</summary>
[Serializable]
public sealed class WeakOrganHostRef
{
    public Component hostComponent;

    public IOrganSystemHost Resolve()
    {
        if (hostComponent == null) return null;
        if (hostComponent is IOrganSystemHost h) return h;
        return hostComponent.GetComponent<IOrganSystemHost>();
    }
}
