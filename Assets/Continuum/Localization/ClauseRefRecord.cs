using System;

/// <summary>Shared clause anchor — Farey identity + char cache for script localization.</summary>
[Serializable]
public sealed class ClauseRefRecord
{
    public int fareyLeftNum;
    public int fareyLeftDen = 1;
    public int fareyRightNum = 1;
    public int fareyRightDen = 1;
    public int charStart;
    public int charEnd;
    public string selectionText;
    public string astNodeId;
    public string entryId;
    public string draftScriptId;
    public string episodeScriptId;

    public FareySpanRecord FareySpan => new FareySpanRecord
    {
        ln = fareyLeftNum,
        ld = fareyLeftDen,
        rn = fareyRightNum,
        rd = fareyRightDen
    };
}

public static class LocalizationBindingKinds
{
    public const string Property = "property";
    public const string Lemma = "lemma";
    public const string Localization = "localization";
    public const string PromptPlaceholder = "prompt_placeholder";
}
