using System.Collections.Generic;
using UnityEngine;

/// <summary>Menu behavior-tree node with mode gating, named events, and 2D spatial placement.</summary>
[AddComponentMenu("System Drawer/Networking/Menu Ragdoll Node")]
public class MenuRagdollNode : SGBehaviorTreeNode2D
{
    public string eventName = "menu.node";
    public MenuServerModeMask serverModeMask = MenuServerModeMask.All;
    public MenuClientRoleMask clientRoleMask = MenuClientRoleMask.All;
    public bool useLocalHangingPhysics;

    [Tooltip("When true and host sync is on, spec-owned fields are read-only in the inspector.")]
    public bool managedByNetworkRequirements;

    MenuRagdollNode _parent;

    public MenuRagdollNode Parent => _parent;

    void Start()
    {
        RefreshChildNodesFromHierarchy();
        RefreshMenuChildLinks();
    }

    public IReadOnlyList<MenuRagdollNode> GetMenuChildren()
    {
        var list = new List<MenuRagdollNode>();
        for (int i = 0; i < childNodes.Count; i++)
        {
            if (childNodes[i] is MenuRagdollNode menuChild)
                list.Add(menuChild);
        }
        return list;
    }

    public void RefreshMenuChildLinks()
    {
        for (int i = 0; i < childNodes.Count; i++)
        {
            if (childNodes[i] is MenuRagdollNode menuChild)
                menuChild._parent = this;
        }
    }

    public bool CanShow()
    {
        if (!isEnabled)
            return false;

        var client = ClientOrchestrator.Instance;
        var mode = client != null ? client.CurrentMode : NetworkServerMode.SinglePlayer;

        if (serverModeMask != MenuServerModeMask.All)
        {
            if (mode == NetworkServerMode.SinglePlayer)
            {
                if ((serverModeMask & MenuServerModeMask.SinglePlayer) == 0)
                    return false;
            }
            else if ((serverModeMask & MenuServerModeMask.Multiplayer) == 0)
                return false;
        }

        if (clientRoleMask == MenuClientRoleMask.SpectatorOnly && client != null && !client.IsSpectator)
            return false;

        if (clientRoleMask == MenuClientRoleMask.PlayerOnly && client != null && client.IsSpectator)
            return false;

        var server = FindAnyObjectByType<ServerOrchestrator>();
        if (eventName == "lobby.spectate.join" && server != null && !server.AllowSpectators)
            return false;

        return true;
    }

    public void Send(string eventName, object payload = null) =>
        BubbleUp(new MenuRagdollEvent(eventName, this, payload));

    public void Broadcast(string eventName, object payload = null) =>
        BroadcastDescend(new MenuRagdollEvent(eventName, this, payload));

    public void BubbleUp(MenuRagdollEvent e)
    {
        if (HandleBubble(e))
            return;
        if (_parent != null)
            _parent.BubbleUp(e);
        else
        {
            var root = GetComponentInParent<MenuRagdollBase>();
            root?.HandleBubble(e);
        }
    }

    public void BroadcastDescend(MenuRagdollEvent e, string nameFilter = null)
    {
        if (!string.IsNullOrEmpty(nameFilter) && e.Name != nameFilter)
            return;
        HandleDescend(e);
        var children = GetMenuChildren();
        for (int i = 0; i < children.Count; i++)
            children[i]?.BroadcastDescend(e, nameFilter);
    }

    protected virtual bool HandleBubble(MenuRagdollEvent e) => false;

    protected virtual void HandleDescend(MenuRagdollEvent e)
    {
        if (e.Name == "menu.refresh")
            isEnabled = CanShow();
    }

    public void ApplySelectionImpulse()
    {
        var rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
            return;
        var baseHost = GetComponentInParent<MenuRagdollBase>();
        float impulse = baseHost != null ? baseHost.selectionImpulse : 0.5f;
        rb.AddForce(Vector2.right * impulse, ForceMode2D.Impulse);
    }
}
