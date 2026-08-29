using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KingCard : MonarchCard
{
    public bool ceremonial = true;
    public MonarchPaperDoll doll;
    public MonarchicVenueRuntime venue;

    public KingCard()
    {
        sectionName = "king";
        physicalPathingTag = "monarch_king";
        decorum = ceremonial ? "ceremonial" : "sovereign";
    }
}

[System.Serializable]
public class QueenCard : MonarchCard
{
    public bool ceremonial = true;
    public MonarchPaperDoll doll;
    public MonarchicVenueRuntime venue;

    public QueenCard()
    {
        sectionName = "queen";
        physicalPathingTag = "monarch_queen";
        decorum = ceremonial ? "ceremonial" : "sovereign";
    }
}

[System.Serializable]
public class KnightCard : GoodSection
{
    public string houseId = "house";

    public KnightCard()
    {
        isCivilGoal = true;
        sectionName = "knight";
        physicalPathingTag = "knight";
    }
}

[System.Serializable]
public class SquireCard : GoodSection
{
    public KnightCard knight;

    public SquireCard()
    {
        isCivilGoal = true;
        sectionName = "squire";
        physicalPathingTag = "squire";
    }
}

[System.Serializable]
public class KnaveCard : GoodSection
{
    public KnaveCard()
    {
        isCivilGoal = true;
        sectionName = "knave";
        physicalPathingTag = "knave";
    }
}

[System.Serializable]
public class JesterCard : GoodSection
{
    public RagdollAnimationSetManager danceSet;
    [TextArea] public string dialogue;
    [Range(0f, 1f)] public float parkour01 = 0.4f;
    [Range(0f, 1f)] public float voiceCoeff01 = 0.5f;
    [Range(0f, 1f)] public float physicCoeff01 = 0.5f;
    [Range(0f, 1f)] public float theocraticCoeff01 = 0.3f;
    [Range(0f, 1f)] public float governmentCoeff01 = 0.3f;
    [Range(0f, 1f)] public float crimeCoeff01;

    public JesterCard()
    {
        isCivilGoal = true;
        sectionName = "jester";
        physicalPathingTag = "jester";
    }
}

[System.Serializable]
public class CouncilorCard : GoodSection
{
    public bool developerInpaint;
    public List<LawCard> laws = new List<LawCard>();
    public List<string> scriptureRefs = new List<string>();
    [TextArea] public string inpaintPrompt;

    public CouncilorCard()
    {
        isCivilGoal = true;
        sectionName = "councilor";
        physicalPathingTag = "councilor";
    }
}

[System.Serializable]
public class ChancellorCard : GoodSection
{
    public bool isHeadOfUniversity;
    public UniversityCampusAsset universityReference;
    public bool isScribe;
    public PenInkInstrument penInk;
    public PaintCanvas sharedCanvas;
    [TextArea] public string inpaintPrompt;

    public ChancellorCard()
    {
        isCivilGoal = true;
        sectionName = "chancellor";
        physicalPathingTag = "chancellor";
    }

    public void WireSharedCanvas()
    {
        if (sharedCanvas == null && penInk != null)
            sharedCanvas = penInk.GetComponent<PaintCanvas>() ?? penInk.GetComponentInChildren<PaintCanvas>();
        if (sharedCanvas == null) return;
        if (sharedCanvas.surfaceKind == PaintCanvas.SurfaceKind.Plane)
            sharedCanvas.surfaceKind = PaintCanvas.SurfaceKind.CurvedDecal;
    }
}

[System.Serializable]
public class ExecutiveCard : GoodSection
{
    public bool militaristic;

    public ExecutiveCard()
    {
        isCivilGoal = true;
        sectionName = "executive";
        physicalPathingTag = "executive";
    }
}

[CreateAssetMenu(fileName = "ConversationCard", menuName = "Locomotion/Civil/Conversation Card")]
public sealed class ConversationCard : ScriptableObject
{
    public string cardId = "conversation";
    [TextArea] public string line;
    public string dialogTreeSetId;
}

[CreateAssetMenu(fileName = "LawConversationCard", menuName = "Locomotion/Civil/Law Conversation Card")]
public sealed class LawConversationCard : ScriptableObject
{
    public string cardId = "law_conversation";
    public List<CivilianPaperDoll> civilians = new List<CivilianPaperDoll>();
    public List<SenatePersonPaperDoll> senators = new List<SenatePersonPaperDoll>();
    public List<CongressPersonPaperDoll> congresspeople = new List<CongressPersonPaperDoll>();
    public List<ParliamentPersonPaperDoll> parliament = new List<ParliamentPersonPaperDoll>();
    public List<MonarchPaperDoll> monarchs = new List<MonarchPaperDoll>();
}
