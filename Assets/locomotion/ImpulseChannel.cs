using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Named impulse channel for routing impulses through the nervous system.
/// Channels can have priorities and filters to control impulse flow.
/// </summary>
[System.Serializable]
public class ImpulseChannel
{
    [Header("Channel Properties")]
    [Tooltip("Name of this channel (e.g., 'Spinal', 'Limb', 'Emergency')")]
    public string channelName;

    [Tooltip("Channel priority (higher = more important)")]
    public int priority = 0;

    [Header("Filters")]
    [Tooltip("Filters that control which impulses pass through this channel")]
    public List<ImpulseFilter> filters = new List<ImpulseFilter>();

    // Internal state
    private Queue<ImpulseData> impulseQueue = new Queue<ImpulseData>();
    private int maxQueueSize = 100;

    /// <summary>
    /// Create a new impulse channel.
    /// </summary>
    public ImpulseChannel(string name, int priority = 0)
    {
        this.channelName = name;
        this.priority = priority;
        this.filters = new List<ImpulseFilter>();
    }

    /// <summary>
    /// Send an impulse through this channel.
    /// Returns true if impulse was accepted (passed filters).
    /// </summary>
    public bool SendImpulse(ImpulseData impulse)
    {
        if (impulse == null || impulseQueue == null)
            return false;
        if (!ShouldAllow(impulse))
            return false;
        if (impulseQueue.Count < maxQueueSize)
        {
            impulseQueue.Enqueue(impulse);
            return true;
        }
        else
        {
            Debug.LogWarning($"Impulse channel '{channelName}' queue is full, dropping impulse");
            return false;
        }
    }

    /// <summary>
    /// Get next impulse from queue (returns null if queue is empty). Skips null entries.
    /// </summary>
    public ImpulseData GetNextImpulse()
    {
        if (impulseQueue == null)
            return null;
        while (impulseQueue.Count > 0)
        {
            var impulse = impulseQueue.Dequeue();
            if (impulse != null)
                return impulse;
        }
        return null;
    }

    /// <summary>
    /// Check if channel has any pending impulses.
    /// </summary>
    public bool HasImpulses()
    {
        return impulseQueue.Count > 0;
    }

    /// <summary>
    /// Get number of pending impulses in queue.
    /// </summary>
    public int GetQueueSize()
    {
        return impulseQueue.Count;
    }

    /// <summary>
    /// Clear all impulses from queue.
    /// </summary>
    public void ClearQueue()
    {
        impulseQueue.Clear();
    }

    /// <summary>
    /// Register a filter for this channel.
    /// </summary>
    public void RegisterFilter(ImpulseFilter filter)
    {
        if (filter != null && !filters.Contains(filter))
        {
            filters.Add(filter);
        }
    }

    /// <summary>
    /// Remove a filter from this channel.
    /// </summary>
    public void RemoveFilter(ImpulseFilter filter)
    {
        if (filter != null && filters.Contains(filter))
        {
            filters.Remove(filter);
        }
    }

    /// <summary>
    /// Check if an impulse should be allowed through filters.
    /// </summary>
    private bool ShouldAllow(ImpulseData impulse)
    {
        // If no filters, allow all
        if (filters == null || filters.Count == 0)
        {
            return true;
        }

        // Apply all filters (all must pass for impulse to be allowed)
        foreach (var filter in filters)
        {
            if (filter != null && !filter.ShouldAllow(impulse))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get all pending impulses (without removing from queue).
    /// </summary>
    public List<ImpulseData> PeekImpulses(int maxCount = -1)
    {
        List<ImpulseData> impulses = new List<ImpulseData>();
        int count = 0;
        
        foreach (var impulse in impulseQueue)
        {
            if (maxCount > 0 && count >= maxCount)
                break;
            
            impulses.Add(impulse);
            count++;
        }

        return impulses;
    }

    /// <summary>
    /// Sort queue by priority (highest priority first). Ignores null entries.
    /// </summary>
    public void SortQueueByPriority()
    {
        if (impulseQueue == null || impulseQueue.Count <= 1)
            return;

        var sorted = impulseQueue.Where(i => i != null).OrderByDescending(i => i.priority).ToList();
        impulseQueue.Clear();
        foreach (var impulse in sorted)
        {
            impulseQueue.Enqueue(impulse);
        }
    }
}
