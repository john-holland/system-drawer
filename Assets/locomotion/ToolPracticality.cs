using UnityEngine;

/// <summary>
/// Tool practicality evaluation structure.
/// Contains scores and feasibility information for tool usage.
/// </summary>
[System.Serializable]
public class ToolPracticality
{
    [Header("Tool Information")]
    [Tooltip("Tool GameObject")]
    public GameObject tool;

    [Tooltip("Tool name")]
    public string toolName;

    [Header("Scoring (0-1)")]
    [Tooltip("How useful this tool is for current goal")]
    [Range(0f, 1f)]
    public float usefulness = 0.5f;

    [Tooltip("Can we reach/grasp this tool")]
    [Range(0f, 1f)]
    public float accessibility = 0.5f;

    [Tooltip("Is this tool better than alternatives")]
    [Range(0f, 1f)]
    public float efficiency = 0.5f;

    [Tooltip("How hard is it to hold this tool")]
    [Range(0f, 1f)]
    public float graspDifficulty = 0.5f;

    [Tooltip("Combined overall score")]
    [Range(0f, 1f)]
    public float overallScore = 0.5f;

    [Header("Generated Cards")]
    [Tooltip("Card for approaching tool")]
    public GoodSection approachCard;

    [Tooltip("Card for grasping tool")]
    public GoodSection graspCard;

    [Tooltip("Card for orienting tool")]
    public GoodSection orientCard;

    [Tooltip("Card for using tool")]
    public GoodSection useCard;

    [Tooltip("Card for releasing tool")]
    public GoodSection releaseCard;

    [Tooltip("Card for returning tool")]
    public GoodSection returnCard;

    [Header("Feasibility")]
    [Tooltip("Can we actually use this tool?")]
    public bool isFeasible = false;

    [Tooltip("Reason for feasibility/infeasibility")]
    public string feasibilityReason = "";

    /// <summary>
    /// Calculate overall score from individual scores.
    /// </summary>
    public void CalculateOverallScore()
    {
        // Weighted average: usefulness (40%), accessibility (30%), efficiency (20%), graspDifficulty (10%)
        overallScore = usefulness * 0.4f + 
                      accessibility * 0.3f + 
                      efficiency * 0.2f + 
                      (1f - graspDifficulty) * 0.1f; // Lower grasp difficulty = better
    }

    /// <summary>
    /// Validate feasibility (check if tool can actually be used).
    /// </summary>
    public void ValidateFeasibility()
    {
        // Basic feasibility check
        isFeasible = usefulness > 0.3f && 
                    accessibility > 0.3f && 
                    efficiency > 0.2f;

        if (!isFeasible)
        {
            if (usefulness <= 0.3f)
                feasibilityReason = "Tool is not useful enough for this task";
            else if (accessibility <= 0.3f)
                feasibilityReason = "Tool is not accessible (cannot reach/grasp)";
            else if (efficiency <= 0.2f)
                feasibilityReason = "Tool is less efficient than alternatives";
        }
        else
        {
            feasibilityReason = "Tool is feasible for use";
        }
    }
}
