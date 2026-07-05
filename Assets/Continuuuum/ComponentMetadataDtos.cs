using System;

[Serializable]
public class ComponentMetadataComponentDto
{
    public string typeName;
    public string assembly;
}

[Serializable]
public class ComponentMetadataNodeDto
{
    public string path;
    public string gameObjectName;
    public ComponentMetadataComponentDto[] components;
}

[Serializable]
public class ComponentMetadataCausalityLinkDto
{
    public string leafBack;
    public string leafPause;
    public string leafForward;
}

[Serializable]
public class ComponentMetadataSpatialBucketDto
{
    public string bucketId;
    public float px;
    public float py;
    public float pz;
    public float narrativeT;
}

[Serializable]
public class ComponentMetadataCausalityEdgeDto
{
    public string fromNodePath;
    public string toNodePath;
    public string kind;
}

[Serializable]
public class ComponentMetadataPayloadDto
{
    public int schemaVersion = 1;
    public string entryId;
    public string prefabRef;
    public string source;
    public string capturedAt;
    public string contentHash;
    public string runId;
    public ComponentMetadataNodeDto[] nodes;
    public ComponentMetadataCausalityLinkDto[] causalityLinks;
    public ComponentMetadataSpatialBucketDto[] spatialBuckets;
    public ComponentMetadataCausalityEdgeDto[] causality;
}

[Serializable]
public class ComponentMetadataPostResultDto
{
    public string id;
    public string entryId;
    public string runId;
}

[Serializable]
public class LemmaCompositionChildDto
{
    public string id;
    public string entryId;
    public string term;
    public int sortOrder;
    public string spatial4dId;
    public string anchorText;
    public string draftEpisodeId;
}

[Serializable]
public class LemmaCompositionResponseDto
{
    public string parentEntryId;
    public LemmaCompositionChildDto[] children;
    public bool isComposedLemma;
}

[Serializable]
public class LemmaCompositionPutBody
{
    public LemmaCompositionChildPutDto[] children;
}

[Serializable]
public class LemmaCompositionChildPutDto
{
    public string entryId;
    public int sortOrder;
}

[Serializable]
public class LemmaRecombobulateIssueDto
{
    public string id;
    public string code;
    public string severity;
    public string entryId;
    public string message;
    public bool requiresAck;
    public string storedText;
    public string currentText;
}

[Serializable]
public class LemmaRecombobulateResponseDto
{
    public string parentEntryId;
    public LemmaRecombobulateIssueDto[] issues;
    public string[] appliedIssueIds;
    public LemmaCompositionResponseDto composition;
}

[Serializable]
public class LemmaRecombobulateRequestDto
{
    public string scriptText;
    public string draftEpisodeId;
    public bool apply;
    public string[] acknowledgedIssueIds;
}
