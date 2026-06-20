using System;

[Serializable]
public class LocalizationChangeListRecord
{
    public string id;
    public string episodeScriptId;
    public string draftEpisodeId;
    public string commentTopicId;
    public string workflowStatus;
    public int revision;
    public int reviewCycle;
    public string lastSavedAt;
    public string submitScheduleCron;
    public string submitWindowOpensAt;
    public string submitWindowClosesAt;
    public string createdAt;
    public string updatedAt;
    public string submittedAt;
    public string mergedAt;
}

[Serializable]
public sealed class LocalizationChangeListItemRecord
{
    public string id;
    public string changeListId;
    public int sortOrder;
    public string severity;
    public string itemType;
    public string bindingId;
    public string description;
    public int oldCharStart;
    public int oldCharEnd;
    public int newCharStart;
    public int newCharEnd;
    public bool autoApplied;
    public bool userAcknowledged;
    public string supersededAt;
    public string createdAt;
}

[Serializable]
public sealed class LocalizationChangeListReviewerRecord
{
    public string changeListId;
    public string userId;
    public string role;
    public string approvedAt;
    public string rejectedAt;
}
