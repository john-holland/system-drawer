using System.Collections.Generic;
using UnityEngine;

/// <summary>Syncs MenuRagdollNode hierarchy from MainMenuNetworkRequirements spec.</summary>
public static class MainMenuNetworkRequirementsSync
{
    public sealed class SyncResult
    {
        public MenuRagdollNode rootNode;
        public int nodesCreated;
        public int nodesUpdated;
        public int nodesRemoved;
    }

    public static SyncResult Apply(
        Transform menuRoot,
        MenuRagdoll menuRagdoll,
        bool syncNetworkRequirements,
        bool removeOrphansWhenSyncing)
    {
        var result = new SyncResult();
        if (menuRoot == null || !syncNetworkRequirements)
            return result;

        var spec = MainMenuNetworkRequirements.BuildCanonicalTree();
        var managedEvents = MainMenuNetworkRequirements.CollectManagedEventNames(spec);
        var existingByEvent = IndexExistingNodes(menuRoot);

        result.rootNode = SyncNodeRecursive(menuRoot, spec, existingByEvent, menuRagdoll, managedEvents, result, syncNetworkRequirements);

        if (removeOrphansWhenSyncing)
            result.nodesRemoved += RemoveOrphans(menuRoot, managedEvents, result.rootNode);

        WirePasswordField(menuRoot, menuRagdoll);
        RefreshAllLinks(menuRoot);
        return result;
    }

    static Dictionary<string, MenuRagdollNode> IndexExistingNodes(Transform menuRoot)
    {
        var map = new Dictionary<string, MenuRagdollNode>();
        var nodes = menuRoot.GetComponentsInChildren<MenuRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node == null || string.IsNullOrEmpty(node.eventName))
                continue;
            if (!map.ContainsKey(node.eventName))
                map[node.eventName] = node;
        }
        return map;
    }

    static MenuRagdollNode SyncNodeRecursive(
        Transform parent,
        MainMenuNetworkRequirements.NodeSpec spec,
        Dictionary<string, MenuRagdollNode> existingByEvent,
        MenuRagdoll menuRagdoll,
        HashSet<string> managedEvents,
        SyncResult result,
        bool markManaged)
    {
        MenuRagdollNode node = null;
        if (!string.IsNullOrEmpty(spec.eventName) && existingByEvent.TryGetValue(spec.eventName, out node) && node.transform.parent != parent)
        {
            node.transform.SetParent(parent, false);
        }

        if (node == null)
        {
            var go = new GameObject(spec.label);
            go.transform.SetParent(parent, false);
            node = go.AddComponent<MenuRagdollNode>();
            if (!string.IsNullOrEmpty(spec.eventName))
                existingByEvent[spec.eventName] = node;
            result.nodesCreated++;
        }
        else
        {
            node.gameObject.name = spec.label;
            result.nodesUpdated++;
        }

        node.eventName = spec.eventName;
        node.serverModeMask = spec.serverModeMask;
        node.clientRoleMask = spec.clientRoleMask;
        node.isEnabled = spec.enabledByDefault;
        node.managedByNetworkRequirements = markManaged;
        node.ApplyTwoDimensionalDefaults(asSiblingStack: !spec.isContainer && spec.stackSiblingsHorizontally);
        if (spec.isContainer)
            node.placementMode = SGBehaviorTreeNode.PlacementMode.In;

        for (int i = 0; i < spec.children.Count; i++)
            SyncNodeRecursive(node.transform, spec.children[i], existingByEvent, menuRagdoll, managedEvents, result, markManaged);

        node.RefreshChildNodesFromHierarchy();
        return node;
    }

    static int RemoveOrphans(Transform menuRoot, HashSet<string> managedEvents, MenuRagdollNode specRoot)
    {
        int removed = 0;
        var nodes = menuRoot.GetComponentsInChildren<MenuRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node == null || node == specRoot)
                continue;
            if (node.managedByNetworkRequirements && !managedEvents.Contains(node.eventName))
            {
                Object.DestroyImmediate(node.gameObject);
                removed++;
            }
        }
        return removed;
    }

    static void WirePasswordField(Transform menuRoot, MenuRagdoll menuRagdoll)
    {
        MenuRagdollNode joinGroup = null;
        var nodes = menuRoot.GetComponentsInChildren<MenuRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].eventName == "lobby.join.group")
            {
                joinGroup = nodes[i];
                break;
            }
        }
        if (joinGroup == null)
            return;

        var field = joinGroup.GetComponentInChildren<LobbyMenuPasswordField>(true);
        if (field == null)
        {
            var passwordGo = new GameObject("LobbyPasswordField");
            passwordGo.transform.SetParent(joinGroup.transform, false);
            field = passwordGo.AddComponent<LobbyMenuPasswordField>();
        }
        field.menuRagdoll = menuRagdoll;
    }

    static void RefreshAllLinks(Transform menuRoot)
    {
        var nodes = menuRoot.GetComponentsInChildren<MenuRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].RefreshChildNodesFromHierarchy();
            nodes[i].RefreshMenuChildLinks();
        }
    }
}
