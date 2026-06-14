using UnityEngine;

/// <summary>
/// Hosts the current <see cref="TravelExecutionContext"/> for activation nodes under a traveling actor.
/// </summary>
[DisallowMultipleComponent]
public class TravelExecutionContextProvider : MonoBehaviour
{
    TravelExecutionContext _current;

    /// <summary>Latest published context (leg or mode transition).</summary>
    public TravelExecutionContext Current => _current;

    /// <summary>Remaining reverse arc-length budget published by travel legs.</summary>
    public float ReverseBudgetRemainingMeters { get; set; }

    /// <summary>True when the active leg is executing the reverse tail.</summary>
    public bool InReverseTail { get; set; }

    public void Publish(TravelExecutionContext ctx) => _current = ctx;

    public void Clear()
    {
        _current = null;
        ReverseBudgetRemainingMeters = 0f;
        InReverseTail = false;
    }

    /// <summary>
    /// Ensure a provider on <paramref name="executionHost"/> (leg/transition hierarchy root).
    /// Prefer the composite node so cloned activation children resolve context via GetComponentInParent.
    /// </summary>
    public static TravelExecutionContextProvider Ensure(GameObject executionHost, BehaviorTree tree, TravelAgent travelAgent)
    {
        GameObject host = executionHost != null ? executionHost
            : tree != null ? tree.gameObject
            : travelAgent != null ? travelAgent.gameObject
            : null;
        if (host == null)
            return null;

        var provider = host.GetComponent<TravelExecutionContextProvider>();
        if (provider == null)
            provider = host.AddComponent<TravelExecutionContextProvider>();
        return provider;
    }
}
