using System;
using System.Collections.Generic;

/// <summary>
/// One built-in thesaurus-style entry: stable URN <see cref="Id"/>, language code, lemma, POS, category, optional tags.
/// </summary>
public readonly struct VocabularyBuiltInDescriptor : IEquatable<VocabularyBuiltInDescriptor>
{
    public string Id { get; }
    public string LanguageCode { get; }
    public string Term { get; }
    public string PosTag { get; }
    public VocabularyBuiltInCategory Category { get; }
    public IReadOnlyList<string> Tags { get; }

    public VocabularyBuiltInDescriptor(
        string id,
        string languageCode,
        string term,
        string posTag,
        VocabularyBuiltInCategory category,
        IReadOnlyList<string> tags = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        LanguageCode = VocabularyLanguageEncoding.NormalizeLanguageCode(languageCode);
        Term = term ?? throw new ArgumentNullException(nameof(term));
        PosTag = posTag ?? "unknown";
        Category = category;
        Tags = tags ?? Array.Empty<string>();
    }

    public bool Equals(VocabularyBuiltInDescriptor other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is VocabularyBuiltInDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id != null ? Id.GetHashCode() : 0;
    }
}
