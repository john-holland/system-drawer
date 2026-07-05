/// <summary>
/// DTO for rows from Continuuuum API / DB merge (thesaurus + optional dictionary text).
/// </summary>
public sealed class VocabularyApiThesaurusEntry
{
    public string Id;
    public string Term;
    public string PosTag;
    public string LanguageId;
    public string LanguageCode;
    public string Definition;
    public string Version;
    public string[] Alternatives;
}
