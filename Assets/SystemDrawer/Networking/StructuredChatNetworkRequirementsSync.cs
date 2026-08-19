using System.Collections.Generic;
using UnityEngine;

/// <summary>Syncs StructuredChatRagdollNode hierarchy from lexicon-driven spec.</summary>
public static class StructuredChatNetworkRequirementsSync
{
    public sealed class SyncResult
    {
        public StructuredChatRagdollNode rootNode;
        public int nodesCreated;
        public int nodesUpdated;
        public int nodesRemoved;
    }

    public static SyncResult Apply(Transform chatRoot, IList<ChatLexiconWord> words, bool removeOrphans)
    {
        var result = new SyncResult();
        if (chatRoot == null)
            return result;
        var spec = StructuredChatNetworkRequirements.BuildCanonicalTree(words);
        var managed = StructuredChatNetworkRequirements.CollectManagedEventNames(spec);
        var existing = IndexExisting(chatRoot);
        result.rootNode = SyncRecursive(chatRoot, spec, existing, result);
        if (removeOrphans)
            result.nodesRemoved += RemoveOrphans(chatRoot, managed, result.rootNode);
        RefreshLinks(chatRoot);
        return result;
    }

    static Dictionary<string, StructuredChatRagdollNode> IndexExisting(Transform root)
    {
        var map = new Dictionary<string, StructuredChatRagdollNode>();
        var nodes = root.GetComponentsInChildren<StructuredChatRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            if (n == null || string.IsNullOrEmpty(n.eventName))
                continue;
            if (!map.ContainsKey(n.eventName))
                map[n.eventName] = n;
        }
        return map;
    }

    static StructuredChatRagdollNode SyncRecursive(
        Transform parent,
        StructuredChatNetworkRequirements.NodeSpec spec,
        Dictionary<string, StructuredChatRagdollNode> existing,
        SyncResult result)
    {
        StructuredChatRagdollNode node = null;
        if (!string.IsNullOrEmpty(spec.eventName) && existing.TryGetValue(spec.eventName, out node) && node.transform.parent != parent)
            node.transform.SetParent(parent, false);

        if (node == null)
        {
            var go = new GameObject(spec.label);
            go.transform.SetParent(parent, false);
            node = go.AddComponent<StructuredChatRagdollNode>();
            if (!string.IsNullOrEmpty(spec.eventName))
                existing[spec.eventName] = node;
            result.nodesCreated++;
        }
        else
        {
            node.gameObject.name = spec.label;
            result.nodesUpdated++;
        }

        node.eventName = spec.eventName;
        node.isEnabled = spec.enabledByDefault;
        node.managedByNetworkRequirements = true;
        node.ApplyTwoDimensionalDefaults(asSiblingStack: !spec.isContainer && spec.stackSiblingsHorizontally);
        if (spec.isContainer)
            node.placementMode = SGBehaviorTreeNode.PlacementMode.In;

        if (spec.eventName != null && spec.eventName.StartsWith("chat.word."))
        {
            var click = node.GetComponent<StructuredChatNodeClick>();
            if (click == null)
                click = node.gameObject.AddComponent<StructuredChatNodeClick>();
            click.eventName = "chat.word";
            click.wordId = spec.eventName.Substring("chat.word.".Length);
        }
        else if (spec.eventName == "chat.send")
        {
            var click = node.GetComponent<StructuredChatNodeClick>();
            if (click == null)
                click = node.gameObject.AddComponent<StructuredChatNodeClick>();
            click.eventName = "chat.send";
            click.wordId = null;
        }

        for (int i = 0; i < spec.children.Count; i++)
            SyncRecursive(node.transform, spec.children[i], existing, result);

        node.RefreshChildNodesFromHierarchy();
        return node;
    }

    static int RemoveOrphans(Transform root, HashSet<string> managed, StructuredChatRagdollNode specRoot)
    {
        int removed = 0;
        var nodes = root.GetComponentsInChildren<StructuredChatRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node == null || node == specRoot)
                continue;
            if (node.managedByNetworkRequirements && !managed.Contains(node.eventName))
            {
                Object.DestroyImmediate(node.gameObject);
                removed++;
            }
        }
        return removed;
    }

    static void RefreshLinks(Transform root)
    {
        var nodes = root.GetComponentsInChildren<StructuredChatRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].RefreshChildNodesFromHierarchy();
            nodes[i].RefreshMenuChildLinks();
        }
    }
}
