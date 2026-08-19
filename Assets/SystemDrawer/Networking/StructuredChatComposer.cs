using System;
using System.Collections.Generic;

/// <summary>Assembles whitelist tokens and decides when to stream a compose BT.</summary>
public sealed class StructuredChatComposer
{
    public const string DenyWord = "chat_word_not_allowed";

    public string ComposeMode = "preview";
    public string TreeId = "chat.compose.local";
    public readonly List<string> Tokens = new List<string>();
    public readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool ShowPreview => string.Equals(ComposeMode, "preview", StringComparison.OrdinalIgnoreCase);
    public bool StreamOnAppend => ShowPreview;
    public string AssembledText => string.Join(" ", Tokens);

    public void SetAllowedWords(IEnumerable<ChatLexiconWord> words)
    {
        Allowed.Clear();
        if (words == null)
            return;
        foreach (var w in words)
        {
            if (w == null)
                continue;
            if (!string.IsNullOrEmpty(w.id))
                Allowed.Add(w.id);
            if (!string.IsNullOrEmpty(w.text))
                Allowed.Add(w.text);
        }
    }

    public bool TryAppend(string wordId, out string denyCode)
    {
        denyCode = null;
        if (string.IsNullOrWhiteSpace(wordId))
        {
            denyCode = DenyWord;
            return false;
        }
        if (Allowed.Count > 0 && !Allowed.Contains(wordId))
        {
            denyCode = DenyWord;
            return false;
        }
        if (Allowed.Count == 0)
        {
            denyCode = DenyWord;
            return false;
        }
        Tokens.Add(wordId);
        return true;
    }

    public ChatComposeDeltaPayload BuildDelta(bool committed)
    {
        return new ChatComposeDeltaPayload
        {
            treeId = TreeId,
            tokens = Tokens.ToArray(),
            text = AssembledText,
            committed = committed
        };
    }

    public void ClearAfterSend()
    {
        Tokens.Clear();
    }
}
