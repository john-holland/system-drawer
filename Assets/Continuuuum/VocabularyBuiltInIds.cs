/// <summary>
/// Well-known built-in thesaurus entry URNs (same strings as <see cref="VocabularyBuiltInRegistry"/>).
/// Use for call sites; prefer <see cref="VocabularyLanguageEncoding.FormatBuiltInUrn"/> for dynamic entries.
/// </summary>
public static class VocabularyBuiltInIds
{
    public const string Prefix = VocabularyLanguageEncoding.BuiltInUrnPrefix;

    public static readonly string EnThe = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "det", "the");
    public static readonly string EnIf = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "conj", "if");
    public static readonly string EnThen = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "conj", "then");
    public static readonly string EnBack = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "back");
    public static readonly string EnForward = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "forward");
    public static readonly string EnPause = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "pause");
    public static readonly string EnVector3 = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "literal", "vector3");
    public static readonly string EnBoolean = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "literal", "boolean");
}
