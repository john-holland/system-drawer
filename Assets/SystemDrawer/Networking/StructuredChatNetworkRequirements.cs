using System.Collections.Generic;

/// <summary>Canonical SG2D tree for structured multiplayer chat UI.</summary>
public static class StructuredChatNetworkRequirements
{
    public sealed class NodeSpec
    {
        public string label;
        public string eventName;
        public bool isContainer;
        public bool stackSiblingsHorizontally = true;
        public bool enabledByDefault = true;
        public List<NodeSpec> children = new List<NodeSpec>();
    }

    public static NodeSpec BuildCanonicalTree(IList<ChatLexiconWord> words)
    {
        var root = new NodeSpec
        {
            label = "Structured Chat",
            eventName = "chat.root",
            isContainer = true,
            stackSiblingsHorizontally = false
        };

        var bank = new NodeSpec
        {
            label = "Word Bank",
            eventName = "chat.wordBank",
            isContainer = true,
            stackSiblingsHorizontally = true
        };
        if (words != null)
        {
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                if (w == null || string.IsNullOrEmpty(w.id))
                    continue;
                bank.children.Add(new NodeSpec
                {
                    label = string.IsNullOrEmpty(w.text) ? w.id : w.text,
                    eventName = "chat.word." + w.id
                });
            }
        }
        root.children.Add(bank);
        root.children.Add(new NodeSpec { label = "Compose", eventName = "chat.composeBox" });
        root.children.Add(new NodeSpec { label = "History", eventName = "chat.history" });
        root.children.Add(new NodeSpec { label = "Preview", eventName = "chat.preview" });
        root.children.Add(new NodeSpec { label = "Send", eventName = "chat.send" });
        root.children.Add(new NodeSpec { label = "Sent!", eventName = "chat.sentFlash", enabledByDefault = false });
        return root;
    }

    public static HashSet<string> CollectManagedEventNames(NodeSpec spec)
    {
        var set = new HashSet<string>();
        Collect(spec, set);
        return set;
    }

    static void Collect(NodeSpec spec, HashSet<string> set)
    {
        if (spec == null)
            return;
        if (!string.IsNullOrEmpty(spec.eventName))
            set.Add(spec.eventName);
        for (int i = 0; i < spec.children.Count; i++)
            Collect(spec.children[i], set);
    }
}
