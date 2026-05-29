using UnityEngine;

/// <summary>
/// Base for travel mode activation nodes that read <see cref="TravelExecutionContextProvider.Current"/>.
/// </summary>
public abstract class TravelContextBehaviorTreeNode : BehaviorTreeNode
{
    protected TravelExecutionContext Ctx
    {
        get
        {
            var provider = GetComponentInParent<TravelExecutionContextProvider>();
            return provider != null ? provider.Current : null;
        }
    }
}
