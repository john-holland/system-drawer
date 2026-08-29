using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum RelationshipStationKind
{
    Approach = 0,
    ShareSpace = 1,
    DialogColumn = 2,
    Intimacy = 3,
    License = 4,
    Vow = 5,
    RestrainingOrder = 6
}

/// <summary>Serializable relationship route: subjects, target stage, worlds, dialog tree id.</summary>
[CreateAssetMenu(fileName = "RelationshipRoute", menuName = "Locomotion/Civil/Relationship Route")]
public sealed class RelationshipRoute : ScriptableObject
{
    public RomanceSeverity targetSeverity = RomanceSeverity.GoingOut;
    public string customStageId;
    public string dialogTreeId;
    public RelationshipDialogTree dialogTree;
    public List<GameObject> subjects = new List<GameObject>();
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
}

[Serializable]
public sealed class RelationshipStep
{
    public RelationshipStationKind station = RelationshipStationKind.Approach;
    public RomanceSeverity targetSeverity = RomanceSeverity.Crush;
    public string customStageId;
    public Vector3 predictedWorld;
    public Vector3 inpaintWorld;
    public bool hasInpaint;
    [Range(0f, 1f)] public float[] expected01 = { 0.5f, 0.5f, 0.5f, 0.5f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public EducationalTimingMode timing = EducationalTimingMode.Specific;
    public float minSeconds = 600f;
    public float maxSeconds = 3600f;
    public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 19, 0, 0);
    public float durationSeconds = 1800f;
    public string eventId;
    public string enablesEventId;
    public Bounds4? spatiotemporalVolume;
    public int dialogColumnIndex;
    public string dialogNodeId;
    public LawCard lawCard;
    public ReligiousLawCard religiousLawCard;
    public CourtWarden courtWarden;

    public float[] Expected01() => CivilianPaperDoll.Pad4(expected01, 0.5f);

    public float[] FireLimit01() => CivilianPaperDoll.Pad4(fireLimit01, 0.9f);

    public float DurationSeconds()
    {
        if (timing == EducationalTimingMode.RngRange)
            return Mathf.Max(1f, (minSeconds + maxSeconds) * 0.5f);
        return Mathf.Max(1f, durationSeconds);
    }
}
