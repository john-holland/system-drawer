using System;

/// <summary>DTO types shared by lemma build UI, chat, and codegen validation.</summary>
[Serializable]
public sealed class LemmaMechanismDescriptor
{
    public string lemma;
    public string posTag;
    public string mechanicalRole;
    public int outputTier;
    public string functionalDescription;
    public string mechanismPrompt;
    public string[] synonyms;
    public LemmaCompositionChildPutDto[] compositionChildren;
    public ThesaurusEntryPropertyRecord[] properties;
}

[Serializable]
public sealed class LemmaBuildFormSnapshot
{
    public string lemma;
    public string posTag;
    public string mechanicalRole;
    public int outputTier;
    public string functionalDescription;
    public string mechanismPrompt;
    public string[] synonyms;
    public LemmaCompositionChildPutDto[] compositionChildren;
    public ThesaurusEntryPropertyRecord[] properties;
}

[Serializable]
public sealed class LemmaBuildChatMessage
{
    public string role;
    public string content;
    public string timestampUtc;
}

[Serializable]
public sealed class LemmaBuildChatSessionData
{
    public LemmaBuildChatMessage[] messages;
    public string modelId;
    public string lemmaSlug;
}
