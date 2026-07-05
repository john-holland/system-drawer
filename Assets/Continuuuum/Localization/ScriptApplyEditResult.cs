using System;

[Serializable]
public sealed class ScriptApplyEditResult
{
    public string changeListId;
    public int revision;
    public LocalizationChangeListItemRecord[] required;
    public LocalizationChangeListItemRecord[] warnings;

    public static ScriptApplyEditResult Empty => new ScriptApplyEditResult
    {
        required = Array.Empty<LocalizationChangeListItemRecord>(),
        warnings = Array.Empty<LocalizationChangeListItemRecord>()
    };
}
