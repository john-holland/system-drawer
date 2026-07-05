using System;
using System.Collections.Generic;

/// <summary>
/// Vocabulary render mask: one row per vocabulary asset synonym. The set of bucket IDs
/// is in VocabularyRenderMaskBucket; load and populate bucketIds when needed.
/// </summary>
[Serializable]
public class VocabularyRenderMask
{
    public string id;
    public string tenantId = "default";
    public string assetSynonym;  // e.g. "ladder", "choice_point"
    public string episodeId;    // nullable; set when mask built in episode context

    /// <summary>Loaded from vocabulary_render_mask_buckets; not persisted in this row.</summary>
    [NonSerialized]
    public List<string> bucketIds;
}
