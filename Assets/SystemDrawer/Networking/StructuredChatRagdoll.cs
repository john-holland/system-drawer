using UnityEngine;

/// <summary>SG2D structured chat host: word bank, compose box, history, send flash.</summary>
[AddComponentMenu("System Drawer/Networking/Structured Chat Ragdoll")]
public sealed class StructuredChatRagdoll : MenuRagdollBase
{
    public string productId = "default";
    public string sessionId = "default";
    public string userId = "local";
    public ChatLexiconWord[] LexiconWords;
    public StructuredChatChannel Channel = new StructuredChatChannel();
    public readonly StructuredChatComposer Composer = new StructuredChatComposer();
    public ChatComposeDeltaPayload LastStreamed;
    public bool SentFlashVisible;
    public string LastDenyCode;
    public float sentFlashSeconds = 1f;
    public bool IsOpen = true;
    public bool autoCloseOnExit;

    float _flashUntil;

    protected override void Awake()
    {
        base.Awake();
        SystemDrawerService.FindInScene()?.Register(SystemDrawerServiceKeys.StructuredChatRagdoll, this);
        ApplyLexicon(LexiconWords, Composer.ComposeMode);
        RegisterComposeTree();
        RefreshLabels();
    }

    protected override void OnDestroy()
    {
        SystemDrawerService.FindInScene()?.Unregister(SystemDrawerServiceKeys.StructuredChatRagdoll);
        base.OnDestroy();
    }

    void Update()
    {
        if (SentFlashVisible && Time.unscaledTime >= _flashUntil)
        {
            SentFlashVisible = false;
            SetNodeEnabled("chat.sentFlash", false);
        }
    }

    public void ApplyLexicon(ChatLexiconWord[] words, string composeMode)
    {
        LexiconWords = words;
        Composer.ComposeMode = string.IsNullOrEmpty(composeMode) ? "preview" : composeMode;
        Composer.TreeId = "chat.compose." + (userId ?? "local");
        Composer.SetAllowedWords(words);
        if (Channel.Entitlement != null)
        {
            Channel.Entitlement.composeMode = Composer.ComposeMode;
            Channel.Entitlement.lexicon = new ChatLexiconData { words = words };
        }
    }

    public override bool HandleBubble(MenuRagdollEvent e)
    {
        if (e.Name == "chat.open")
        {
            SetOpen(true);
            return true;
        }
        if (e.Name == "chat.close")
        {
            SetOpen(false);
            return true;
        }
        if (!IsOpen)
            return false;
        if (e.Name == "chat.word")
        {
            AppendWord(e.Payload as string);
            return true;
        }
        if (e.Name != null && e.Name.StartsWith("chat.word."))
        {
            AppendWord(e.Name.Substring("chat.word.".Length));
            return true;
        }
        if (e.Name == "chat.send")
        {
            Commit();
            return true;
        }
        return base.HandleBubble(e);
    }

    public bool AppendWord(string wordId)
    {
        LastDenyCode = null;
        if (!Composer.TryAppend(wordId, out string deny))
        {
            LastDenyCode = deny;
            return false;
        }
        RefreshLabels();
        if (Composer.StreamOnAppend)
            StreamDelta(false);
        return true;
    }

    public bool Commit()
    {
        LastDenyCode = null;
        var stream = Client != null ? Client.Tcp : null;
        if (!Channel.TrySend(stream, Composer.AssembledText, Composer.Tokens, out string deny))
        {
            LastDenyCode = deny;
            return false;
        }
        StructuredChatSessionHistory.Append(productId, sessionId, new StructuredChatSessionHistory.Entry
        {
            userId = userId,
            tokens = Composer.Tokens.ToArray(),
            text = Composer.AssembledText,
            direction = "out"
        });
        StreamDelta(true);
        Composer.ClearAfterSend();
        FlashSent();
        RefreshLabels();
        return true;
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        var root = FindNode("chat.root");
        if (root != null)
            root.isEnabled = open;
    }

    public void OnRemoteCommitted(string text, string[] tokens, string fromUser)
    {
        StructuredChatSessionHistory.Append(productId, sessionId, new StructuredChatSessionHistory.Entry
        {
            userId = fromUser,
            tokens = tokens,
            text = text,
            direction = "in"
        });
        RefreshLabels();
    }

    public void FlashSent()
    {
        SentFlashVisible = true;
        _flashUntil = Time.unscaledTime + sentFlashSeconds;
        SetNodeEnabled("chat.sentFlash", true);
        var flash = FindNode("chat.sentFlash");
        flash?.SetLabel("Sent!");
    }

    void StreamDelta(bool committed)
    {
        LastStreamed = Composer.BuildDelta(committed);
        string json = UnityEngine.JsonUtility.ToJson(LastStreamed);
        Client?.Tcp?.Send(NetworkMessageEnvelope.Create("TreeStreamChannel", "chat.composeDelta", json));
    }

    void RegisterComposeTree()
    {
        var server = Server;
        if (server == null)
            return;
        server.TreeRegistry.Register(new NetworkTreeDescriptor
        {
            TreeId = Composer.TreeId,
            Dimension = TreeDimension.Spatial2D,
            TransmitPolicy = TreeTransmitPolicy.PeerTransferable,
            CausalityLeafPrefix = "chat",
            StreamForOwnership = true
        });
    }

    void RefreshLabels()
    {
        FindNode("chat.composeBox")?.SetLabel(Composer.AssembledText);
        var preview = FindNode("chat.preview");
        if (preview != null)
        {
            preview.isEnabled = Composer.ShowPreview;
            preview.SetLabel(Composer.AssembledText);
        }
        var history = FindNode("chat.history");
        if (history != null)
        {
            var entries = StructuredChatSessionHistory.Load(productId, sessionId);
            history.SetLabel(entries.Count == 0 ? "History" : entries[entries.Count - 1].text);
        }
    }

    StructuredChatRagdollNode FindNode(string eventName)
    {
        var nodes = GetComponentsInChildren<StructuredChatRagdollNode>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].eventName == eventName)
                return nodes[i];
        }
        return null;
    }

    void SetNodeEnabled(string eventName, bool enabled)
    {
        var node = FindNode(eventName);
        if (node != null)
            node.isEnabled = enabled;
    }
}
