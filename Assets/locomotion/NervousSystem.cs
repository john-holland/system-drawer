using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Core nervous system component that routes impulses bidirectionally (up/down),
/// maintains temporal graph of good sections, coordinates tool usage discovery,
/// and manages goal queues and cleanup tasks.
/// </summary>
public class NervousSystem : MonoBehaviour
{
    [Header("Impulse Routing")]
    [Tooltip("Named impulse channels for routing")]
    public List<ImpulseChannel> impulseChannels = new List<ImpulseChannel>();

    [Header("Good Sections")]
    [Tooltip("Available good sections (physics cards)")]
    public List<GoodSection> goodSections = new List<GoodSection>();

    [Header("Consider Components")]
    [Tooltip("Consider components for dynamic card generation")]
    public List<Consider> considerComponents = new List<Consider>();

    [Header("Tool Tracking")]
    [Tooltip("Known tools and their original positions for cleanup")]
    public Dictionary<GameObject, ToolInfo> knownTools = new Dictionary<GameObject, ToolInfo>();

    [Header("Goal Management")]
    [Tooltip("Goal queue for sequential task execution")]
    public Queue<BehaviorTreeGoal> goalQueue = new Queue<BehaviorTreeGoal>();

    [Tooltip("Cleanup stack for post-task cleanup goals")]
    public Stack<BehaviorTreeGoal> cleanupStack = new Stack<BehaviorTreeGoal>();

    // Internal state
    private Dictionary<string, ImpulseChannel> channelDict = new Dictionary<string, ImpulseChannel>();
    private Dictionary<string, GoodSection> goodSectionDict = new Dictionary<string, GoodSection>();
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private TemporalGraph temporalGraph;
    private BehaviorTreeGoal currentGoal;

    private void Awake()
    {
        // Build channel dictionary
        foreach (var channel in impulseChannels)
        {
            if (channel != null && !string.IsNullOrEmpty(channel.channelName))
            {
                channelDict[channel.channelName] = channel;
            }
        }

        // Build good section dictionary
        foreach (var section in goodSections)
        {
            if (section != null && !string.IsNullOrEmpty(section.sectionName))
            {
                goodSectionDict[section.sectionName] = section;
            }
        }

        // Initialize temporal graph
        temporalGraph = new TemporalGraph();
        
        // Register all good sections in temporal graph
        foreach (var section in goodSections)
        {
            if (section != null)
            {
                temporalGraph.AddNode(section);
            }
        }

        // Auto-find consider components
        if (considerComponents == null || considerComponents.Count == 0)
        {
            considerComponents = GetComponentsInChildren<Consider>().ToList();
        }
    }

    private void Update()
    {
        // Process impulse channels
        ProcessImpulseChannels();

        // Process cleanup goals if no active goal
        if (currentGoal == null && cleanupStack.Count > 0)
        {
            ProcessCleanupGoals();
        }
    }

    /// <summary>
    /// Send impulse down (motor command) to specified channel.
    /// </summary>
    public void SendImpulseDown(string channel, ImpulseData data)
    {
        if (data == null)
            return;

        // Ensure impulse type is Motor
        data.impulseType = ImpulseType.Motor;

        if (channelDict.TryGetValue(channel, out ImpulseChannel channelObj))
        {
            channelObj.SendImpulse(data);
        }
        else
        {
            Debug.LogWarning($"Impulse channel '{channel}' not found in NervousSystem");
        }
    }

    /// <summary>
    /// Send impulse up (sensory data) to specified channel.
    /// </summary>
    public void SendImpulseUp(string channel, ImpulseData data)
    {
        if (data == null)
            return;

        // Ensure impulse type is Sensory
        data.impulseType = ImpulseType.Sensory;

        if (channelDict.TryGetValue(channel, out ImpulseChannel channelObj))
        {
            channelObj.SendImpulse(data);
        }
        else
        {
            Debug.LogWarning($"Impulse channel '{channel}' not found in NervousSystem");
        }
    }

    /// <summary>
    /// Get available good sections for a target object.
    /// </summary>
    public List<GoodSection> GetAvailableGoodSections(GameObject target)
    {
        List<GoodSection> available = new List<GoodSection>();

        foreach (var section in goodSections)
        {
            if (section != null && section.IsFeasible(GetCurrentState()))
            {
                available.Add(section);
            }
        }

        // Query consider components for additional cards
        foreach (var consider in considerComponents)
        {
            if (consider != null)
            {
                var generatedCards = consider.GenerateCardsForTarget(target);
                available.AddRange(generatedCards);
            }
        }

        return available;
    }

    /// <summary>
    /// Get sequence of good sections to reach a goal.
    /// </summary>
    public List<GoodSection> GetGoodSectionSequence(GameObject target, GoodSection goal)
    {
        if (goal == null)
            return new List<GoodSection>();

        RagdollState currentState = GetCurrentState();
        
        // Use temporal graph to find path
        if (temporalGraph != null)
        {
            return temporalGraph.FindPath(currentState, goal);
        }

        // Fallback: direct path
        return new List<GoodSection> { goal };
    }

    /// <summary>
    /// Register a tool with its original position for cleanup tracking.
    /// </summary>
    public void RegisterTool(GameObject tool, Vector3 originalPosition)
    {
        if (tool == null)
            return;

        originalPositions[tool] = originalPosition;

        ToolInfo toolInfo = new ToolInfo
        {
            gameObject = tool,
            toolName = tool.name,
            originalPosition = originalPosition
        };

        knownTools[tool] = toolInfo;
    }

    /// <summary>
    /// Generate a cleanup goal for returning a tool to its original position.
    /// </summary>
    public BehaviorTreeGoal GenerateCleanupGoal(GameObject tool, CleanupUrgency urgency = CleanupUrgency.AfterTask)
    {
        if (tool == null || !originalPositions.ContainsKey(tool))
        {
            return null; // Tool wasn't tracked
        }

        Vector3 returnPos = originalPositions[tool];

        BehaviorTreeGoal goal = new BehaviorTreeGoal
        {
            goalName = $"return_{tool.name}",
            type = GoalType.Cleanup,
            target = tool,
            targetPosition = returnPos,
            requiresCleanup = false, // This IS the cleanup
            priority = (urgency == CleanupUrgency.Immediate) ? 10 : 5,
            cleanupUrgency = urgency
        };

        return goal;
    }

    /// <summary>
    /// Add a cleanup goal to the cleanup stack.
    /// </summary>
    public void AddCleanupGoal(BehaviorTreeGoal cleanupGoal)
    {
        if (cleanupGoal != null && cleanupGoal.type == GoalType.Cleanup)
        {
            cleanupStack.Push(cleanupGoal);
        }
    }

    /// <summary>
    /// Process all pending cleanup goals.
    /// </summary>
    public void ProcessCleanupGoals()
    {
        while (cleanupStack.Count > 0)
        {
            BehaviorTreeGoal cleanupGoal = cleanupStack.Pop();

            // Generate return cards from consider components
            List<GoodSection> returnCards = new List<GoodSection>();
            
            foreach (var consider in considerComponents)
            {
                if (consider != null)
                {
                    var cards = consider.GenerateReturnCards(cleanupGoal.target, cleanupGoal.targetPosition);
                    returnCards.AddRange(cards);
                }
            }

            // Execute return sequence (this would be handled by behavior tree in practice)
            // For now, just verify tool is returned
            if (cleanupGoal.target != null && cleanupGoal.targetPosition != null)
            {
                float distance = Vector3.Distance(
                    cleanupGoal.target.transform.position,
                    cleanupGoal.targetPosition
                );

                if (distance < 0.1f) // Within tolerance
                {
                    originalPositions.Remove(cleanupGoal.target);
                    Debug.Log($"Tool {cleanupGoal.target.name} returned successfully");
                }
            }
        }
    }

    /// <summary>
    /// Register a good section in the nervous system.
    /// </summary>
    public void RegisterGoodSection(GoodSection section)
    {
        if (section != null && !string.IsNullOrEmpty(section.sectionName))
        {
            if (!goodSections.Contains(section))
            {
                goodSections.Add(section);
            }

            goodSectionDict[section.sectionName] = section;

            // Add to temporal graph
            if (temporalGraph != null)
            {
                temporalGraph.AddNode(section);
            }
        }
    }

    /// <summary>
    /// Get current ragdoll state (from RagdollSystem if available).
    /// </summary>
    public RagdollState GetCurrentState()
    {
        RagdollSystem ragdollSystem = GetComponent<RagdollSystem>();
        if (ragdollSystem != null)
        {
            return ragdollSystem.GetCurrentState();
        }

        // Fallback: create empty state
        return new RagdollState();
    }

    /// <summary>
    /// Process all impulse channels (call this in Update).
    /// </summary>
    private void ProcessImpulseChannels()
    {
        foreach (var channel in impulseChannels)
        {
            if (channel != null && channel.HasImpulses())
            {
                // Sort queue by priority
                channel.SortQueueByPriority();

                // Process impulses (in practice, these would be routed to appropriate handlers)
                ImpulseData impulse;
                while ((impulse = channel.GetNextImpulse()) != null)
                {
                    HandleImpulse(impulse);
                }
            }
        }
    }

    /// <summary>
    /// Handle an impulse (route to appropriate handler).
    /// </summary>
    private void HandleImpulse(ImpulseData impulse)
    {
        if (impulse == null)
            return;

        if (impulse.impulseType == ImpulseType.Motor)
        {
            // Route motor impulse to muscle system
            MotorData motorData = impulse.GetData<MotorData>();
            if (motorData != null)
            {
                RagdollSystem ragdollSystem = GetComponent<RagdollSystem>();
                if (ragdollSystem != null)
                {
                    ragdollSystem.ActivateMuscleGroup(motorData.muscleGroup, motorData.activation);
                }
            }
        }
        else if (impulse.impulseType == ImpulseType.Sensory)
        {
            // Route sensory impulse to brain/behavior tree system
            // This would be handled by Brain components in practice
            SensoryData sensoryData = impulse.GetData<SensoryData>();
            if (sensoryData != null)
            {
                // Notify consider components of sensory input
                foreach (var consider in considerComponents)
                {
                    if (consider != null)
                    {
                        consider.ProcessSensoryInput(sensoryData);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get an impulse channel by name.
    /// </summary>
    public ImpulseChannel GetChannel(string channelName)
    {
        channelDict.TryGetValue(channelName, out ImpulseChannel channel);
        return channel;
    }

    /// <summary>
    /// Get a good section by name.
    /// </summary>
    public GoodSection GetGoodSection(string sectionName)
    {
        goodSectionDict.TryGetValue(sectionName, out GoodSection section);
        return section;
    }

    /// <summary>
    /// Set the current goal.
    /// </summary>
    public void SetCurrentGoal(BehaviorTreeGoal goal)
    {
        currentGoal = goal;
    }

    /// <summary>
    /// Get the current goal.
    /// </summary>
    public BehaviorTreeGoal GetCurrentGoal()
    {
        return currentGoal;
    }

    /// <summary>
    /// Add a goal to the queue.
    /// </summary>
    public void AddGoal(BehaviorTreeGoal goal)
    {
        if (goal != null)
        {
            goalQueue.Enqueue(goal);
        }
    }

    /// <summary>
    /// Get the temporal graph.
    /// </summary>
    public TemporalGraph GetTemporalGraph()
    {
        return temporalGraph;
    }
}
