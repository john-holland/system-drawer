using UnityEngine;

public enum ScribeActivity
{
    Copy = 0,
    Illuminate = 1,
    Bind = 2,
    Deliver = 3
}

/// <summary>Scribe / document physics card: copy, illuminate, bind, deliver against a page/anchor.</summary>
[System.Serializable]
public class ScribeCard : GoodSection
{
    public ScribeActivity activity = ScribeActivity.Copy;
    public string configId;
    public int pageIndex;
    public string anchorKey;
    public int peckingOrder = 20;
    public string dialogTreeSetId;
    public GameObject pageSurface;
    public float accuracy01 = 0.85f;

    public ScribeCard()
    {
        isScribeGoal = true;
        physicalPathingTag = "scribe";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "scriptorium";
    }

    public static ScribeCard Generate(
        ScribeActivity activity,
        string configId,
        int pageIndex = 0,
        string anchorKey = null)
    {
        var card = new ScribeCard
        {
            activity = activity,
            configId = configId,
            pageIndex = pageIndex,
            anchorKey = anchorKey,
            sectionName = $"scribe_{activity}_{configId}_{pageIndex}",
            description = $"{activity} {configId} p{pageIndex}",
            isScribeGoal = true,
            physicalPathingTag = $"scribe_{activity.ToString().ToLowerInvariant()}",
            limits = new SectionLimits { maxForce = 40f, maxTorque = 8f, maxVelocityChange = 1f }
        };
        return card;
    }

    public string DutySummary() => $"{activity}:{configId}:{pageIndex}:{anchorKey}";
}
