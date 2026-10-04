using System;
using System.Collections.Generic;

/// <summary>Property keys for {P:chat|op=open} / {P:open-chat} / {P:close-chat} spans.</summary>
public static class ChatLemmaPropertyKeys
{
    public const string Op = "chat-op";
    public const string AliasOp = "op";
    public const string AliasAction = "action";
    public const string ProductId = "product-id";
    public const string AliasProduct = "product";
    public const string SessionId = "session-id";
    public const string AliasSession = "session";
    public const string ComposeMode = "compose-mode";
    public const string Surface = "chat-surface";
    public const string AliasSurface = "surface";
    public const string AutoCloseOnExit = "auto-close-on-exit";
    public const string RequireEntitlement = "require-entitlement";

    public static readonly string[] LemmaPlaceholders =
    {
        "chat", "open-chat", "close-chat", "dismiss"
    };

    public static readonly string[] AllKeys =
    {
        Op, ProductId, SessionId, ComposeMode, Surface, AutoCloseOnExit, RequireEntitlement
    };
}

public enum ChatLemmaOp
{
    Open,
    Close,
    Toggle
}

[Serializable]
public struct ChatLemmaProperties
{
    public ChatLemmaOp op;
    public string productId;
    public string sessionId;
    public string composeMode;
    public string surface;
    public bool autoCloseOnExit;
    public bool requireEntitlement;
    public string lemmaHint;

    public static ChatLemmaProperties Defaults => new ChatLemmaProperties
    {
        op = ChatLemmaOp.Open,
        productId = "",
        sessionId = "",
        composeMode = "",
        surface = "unity-mp-text",
        autoCloseOnExit = false,
        requireEntitlement = true,
        lemmaHint = "chat"
    };

    public static bool IsChatLemma(string placeholderName)
    {
        if (string.IsNullOrEmpty(placeholderName))
            return false;
        string n = NormalizeName(placeholderName);
        for (int i = 0; i < ChatLemmaPropertyKeys.LemmaPlaceholders.Length; i++)
        {
            if (n == ChatLemmaPropertyKeys.LemmaPlaceholders[i])
                return true;
        }
        return false;
    }

    public static ChatLemmaProperties ResolveFromParams(
        Dictionary<string, string> parameters,
        string placeholderName = "chat")
    {
        var p = Defaults;
        p.lemmaHint = placeholderName ?? "chat";
        p.op = InferOp(placeholderName);
        if (parameters == null)
            return p;

        if (Try(parameters, ChatLemmaPropertyKeys.Op, out var op) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasOp, out op) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasAction, out op))
            p.op = ParseOp(op, p.op);

        if (Try(parameters, ChatLemmaPropertyKeys.ProductId, out var pid) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasProduct, out pid))
            p.productId = pid;

        if (Try(parameters, ChatLemmaPropertyKeys.SessionId, out var sid) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasSession, out sid))
            p.sessionId = sid;

        if (Try(parameters, ChatLemmaPropertyKeys.ComposeMode, out var mode))
            p.composeMode = mode;

        if (Try(parameters, ChatLemmaPropertyKeys.Surface, out var surface) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasSurface, out surface))
            p.surface = surface;

        if (Try(parameters, ChatLemmaPropertyKeys.AutoCloseOnExit, out var autoClose))
            p.autoCloseOnExit = ParseBool(autoClose);

        if (Try(parameters, ChatLemmaPropertyKeys.RequireEntitlement, out var req))
            p.requireEntitlement = ParseBool(req);

        return p;
    }

    public static ChatLemmaOp InferOp(string placeholderName)
    {
        string n = NormalizeName(placeholderName);
        if (n == "close-chat" || n == "dismiss")
            return ChatLemmaOp.Close;
        if (n == "open-chat")
            return ChatLemmaOp.Open;
        return ChatLemmaOp.Open;
    }

    // todo: review: add send, and refine open to use join as a separate lemma
    public static ChatLemmaOp ParseOp(string raw, ChatLemmaOp fallback = ChatLemmaOp.Open)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        string n = NormalizeName(raw);
        switch (n)
        {
            case "close":
            case "dismiss":
            case "leave":
            case "hang-up":
                return ChatLemmaOp.Close;
            case "toggle":
            case "flip":
                return ChatLemmaOp.Toggle;
            case "open":
            case "join":
            case "show":
                return ChatLemmaOp.Open;
            default:
                return fallback;
        }
    }

    static string NormalizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        return raw.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
    }

    static bool ParseBool(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return raw == "1" ||
               string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
    }

    static bool Try(Dictionary<string, string> p, string key, out string v)
    {
        v = null;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                v = kv.Value;
                return !string.IsNullOrEmpty(v);
            }
        }
        return false;
    }
}
