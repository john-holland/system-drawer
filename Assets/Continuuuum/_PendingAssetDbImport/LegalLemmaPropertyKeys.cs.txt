/// <summary>Lemma keys for court, constitution, scripture, and chambers.</summary>
public static class LegalLemmaPropertyKeys
{
    public const string Court = "court";
    public const string Constitution = "constitution";
    public const string Scripture = "scripture";
    public const string Chamber = "chamber";
    public const string Rights = "rights";
    public const string Law = "law";
    public const string Junta = "junta";
    public const string GenevaConventions = "geneva-conventions";
    public const string Torture = "torture";
    public const string RespectsGenevaConventions = "respects-geneva-conventions";
    public const string Announce = "announce";
    public const string Returned = "returned";
    public const string RightsReturned = "rights-returned";
    public const string AnnounceRightsReturned = "announce-rights-returned";

    public static readonly string[] LemmaPlaceholders =
    {
        "court", "constitution", "scripture", "chamber", "rights", "law", "junta",
        "geneva-conventions", "torture", "respects-geneva-conventions",
        "announce", "returned", "rights-returned", "announce-rights-returned"
    };
}
