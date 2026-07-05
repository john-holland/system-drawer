using System;

/// <summary>
/// One bucket ID in a vocabulary render mask (quad/oct/4D leaf ID). FK to vocabulary_render_masks.
/// </summary>
[Serializable]
public class VocabularyRenderMaskBucket
{
    public string id;
    public string maskId;   // FK → vocabulary_render_masks(id)
    public string bucketId;  // e.g. Q2.1.3, O2.1.7, S3.O2.1.7
}
