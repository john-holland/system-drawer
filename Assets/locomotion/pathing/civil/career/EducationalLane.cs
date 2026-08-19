using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EducationalLaneGoal
{
    public LearningStationKind station = LearningStationKind.Desk;
    public string credentialId;
    [Range(0f, 1f)] public float[] expected01 = { 0.5f, 0.5f, 0.5f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
}

[CreateAssetMenu(fileName = "EducationalLane", menuName = "Locomotion/Civil/Educational Lane")]
public sealed class EducationalLane : ScriptableObject
{
    public string laneId = "default";
    public List<EducationalLaneGoal> goals = new List<EducationalLaneGoal>();

    public static EducationalLane CreateWith(params LearningStationKind[] stations)
    {
        var lane = CreateInstance<EducationalLane>();
        lane.goals = new List<EducationalLaneGoal>();
        if (stations == null) return lane;
        for (int i = 0; i < stations.Length; i++)
            lane.goals.Add(new EducationalLaneGoal { station = stations[i] });
        return lane;
    }
}
