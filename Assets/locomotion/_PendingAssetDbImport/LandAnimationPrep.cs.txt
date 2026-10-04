using System.Collections.Generic;
using UnityEngine;

/// <summary>Single sample on a land impact envelope; <see cref="isImpact"/> marks contact keyframes.</summary>
[System.Serializable]
public class LandImpactKeyframe
{
    [Tooltip("Source animation frame index (optional authoring aid).")]
    public int frameIndex;

    [Tooltip("Normalized time along the land prep (0-1).")]
    [Range(0f, 1f)]
    public float time01;

    [Tooltip("Impact strength at this sample (0-1).")]
    [Range(0f, 1f)]
    public float strength01 = 1f;

    [Tooltip("When true, this keyframe is a designated impact / contact moment.")]
    public bool isImpact;
}

/// <summary>Impact envelope for landing BT IK prep, with designated impact keyframes.</summary>
[System.Serializable]
public class LandImpactCurve
{
    [Tooltip("Continuous impact envelope over normalized land time (0-1).")]
    public AnimationCurve envelope = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Discrete keyframes; mark contact with isImpact.")]
    public List<LandImpactKeyframe> keyframes = new List<LandImpactKeyframe>();

    public float Evaluate(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        if (envelope != null && envelope.keys != null && envelope.keys.Length > 0)
            return Mathf.Clamp01(envelope.Evaluate(t01));

        if (keyframes == null || keyframes.Count == 0)
            return 0f;

        float best = 0f;
        float bestDist = float.MaxValue;
        for (int i = 0; i < keyframes.Count; i++)
        {
            LandImpactKeyframe k = keyframes[i];
            if (k == null) continue;
            float d = Mathf.Abs(k.time01 - t01);
            if (d < bestDist)
            {
                bestDist = d;
                best = k.strength01;
            }
        }
        return Mathf.Clamp01(best);
    }

    public List<LandImpactKeyframe> GetImpactKeyframes()
    {
        var list = new List<LandImpactKeyframe>();
        if (keyframes == null) return list;
        for (int i = 0; i < keyframes.Count; i++)
        {
            LandImpactKeyframe k = keyframes[i];
            if (k != null && k.isImpact)
                list.Add(k);
        }
        return list;
    }

    /// <summary>Fill a default ease-in envelope with one mid impact key if empty.</summary>
    public void EnsureExampleCurve()
    {
        if (envelope == null || envelope.keys == null || envelope.keys.Length == 0)
            envelope = AnimationCurve.EaseInOut(0f, 0.05f, 1f, 1f);

        if (keyframes == null)
            keyframes = new List<LandImpactKeyframe>();

        if (keyframes.Count == 0)
        {
            keyframes.Add(new LandImpactKeyframe
            {
                frameIndex = 0,
                time01 = 0f,
                strength01 = 0.05f,
                isImpact = false
            });
            keyframes.Add(new LandImpactKeyframe
            {
                frameIndex = 12,
                time01 = 0.55f,
                strength01 = 1f,
                isImpact = true
            });
            keyframes.Add(new LandImpactKeyframe
            {
                frameIndex = 24,
                time01 = 1f,
                strength01 = 0.2f,
                isImpact = false
            });
        }
    }
}

/// <summary>ABT-side land animation prep: landing goal + impact curve for parkour BT IK.</summary>
[System.Serializable]
public class LandAnimationPrep
{
    [Tooltip("Landing goal template; type is forced to Land when applied.")]
    public BehaviorTreeGoal landingGoal;

    [Tooltip("Impact envelope with designated impact keyframes.")]
    public LandImpactCurve impactCurve = new LandImpactCurve();

    [Tooltip("Seconds before contact to begin land prep (authoring / planner aid).")]
    public float prepareLeadSeconds = 0.35f;

    public void EnsureReady()
    {
        if (landingGoal == null)
        {
            landingGoal = new BehaviorTreeGoal
            {
                goalName = "land",
                type = GoalType.Land,
                priority = 8
            };
        }
        else
        {
            landingGoal.type = GoalType.Land;
            if (string.IsNullOrEmpty(landingGoal.goalName))
                landingGoal.goalName = "land";
        }

        if (impactCurve == null)
            impactCurve = new LandImpactCurve();
        impactCurve.EnsureExampleCurve();
    }

    public BehaviorTreeGoal BuildGoalAt(Vector3 worldPosition)
    {
        EnsureReady();
        return new BehaviorTreeGoal
        {
            goalName = landingGoal.goalName,
            type = GoalType.Land,
            target = landingGoal.target,
            targetPosition = worldPosition,
            targetRotation = landingGoal.targetRotation,
            priority = landingGoal.priority,
            requiresCleanup = landingGoal.requiresCleanup,
            cleanupUrgency = landingGoal.cleanupUrgency
        };
    }
}
