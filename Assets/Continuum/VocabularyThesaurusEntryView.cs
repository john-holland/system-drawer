using System.Collections.Generic;

/// <summary>
/// Unified read model after local registration and optional API enrichment.
/// </summary>
public sealed class VocabularyThesaurusEntryView
{
    public string Id;
    public string Term;
    public string PosTag;
    public string LanguageCode;
    /// <summary>Optional continuum <c>languages.id</c> when resolved.</summary>
    public string LanguageId;
    public string Definition;
    public string Version;
    public VocabularyBuiltInCategory? BuiltInCategory;
    public List<string> Tags;
    public List<string> Alternatives;

    public bool IsBuiltIn => BuiltInCategory.HasValue || VocabularyLanguageEncoding.IsBuiltInUrn(Id);
}
