using System;

[Serializable]
public sealed class DraftEpisodeRecord
{
    public string id;
    public string episodeId;
    public string title;
    public string createdBy;
    public string committedAt;
    public string updatedAt;
    public string plotDescription;

    public string DisplayLabel
    {
        get
        {
            var t = string.IsNullOrEmpty(title) ? id : title;
            if (!string.IsNullOrEmpty(id) && id.Length > 8)
                return $"{t} ({id.Substring(0, 8)}…)";
            return t;
        }
    }

    public bool IsCommitted => !string.IsNullOrEmpty(committedAt);
}
