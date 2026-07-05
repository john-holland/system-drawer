using System;

[Serializable]
public sealed class ReviewerCommentRecord
{
    public string id;
    public string reviewerId;
    public string scriptRef;
    public int textSelectionStart;
    public int textSelectionEnd;
    public string commentText;
    public string commentTopicId;
    public int reviewCycle;
    public string propertyKey;
    public string deleteRequestedAt;
    public string deleteRequestedBy;
    public string deleteApprovedAt;
    public string deleteApprovedBy;
    public string createdAt;
}

[Serializable]
public sealed class ReviewerCommentArchiveRecord
{
    public string id;
    public string reviewerId;
    public string originalCommentId;
    public string commentText;
    public string previouslyOn;
    public int textSelectionStart;
    public int textSelectionEnd;
    public string propertyKey;
    public int reviewCycle;
    public string archivedAt;
    public string archivedReason;
}
