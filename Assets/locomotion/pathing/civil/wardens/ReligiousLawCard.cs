using System.Collections.Generic;
using UnityEngine;

/// <summary>Religious-form law: contained LawCards plus scripture refs.</summary>
[CreateAssetMenu(fileName = "ReligiousLawCard", menuName = "Locomotion/Civil/Religious Law Card")]
public sealed class ReligiousLawCard : ScriptableObject
{
    public string doctrineId;
    public List<LawCard> lawCards = new List<LawCard>();
    public List<string> scriptureRefs = new List<string>();
    [Range(0f, 1f)] public float lastScore01 = 1f;

    public float Allow01() => lastScore01;
}

[CreateAssetMenu(fileName = "ReligiousFigure", menuName = "Locomotion/Civil/Religious Figure")]
public sealed class ReligiousFigure : ScriptableObject
{
    public string figureId = "cleric";
    public string displayName = "Religious Figure";
    public ReligiousLawCard law;
    public List<string> scriptureRefs = new List<string>();
    public CivilianPaperDoll paperDoll;
}
