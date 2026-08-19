using System;

/// <summary>Snapshot of GET /api/chat/entitlement for Unity structured multiplayer chat.</summary>
[Serializable]
public sealed class ChatEntitlementSnapshot
{
    public bool entitled;
    public string denyCode;
    public bool textAllowed;
    public bool voiceAllowed;
    public bool tosSigned;
    public string structuredChat;
    public string voiceChat;
    public string jurisdiction;
    public string composeMode;
    public ChatLexiconData lexicon;

    public static ChatEntitlementSnapshot Denied(string code)
    {
        return new ChatEntitlementSnapshot
        {
            entitled = false,
            denyCode = code ?? "chat_entitlement_required",
            textAllowed = false,
            voiceAllowed = false
        };
    }
}
