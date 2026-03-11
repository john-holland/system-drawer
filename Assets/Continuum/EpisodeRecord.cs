using System;

/// <summary>
/// Episode record for continuum DB: time info, scene/engine, tokenized script ref, plot description.
/// </summary>
[Serializable]
public class EpisodeRecord
{
    public string id;
    public string tenantId = "default";
    public string title;
    public string createdAt;  // ISO8601
    public string engine = "unity";  // "unity" or "unreal"
    public string scenePath;  // Unity: Assets/Scenes/Episode1.unity
    public float tStart;
    public float tEnd;
    public string tokenizedScriptRef;  // FK to document_blobs or semantic_chunks
    public string plotDescription;
}
