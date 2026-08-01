using System;

/// <summary>Open taste tags for recipe/meal prompt in-painting (string-open via lemma).</summary>
public enum TasteNoteId
{
    Sour,
    Spicy,
    Sweet,
    Bitter,
    Umami,
    Salty
}

[Serializable]
public sealed class TasteNoteEntry
{
    public TasteNoteId note = TasteNoteId.Umami;
    [UnityEngine.Range(0f, 1f)] public float intensity01 = 0.5f;
    public string customTag;
}
