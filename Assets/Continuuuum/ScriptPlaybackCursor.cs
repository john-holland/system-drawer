using System;

/// <summary>Play-mode script position published from runtime for the Script Karaoke editor window.</summary>
public static class ScriptPlaybackCursor
{
    public static string ScriptText { get; private set; } = "";
    public static string ActivePhrase { get; private set; } = "";
    public static int ActiveEventIndex { get; private set; } = -1;
    public static int WordIndex { get; private set; }
    public static int WordCount { get; private set; }
    public static bool IsLive { get; private set; }

    public static void Update(string scriptText, string activePhrase, int activeEventIndex)
    {
        ScriptText = scriptText ?? "";
        ActivePhrase = activePhrase ?? "";
        ActiveEventIndex = activeEventIndex;
        var tokens = ScriptTextTokenizer.Tokenize(ScriptText);
        WordCount = tokens.Count;
        WordIndex = ScriptTextTokenizer.ResolveWordIndex(tokens, ActivePhrase, ActiveEventIndex);
        IsLive = true;
    }

    public static void Clear()
    {
        ScriptText = "";
        ActivePhrase = "";
        ActiveEventIndex = -1;
        WordIndex = 0;
        WordCount = 0;
        IsLive = false;
    }
}
