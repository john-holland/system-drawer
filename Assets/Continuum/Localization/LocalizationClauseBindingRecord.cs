using System;

[Serializable]
public sealed class LocalizationClauseBindingRecord
{
    public string id;
    public string episodeScriptId;
    public string draftScriptId;
    public int fareyLeftNum;
    public int fareyLeftDen;
    public int fareyRightNum;
    public int fareyRightDen;
    public int charStart;
    public int charEnd;
    public string selectionText;
    public string propertyKey;
    public string propertyValue;
    public string bindingKind;
    public string astNodeId;
    public string promptPlaceholderName;
    public string createdAt;
    public string updatedAt;

    public FareySpanRecord FareySpan => new FareySpanRecord
    {
        ln = fareyLeftNum,
        ld = fareyLeftDen,
        rn = fareyRightNum,
        rd = fareyRightDen
    };
}
