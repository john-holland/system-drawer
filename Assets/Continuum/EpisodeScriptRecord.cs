using System;

/// <summary>
/// Episode script: one script record per episode. Links script content (ref or inline text) to episode_id.
/// </summary>
[Serializable]
public class EpisodeScriptRecord
{
    public string id;
    public string episodeId;
    public string scriptRef;   // FK to document_blobs or semantic_chunks
    public string scriptText;  // inline text if not using ref
    public string language;
    public string createdAt;   // ISO8601
}
