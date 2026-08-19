using System;
using System.Collections.Generic;

/// <summary>
/// Built-in game multiplayer text chat. Messages go on the reliable TCP tree stream.
/// Lobby join / send are gated on a chat entitlement snapshot from Continuuuum.
/// Continuuuum editor/web chat is not this product. Voice is a disable slot only.
/// </summary>
public sealed class StructuredChatChannel
{
    public const string ChannelName = "structured-chat";
    public const string TextType = "chat.text";
    public const string DenyEntitlement = "chat_entitlement_required";
    public const string DenyTos = "tos_not_signed";
    public const string DenyJurisdiction = "chat_disabled_jurisdiction";
    public const string DenyWord = "chat_word_not_allowed";
    public const string ComposeDeltaType = "chat.composeDelta";

    public ChatEntitlementSnapshot Entitlement { get; set; }

    public static string EvaluateJoin(string structuredChat, ChatEntitlementSnapshot entitlement)
    {
        string mode = (structuredChat ?? "off").Trim().ToLowerInvariant();
        if (mode == "off")
            return DenyEntitlement;
        if (mode == "required")
            return EvaluateEntitled(entitlement) ? null : (entitlement != null && entitlement.tosSigned ? DenyEntitlement : DenyTos);
        return null;
    }

    public static string EvaluateSend(string structuredChat, string channelKind, ChatEntitlementSnapshot entitlement)
    {
        string mode = (structuredChat ?? "off").Trim().ToLowerInvariant();
        if (mode == "off")
            return DenyEntitlement;
        if (entitlement == null || !entitlement.entitled)
            return entitlement != null && entitlement.tosSigned ? DenyEntitlement : DenyTos;
        bool voice = string.Equals(channelKind, "voice", StringComparison.OrdinalIgnoreCase);
        if (voice)
            return entitlement.voiceAllowed ? null : DenyJurisdiction;
        return entitlement.textAllowed ? null : DenyJurisdiction;
    }

    public bool CanJoinLobby(string structuredChat)
    {
        return EvaluateJoin(structuredChat, Entitlement) == null;
    }

    public static string EvaluateWords(ChatEntitlementSnapshot entitlement, IList<string> tokens, string text = null)
    {
        var check = new List<string>();
        if (tokens != null)
        {
            for (int t = 0; t < tokens.Count; t++)
            {
                if (!string.IsNullOrWhiteSpace(tokens[t]))
                    check.Add(tokens[t].Trim());
            }
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            var parts = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                check.Add(parts[i].Trim('.', ',', '!', '?'));
        }
        if (check.Count == 0)
            return null;
        var words = entitlement != null && entitlement.lexicon != null ? entitlement.lexicon.words : null;
        if (words == null || words.Length == 0)
            return DenyWord;
        for (int t = 0; t < check.Count; t++)
        {
            string tok = check[t];
            bool ok = false;
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w == null)
                    continue;
                if (string.Equals(w.id, tok, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(w.text, tok, StringComparison.OrdinalIgnoreCase))
                {
                    ok = true;
                    break;
                }
            }
            if (!ok)
                return DenyWord;
        }
        return null;
    }

    public bool TrySend(TcpTreeStreamChannel stream, string text, out string denyCode)
    {
        return TrySend(stream, text, null, out denyCode);
    }

    public bool TrySend(TcpTreeStreamChannel stream, string text, IList<string> tokens, out string denyCode)
    {
        denyCode = EvaluateSend(Entitlement != null ? Entitlement.structuredChat : "optional", "text", Entitlement);
        if (denyCode != null)
            return false;
        denyCode = EvaluateWords(Entitlement, tokens, text);
        if (denyCode != null)
            return false;
        if (stream != null)
        {
            string payload = "{\"text\":\"" + Escape(text) + "\"}";
            stream.Send(NetworkMessageEnvelope.Create(ChannelName, TextType, payload));
        }
        return true;
    }

    public bool TrySendVoice(out string denyCode)
    {
        denyCode = EvaluateSend(Entitlement != null ? Entitlement.structuredChat : "optional", "voice", Entitlement);
        if (denyCode == null)
            denyCode = DenyJurisdiction;
        return false;
    }

    static bool EvaluateEntitled(ChatEntitlementSnapshot entitlement)
    {
        return entitlement != null && entitlement.entitled;
    }

    static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
