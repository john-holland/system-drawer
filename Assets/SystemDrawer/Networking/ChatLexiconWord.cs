using System;

[Serializable]
public sealed class ChatLexiconWord
{
    public string id;
    public string text;
    public string lemmaEntryId;
}

[Serializable]
public sealed class ChatLexiconData
{
    public ChatLexiconWord[] words;
}
