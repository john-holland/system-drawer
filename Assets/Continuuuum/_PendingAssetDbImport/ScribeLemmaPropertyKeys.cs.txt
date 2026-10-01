/// <summary>Lemma keys for scribe documents / pages / anchors.</summary>
public static class ScribeLemmaPropertyKeys
{
    public const string ScribeSet = "scribe-set";
    public const string Page = "page";
    public const string Anchor = "anchor";
    public const string Format = "format";
    public const string PeckingOrder = "pecking-order";

    public static readonly string[] LemmaPlaceholders =
    {
        "scribe-set", "page", "anchor", "format", "pecking-order"
    };
}
