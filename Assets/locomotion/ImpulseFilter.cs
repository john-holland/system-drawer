using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Impulse filtering system for filtering impulses in nervous system channels.
/// </summary>
[System.Serializable]
public class ImpulseFilter
{
    [Header("Filter Properties")]
    [Tooltip("Name of this filter")]
    public string filterName;

    [Tooltip("Type of filter")]
    public FilterType filterType = FilterType.Priority;

    [Header("Filter Conditions")]
    [Tooltip("Conditions that must be met for impulse to pass")]
    public List<FilterCondition> conditions = new List<FilterCondition>();

    /// <summary>
    /// Filter an impulse. Returns true if impulse should be allowed.
    /// </summary>
    public bool Filter(ImpulseData impulse)
    {
        return ShouldAllow(impulse);
    }

    /// <summary>
    /// Check if impulse should be allowed through this filter.
    /// </summary>
    public bool ShouldAllow(ImpulseData impulse)
    {
        if (impulse == null)
            return false;

        // Apply filter conditions
        foreach (var condition in conditions)
        {
            if (condition != null && !condition.Evaluate(impulse))
            {
                return false;
            }
        }

        // Apply filter type logic
        switch (filterType)
        {
            case FilterType.Priority:
                return impulse.priority >= 5; // Minimum priority threshold

            case FilterType.Type:
                // Would filter by impulse type
                return true;

            case FilterType.Source:
                // Would filter by source
                return true;

            default:
                return true;
        }
    }
}

/// <summary>
/// Types of impulse filters.
/// </summary>
public enum FilterType
{
    Priority,
    Type,
    Source
}

/// <summary>
/// Filter condition for impulse filtering.
/// </summary>
[System.Serializable]
public class FilterCondition
{
    public string propertyName;
    public ComparisonOperator comparison;
    public float value;

    public bool Evaluate(ImpulseData impulse)
    {
        // Simplified condition evaluation
        // In practice, would check different impulse properties
        return true;
    }
}
