using System;

/// <summary>
/// Episode asset reference: links USC assets to an episode with causality leaf ID.
/// </summary>
[Serializable]
public class EpisodeAssetRef
{
    public string id;
    public string episodeId;
    public string uscAssetId;
    public string assetType;  // "document", "chunk", "kernel"
    public string role;  // "causality_source", "scene_prop"
    public string causalityLeafId;  // e.g. S3.O2.1.7
}
