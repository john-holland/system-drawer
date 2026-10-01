using System;

/// <summary>Property keys for {P:taste|notes=sour,spicy|intensity=0.5} lemma painting.</summary>
public static class TasteNotesLemmaPropertyKeys
{
    public const string PlaceholderName = "taste";
    public const string Notes = "notes";
    public const string Intensity = "intensity";

    public const string SpecNotes = "taste-notes";
    public const string SpecIntensity = "taste-intensity";

    public static readonly string[] AllKeys = { Notes, Intensity };
}

[Serializable]
public struct TasteNotesLemmaProperties
{
    public string notesCsv;
    public float intensity01;

    public static TasteNotesLemmaProperties Defaults => new TasteNotesLemmaProperties
    {
        notesCsv = "",
        intensity01 = 0.5f
    };
}
