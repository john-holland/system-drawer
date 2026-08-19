using Locomotion.Narrative;
using UnityEngine;

/// <summary>Applies {P:chat|op=open} / {P:open-chat} / {P:close-chat} onto StructuredChatRagdoll.</summary>
public static class ChatLemmaResolver
{
    public static bool IsChatLemma(string placeholderName) => ChatLemmaProperties.IsChatLemma(placeholderName);

    public static ChatLemmaProperties Resolve(
        System.Collections.Generic.Dictionary<string, string> parameters,
        string placeholderName = "chat") =>
        ChatLemmaProperties.ResolveFromParams(parameters, placeholderName);

    public static bool Apply(StructuredChatRagdoll ragdoll, ChatLemmaProperties props)
    {
        if (ragdoll == null)
            return false;
        if (!string.IsNullOrEmpty(props.productId))
            ragdoll.productId = props.productId;
        if (!string.IsNullOrEmpty(props.sessionId))
            ragdoll.sessionId = props.sessionId;
        if (!string.IsNullOrEmpty(props.composeMode))
            ragdoll.ApplyLexicon(ragdoll.LexiconWords, props.composeMode);

        bool wantOpen = props.op == ChatLemmaOp.Toggle ? !ragdoll.IsOpen : props.op == ChatLemmaOp.Open;
        if (wantOpen && props.requireEntitlement)
        {
            var snap = ragdoll.Channel != null ? ragdoll.Channel.Entitlement : null;
            if (snap == null || !snap.entitled || !snap.textAllowed)
            {
                ragdoll.LastDenyCode = snap != null && snap.tosSigned
                    ? StructuredChatChannel.DenyEntitlement
                    : StructuredChatChannel.DenyTos;
                return false;
            }
        }

        ragdoll.SetOpen(wantOpen);
        ragdoll.autoCloseOnExit = props.autoCloseOnExit;
        return true;
    }

    public static int ApplyFromPrompt(string prompt, StructuredChatRagdoll ragdoll = null)
    {
        if (string.IsNullOrEmpty(prompt))
            return 0;
        if (ragdoll == null)
            ragdoll = Object.FindAnyObjectByType<StructuredChatRagdoll>();
        if (ragdoll == null)
            return 0;
        var segments = PromptSpanParser.Parse(prompt);
        int applied = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder)
                continue;
            if (!IsChatLemma(seg.placeholderName))
                continue;
            if (Apply(ragdoll, Resolve(seg.placeholderParams, seg.placeholderName)))
                applied++;
        }
        return applied;
    }
}

/// <summary>Applies a serialized lemmaPrompt of chat open/close spans on enable.</summary>
[AddComponentMenu("System Drawer/Networking/Chat Lemma Applier")]
public sealed class ChatLemmaApplier : MonoBehaviour
{
    [TextArea(2, 8)]
    public string lemmaPrompt = "{P:open-chat}";

    public StructuredChatRagdoll ragdoll;
    public bool applyOnEnable = true;

    void OnEnable()
    {
        if (applyOnEnable)
            Apply();
    }

    [ContextMenu("Apply Lemma Prompt")]
    public void Apply()
    {
        if (ragdoll == null)
            ragdoll = GetComponent<StructuredChatRagdoll>()
                      ?? GetComponentInParent<StructuredChatRagdoll>()
                      ?? Object.FindAnyObjectByType<StructuredChatRagdoll>();
        ChatLemmaResolver.ApplyFromPrompt(lemmaPrompt, ragdoll);
    }
}
