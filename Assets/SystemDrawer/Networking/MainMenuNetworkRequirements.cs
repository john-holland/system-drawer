using System.Collections.Generic;

/// <summary>Canonical main-menu node tree for networking features.</summary>
public static class MainMenuNetworkRequirements
{
    public sealed class NodeSpec
    {
        public string label;
        public string eventName;
        public MenuServerModeMask serverModeMask = MenuServerModeMask.All;
        public MenuClientRoleMask clientRoleMask = MenuClientRoleMask.All;
        public bool enabledByDefault = true;
        public bool isContainer;
        public bool stackSiblingsHorizontally = true;
        public List<NodeSpec> children = new List<NodeSpec>();
    }

    public static NodeSpec BuildCanonicalTree()
    {
        var root = new NodeSpec
        {
            label = "Main Menu",
            eventName = "menu.root",
            isContainer = true,
            stackSiblingsHorizontally = false
        };

        root.children.Add(new NodeSpec { label = "Start", eventName = "start" });
        root.children.Add(BuildMultiplayerBranch());
        root.children.Add(new NodeSpec { label = "Settings", eventName = "settings" });

        root.children.Add(new NodeSpec
        {
            label = "Save",
            eventName = "save",
            enabledByDefault = false
        });
        root.children.Add(new NodeSpec
        {
            label = "Load",
            eventName = "load",
            enabledByDefault = false
        });

        return root;
    }

    static NodeSpec BuildMultiplayerBranch()
    {
        var multiplayer = new NodeSpec
        {
            label = "Multiplayer",
            eventName = "multiplayer",
            serverModeMask = MenuServerModeMask.All
        };

        var lobby = new NodeSpec
        {
            label = "Lobby",
            eventName = "lobby",
            serverModeMask = MenuServerModeMask.Multiplayer,
            isContainer = true
        };

        var host = new NodeSpec
        {
            label = "Host",
            eventName = "lobby.host",
            serverModeMask = MenuServerModeMask.Multiplayer,
            isContainer = true
        };
        host.children.Add(new NodeSpec { label = "Host Start", eventName = "lobby.host.start", serverModeMask = MenuServerModeMask.Multiplayer });
        host.children.Add(new NodeSpec { label = "Host Stop", eventName = "lobby.host.stop", serverModeMask = MenuServerModeMask.Multiplayer });
        host.children.Add(new NodeSpec { label = "Set Password", eventName = "lobby.host.password", serverModeMask = MenuServerModeMask.Multiplayer });

        var join = new NodeSpec
        {
            label = "Join",
            eventName = "lobby.join.group",
            serverModeMask = MenuServerModeMask.Multiplayer,
            isContainer = true
        };
        join.children.Add(new NodeSpec { label = "Set Password", eventName = "lobby.join.password", serverModeMask = MenuServerModeMask.Multiplayer });
        join.children.Add(new NodeSpec { label = "Join Game", eventName = "lobby.join", serverModeMask = MenuServerModeMask.Multiplayer });
        join.children.Add(new NodeSpec
        {
            label = "Spectate",
            eventName = "lobby.spectate.join",
            serverModeMask = MenuServerModeMask.Multiplayer,
            clientRoleMask = MenuClientRoleMask.SpectatorOnly
        });

        lobby.children.Add(host);
        lobby.children.Add(join);
        lobby.children.Add(new NodeSpec { label = "Start Game", eventName = "lobby.game.start", serverModeMask = MenuServerModeMask.Multiplayer });
        lobby.children.Add(new NodeSpec
        {
            label = "End Game",
            eventName = "lobby.game.end",
            serverModeMask = MenuServerModeMask.Multiplayer,
            enabledByDefault = false
        });

        multiplayer.children.Add(lobby);
        return multiplayer;
    }

    public static HashSet<string> CollectManagedEventNames(NodeSpec root)
    {
        var set = new HashSet<string>();
        CollectEventNames(root, set);
        return set;
    }

    static void CollectEventNames(NodeSpec node, HashSet<string> set)
    {
        if (node == null)
            return;
        if (!string.IsNullOrEmpty(node.eventName))
            set.Add(node.eventName);
        for (int i = 0; i < node.children.Count; i++)
            CollectEventNames(node.children[i], set);
    }
}
