using System;

/// <summary>Phrase → ORM entry mapping for playback policy (no Inference assembly dependency).</summary>
[Serializable]
public struct PlaybackPhraseBinding
{
    public int eventIndex;
    public string phrase;
    public string resolvedOrmKey;
    public string builtInEntryId;
}
