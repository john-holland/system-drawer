using UnityEngine;

/// <summary>Civil statute card: bill text, chamber, sponsor paper-doll refs, and law stage.</summary>
[CreateAssetMenu(fileName = "LawCard", menuName = "Locomotion/Civil/Law Card")]
public sealed class LawCard : ScriptableObject
{
    public string statuteId;
    [TextArea] public string billText;
    public LawChamberKind chamber = LawChamberKind.House;
    public LawStageKind stage = LawStageKind.Draft;
    public CivilianPaperDoll sponsor;
    public SenatePersonPaperDoll senateSponsor;
    public CongressPersonPaperDoll congressSponsor;
    public ParliamentPersonPaperDoll parliamentSponsor;
    public MonarchPaperDoll monarchSponsor;
    [Range(0f, 1f)] public float lastScore01 = 1f;

    public float Allow01() => lastScore01;
}
