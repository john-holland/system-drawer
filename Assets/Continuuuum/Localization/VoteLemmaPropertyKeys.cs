/// <summary>Lemma keys for ballots, tallies, recounts, and queue-by-address in-paint.</summary>
public static class VoteLemmaPropertyKeys
{
    public const string Vote = "vote";
    public const string Ballot = "ballot";
    public const string Recount = "recount";
    public const string Tally = "tally";
    public const string Queue = "queue";
    public const string Queued = "queued";
    public const string Address = "address";
    public const string HomeAddress = "home-address";
    public const string Randomly = "randomly";
    public const string Happily = "happily";
    public const string IfSo = "if-so";
    public const string Property = "property";

    /// <summary>Default developer in-paint on the local voting-place SG node. <c>if</c> is prefix / infix / postfix / circumfix; anaphor <c>if so</c> after an adverb postfixes.</summary>
    public const string DefaultInpaintPrompt = "queued by address, or randomly, if so";

    public static readonly string[] LemmaPlaceholders =
    {
        "vote", "ballot", "recount", "tally",
        "queue", "queued", "address", "home-address", "randomly", "happily", "if-so", "property"
    };
}
