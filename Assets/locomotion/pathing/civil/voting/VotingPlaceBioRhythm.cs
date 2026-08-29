using UnityEngine;

/// <summary>Session hours, turnout, and ballot-box fill. Activates the perimeter VotingPlaceCard.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Voting Place Bio Rhythm")]
public sealed class VotingPlaceBioRhythm : MonoBehaviour
{
    public CivilVenueBioRhythmService venueBio;
    public VotingPlaceCard perimeter = new VotingPlaceCard();
    public LaneGrid laneGrid;
    public VotingQueueHub queueHub;
    public VoteLedger ledger;
    public string hoursCron = "* 7-20 * * 2,6";
    [Range(0f, 1f)] public float sessionOpen01 = 0.5f;
    [Range(0f, 1f)] public float turnout01;
    [Range(0f, 1f)] public float ballotBoxFill01;
    public int ballotsIssued;
    public int ballotsCast;
    public int ballotsSpoiled;

    void Awake()
    {
        if (venueBio == null)
            venueBio = GetComponent<CivilVenueBioRhythmService>()
                       ?? gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (laneGrid == null)
            laneGrid = GetComponent<LaneGrid>() ?? gameObject.AddComponent<LaneGrid>();
        if (queueHub == null)
            queueHub = GetComponent<VotingQueueHub>() ?? gameObject.AddComponent<VotingQueueHub>();
        if (ledger == null)
            ledger = GetComponent<VoteLedger>() ?? gameObject.AddComponent<VoteLedger>();
        if (perimeter == null)
            perimeter = new VotingPlaceCard();
        queueHub.centralQueue = laneGrid;
        queueHub.placeCard = perimeter;
        perimeter.laneGrid = laneGrid;
        perimeter.hub = queueHub;
        perimeter.bioRhythm = this;
        perimeter.hazardTarget = gameObject;
    }

    public void Tick(System.DateTime utcNow)
    {
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        sessionOpen01 = open ? 0.9f : 0.1f;
        MeasureBallots();
        if (venueBio != null)
        {
            venueBio.activity01 = sessionOpen01;
            venueBio.stress01 = ballotBoxFill01;
            venueBio.pace01 = Mathf.Clamp01(0.35f + turnout01 * 0.4f);
        }
        if (perimeter != null)
        {
            perimeter.laneGrid = laneGrid;
            perimeter.hub = queueHub;
        }
        if (queueHub != null)
        {
            queueHub.centralQueue = laneGrid;
            queueHub.placeCard = perimeter;
            queueHub.Tick();
        }
    }

    public void MeasureBallots()
    {
        if (ledger != null)
        {
            ballotsIssued = ledger.IssuedCount;
            ballotsCast = ledger.CastCount;
            ballotsSpoiled = ledger.SpoiledCount;
        }
        int denom = Mathf.Max(1, ballotsIssued);
        ballotBoxFill01 = Mathf.Clamp01(ballotsCast / (float)denom);
        turnout01 = ballotBoxFill01;
    }

    public VotingPlaceCard ActivatePerimeter()
    {
        if (perimeter == null)
            perimeter = new VotingPlaceCard();
        perimeter.laneGrid = laneGrid;
        perimeter.hub = queueHub;
        perimeter.bioRhythm = this;
        if (queueHub != null)
        {
            queueHub.centralQueue = laneGrid;
            queueHub.placeCard = perimeter;
        }
        return perimeter;
    }
}
