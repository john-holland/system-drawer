using UnityEngine;

/// <summary>
/// Merges the default get-up BehaviorTree onto a ragdoll <see cref="Brain"/> at instantiation.
/// </summary>
public static class RagdollGetUpBootstrap
{
    /// <summary>
    /// When <see cref="RagdollActor.enableGetUp"/> is true, ensure Brain has a get-up Selector
    /// (assigned alone, or wrapping an existing root). Merge runs at most once per actor.
    /// </summary>
    public static bool TryMerge(RagdollActor actor)
    {
        if (actor == null || !actor.enableGetUp)
            return false;

        if (actor.GetUpMerged)
            return false;

        Brain brain = actor.GetComponentInChildren<Brain>(true);
        if (brain == null)
            return false;

        // Already a get-up selector at the active tree root — treat as merged.
        if (brain.behaviorTree != null
            && brain.behaviorTree.rootNode is RagdollGetUpSelectorNode)
        {
            actor.MarkGetUpMerged();
            return false;
        }

        BehaviorTree previous = brain.behaviorTree;
        BehaviorTreeNode previousRoot = previous != null ? previous.rootNode : null;

        BehaviorTree template = actor.getUpBehaviorTreePrefab;
        BehaviorTree newTree;

        if (template != null)
        {
            Transform parent = brain.transform;
            GameObject go = Object.Instantiate(template.gameObject, parent);
            go.name = RagdollGetUpTreeFactory.DefaultTreeName + "(Instance)";
            newTree = go.GetComponent<BehaviorTree>();
            if (newTree == null)
            {
                Object.Destroy(go);
                return false;
            }
        }
        else
        {
            template = Resources.Load<BehaviorTree>(RagdollGetUpTreeFactory.ResourcesLoadName);
            if (template != null)
            {
                Transform parent = brain.transform;
                GameObject go = Object.Instantiate(template.gameObject, parent);
                go.name = RagdollGetUpTreeFactory.DefaultTreeName + "(Instance)";
                newTree = go.GetComponent<BehaviorTree>();
                if (newTree == null)
                {
                    Object.Destroy(go);
                    return false;
                }
            }
            else
            {
                newTree = RagdollGetUpTreeFactory.Build(brain.transform);
                newTree.gameObject.name = RagdollGetUpTreeFactory.DefaultTreeName + "(Instance)";
            }
        }

        var selector = newTree.rootNode as RagdollGetUpSelectorNode;
        if (selector == null)
            selector = newTree.GetComponentInChildren<RagdollGetUpSelectorNode>(true);

        if (selector != null && previousRoot != null && previousRoot != selector)
            selector.SetPassthrough(previousRoot);

        brain.behaviorTree = newTree;
        actor.MarkGetUpMerged();
        return true;
    }
}
