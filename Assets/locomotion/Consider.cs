using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Dynamic card generation component that adds cards either once on start or in real-time with timeout refresh.
/// Generates preposition cards, tool usage cards, and cleanup cards.
/// </summary>
public class Consider : MonoBehaviour
{
    [Header("Generation Mode")]
    [Tooltip("When to generate cards")]
    public GenerationMode mode = GenerationMode.OnStart;

    [Tooltip("Refresh interval for real-time generation (seconds)")]
    public float refreshInterval = 2f;

    [Header("Tool Awareness")]
    [Tooltip("Scan for tools in the environment")]
    public bool scanForTools = true;

    [Tooltip("Range to scan for tools")]
    public float toolScanRange = 10f;

    [Tooltip("Generate tool usage cards")]
    public bool generateToolUsageCards = true;

    [Tooltip("Generate cleanup cards")]
    public bool generateCleanupCards = true;

    [Header("Goal Awareness")]
    [Tooltip("Consider current goals when generating cards")]
    public bool considerCurrentGoals = true;

    [Tooltip("Prune invalid cards based on current state")]
    public bool pruneInvalidCards = true;

    [Header("Preposition Cards")]
    [Tooltip("Generate preposition cards ('in front', 'to the left', 'on top')")]
    public bool generatePrepositionCards = true;

    // Internal state
    private Dictionary<GameObject, List<GoodSection>> toolUsageCards = new Dictionary<GameObject, List<GoodSection>>();
    private Dictionary<GameObject, GoodSection> toolReturnCards = new Dictionary<GameObject, GoodSection>();
    private Dictionary<GameObject, float> cardTimeouts = new Dictionary<GameObject, float>();
    private float lastRefreshTime = 0f;
    private float timeoutSeconds = 5f;

    // References
    private NervousSystem nervousSystem;
    private RagdollSystem ragdollSystem;
    private PhysicsCardSolver cardSolver;

    private void Awake()
    {
        // Get references
        nervousSystem = GetComponentInParent<NervousSystem>();
        ragdollSystem = GetComponentInParent<RagdollSystem>();
        cardSolver = GetComponentInParent<PhysicsCardSolver>();

        // Register with nervous system
        if (nervousSystem != null && !nervousSystem.considerComponents.Contains(this))
        {
            nervousSystem.considerComponents.Add(this);
        }
    }

    private void Start()
    {
        if (mode == GenerationMode.OnStart)
        {
            GenerateCardsForTarget(null);
        }
    }

    private void Update()
    {
        // Real-time generation with refresh interval
        if (mode == GenerationMode.RealTime)
        {
            if (Time.time - lastRefreshTime >= refreshInterval)
            {
                GenerateCardsForTarget(null);
                lastRefreshTime = Time.time;
            }
        }

        // Clean up expired cards
        CleanupExpiredCards();
    }

    /// <summary>
    /// Generate cards for a target object (preposition cards, tool usage, etc.).
    /// </summary>
    public List<GoodSection> GenerateCardsForTarget(GameObject target)
    {
        List<GoodSection> generatedCards = new List<GoodSection>();

        // Generate preposition cards if enabled
        if (generatePrepositionCards)
        {
            var prepositionCards = GeneratePrepositionCards(target);
            generatedCards.AddRange(prepositionCards);
        }

        // Scan for tools and generate tool usage cards
        if (scanForTools && generateToolUsageCards)
        {
            var tools = ScanForTools(toolScanRange, GetCurrentGoal());
            foreach (var tool in tools)
            {
                if (tool.gameObject != null)
                {
                    var toolCards = GenerateToolUsageCards(tool.gameObject, GetCurrentTask(), GetCurrentState());
                    generatedCards.AddRange(toolCards);
                    toolUsageCards[tool.gameObject] = toolCards;
                    
                    // Register tool with nervous system
                    if (nervousSystem != null)
                    {
                        nervousSystem.RegisterTool(tool.gameObject, tool.originalPosition);
                    }
                }
            }
        }

        // Add to card solver
        if (cardSolver != null)
        {
            cardSolver.AddCards(generatedCards);
        }

        return generatedCards;
    }

    /// <summary>
    /// Scan for tools relevant to current goals.
    /// </summary>
    public List<ToolInfo> ScanForTools(float range, BehaviorTreeGoal goal)
    {
        List<ToolInfo> tools = new List<ToolInfo>();

        // Find all GameObjects with colliders in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        
        foreach (var collider in colliders)
        {
            GameObject obj = collider.gameObject;
            
            // Skip self
            if (obj == gameObject || obj.transform.IsChildOf(transform))
                continue;

            // Evaluate if object is a tool (simplified: check for specific tags or components)
            ToolInfo toolInfo = EvaluateTool(obj, goal);
            if (toolInfo != null && toolInfo.usefulness > 0.3f)
            {
                tools.Add(toolInfo);
            }
        }

        return tools;
    }

    /// <summary>
    /// Evaluate tool practicality for a goal.
    /// </summary>
    public ToolPracticality EvaluateTool(GameObject tool, BehaviorTreeGoal goal, RagdollState state)
    {
        if (tool == null || goal == null)
            return null;

        ToolPracticality practicality = new ToolPracticality
        {
            tool = tool,
            toolName = tool.name
        };

        // Calculate usefulness (simplified: based on tool name matching goal)
        if (goal.goalName.Contains("turn") && tool.name.ToLower().Contains("screwdriver"))
        {
            practicality.usefulness = 0.9f;
        }
        else if (goal.goalName.Contains("cut") && tool.name.ToLower().Contains("knife"))
        {
            practicality.usefulness = 0.9f;
        }
        else
        {
            practicality.usefulness = 0.5f; // Default
        }

        // Calculate accessibility (based on distance and reachability)
        float distance = Vector3.Distance(transform.position, tool.transform.position);
        practicality.accessibility = 1f - Mathf.Clamp01(distance / toolScanRange);

        // Calculate efficiency (simplified)
        practicality.efficiency = 0.7f;

        // Calculate grasp difficulty (simplified: based on object size/mass)
        Rigidbody rb = tool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            practicality.graspDifficulty = Mathf.Clamp01(rb.mass / 10f); // Heavier = harder to grasp
        }
        else
        {
            practicality.graspDifficulty = 0.3f;
        }

        // Calculate overall score and validate feasibility
        practicality.CalculateOverallScore();
        practicality.ValidateFeasibility();

        return practicality;
    }

    /// <summary>
    /// Generate tool usage cards for a tool and task.
    /// </summary>
    public List<GoodSection> GenerateToolUsageCards(GameObject tool, string task, RagdollState state)
    {
        List<GoodSection> cards = new List<GoodSection>();

        if (tool == null)
            return cards;

        // Generate approach card
        GoodSection approachCard = GenerateApproachCard(tool);
        cards.Add(approachCard);

        // Generate grasp card
        GoodSection graspCard = GenerateGraspCard(tool);
        cards.Add(graspCard);

        // Generate orient card
        GoodSection orientCard = GenerateOrientCard(tool);
        cards.Add(orientCard);

        // Generate use card
        GoodSection useCard = GenerateUseCard(tool, task);
        cards.Add(useCard);

        // Generate release card
        GoodSection releaseCard = GenerateReleaseCard(tool);
        cards.Add(releaseCard);

        return cards;
    }

    /// <summary>
    /// Generate return cards for a tool to return it to original position.
    /// </summary>
    public List<GoodSection> GenerateReturnCards(GameObject tool, Vector3 returnPosition)
    {
        List<GoodSection> cards = new List<GoodSection>();

        if (tool == null)
            return cards;

        // Generate approach card to return position
        GoodSection approachCard = GenerateApproachCardToPosition(returnPosition);
        cards.Add(approachCard);

        // Generate release card
        GoodSection releaseCard = GenerateReleaseCard(tool);
        cards.Add(releaseCard);

        // Store return card
        if (cards.Count > 0)
        {
            toolReturnCards[tool] = cards[0]; // Store first card as return card
        }

        return cards;
    }

    /// <summary>
    /// Generate preposition cards ("in front", "to the left", "on top") for a target.
    /// These are pathing targets for behavior tree hierarchical pathing systems.
    /// </summary>
    private List<GoodSection> GeneratePrepositionCards(GameObject target)
    {
        List<GoodSection> cards = new List<GoodSection>();

        if (target == null)
        {
            // Generate cards relative to self
            cards.Add(CreatePrepositionCard("in_front", transform.position + transform.forward * 2f));
            cards.Add(CreatePrepositionCard("to_the_left", transform.position + -transform.right * 2f));
            cards.Add(CreatePrepositionCard("to_the_right", transform.position + transform.right * 2f));
            cards.Add(CreatePrepositionCard("behind", transform.position + -transform.forward * 2f));
        }
        else
        {
            // Generate cards relative to target
            Vector3 targetPos = target.transform.position;
            Vector3 toTarget = (targetPos - transform.position).normalized;
            
            cards.Add(CreatePrepositionCard("in_front_of_target", targetPos + toTarget * 1f));
            cards.Add(CreatePrepositionCard("to_the_left_of_target", targetPos + Vector3.Cross(toTarget, Vector3.up) * 1f));
            cards.Add(CreatePrepositionCard("to_the_right_of_target", targetPos + -Vector3.Cross(toTarget, Vector3.up) * 1f));
        }

        return cards;
    }

    /// <summary>
    /// Create a preposition card for a position.
    /// </summary>
    private GoodSection CreatePrepositionCard(string name, Vector3 position)
    {
        GoodSection card = new GoodSection
        {
            sectionName = name,
            description = $"Move to {name}",
            requiredState = GetCurrentState(),
            targetState = CreateStateAtPosition(position),
            limits = new SectionLimits()
        };

        // Create impulse action for movement
        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Legs",
            activation = 0.7f,
            duration = 1f
        };

        card.impulseStack = new List<ImpulseAction> { action };

        return card;
    }

    // Helper methods for generating tool usage cards (simplified implementations)

    private GoodSection GenerateApproachCard(GameObject tool)
    {
        Vector3 toolPos = tool.transform.position;
        Vector3 approachPos = toolPos - (toolPos - transform.position).normalized * 0.5f;

        return CreatePrepositionCard($"approach_{tool.name}", approachPos);
    }

    private GoodSection GenerateApproachCardToPosition(Vector3 position)
    {
        return CreatePrepositionCard("approach_return_position", position);
    }

    private GoodSection GenerateGraspCard(GameObject tool)
    {
        GoodSection card = new GoodSection
        {
            sectionName = $"grasp_{tool.name}",
            description = $"Grasp {tool.name}",
            requiredState = GetCurrentState(),
            limits = new SectionLimits()
        };

        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Hand",
            activation = 0.8f,
            duration = 0.5f
        };

        card.impulseStack = new List<ImpulseAction> { action };
        return card;
    }

    private GoodSection GenerateOrientCard(GameObject tool)
    {
        GoodSection card = new GoodSection
        {
            sectionName = $"orient_{tool.name}",
            description = $"Orient {tool.name} for use",
            requiredState = GetCurrentState(),
            limits = new SectionLimits()
        };

        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Arm",
            activation = 0.6f,
            duration = 0.3f
        };

        card.impulseStack = new List<ImpulseAction> { action };
        return card;
    }

    private GoodSection GenerateUseCard(GameObject tool, string task)
    {
        GoodSection card = new GoodSection
        {
            sectionName = $"use_{tool.name}_{task}",
            description = $"Use {tool.name} to {task}",
            requiredState = GetCurrentState(),
            limits = new SectionLimits()
        };

        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Arm",
            activation = 0.7f,
            duration = 1f
        };

        card.impulseStack = new List<ImpulseAction> { action };
        return card;
    }

    private GoodSection GenerateReleaseCard(GameObject tool)
    {
        GoodSection card = new GoodSection
        {
            sectionName = $"release_{tool.name}",
            description = $"Release {tool.name}",
            requiredState = GetCurrentState(),
            limits = new SectionLimits()
        };

        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Hand",
            activation = 0f, // Release = no activation
            duration = 0.2f
        };

        card.impulseStack = new List<ImpulseAction> { action };
        return card;
    }

    private ToolInfo EvaluateTool(GameObject obj, BehaviorTreeGoal goal)
    {
        if (obj == null)
            return null;

        ToolInfo toolInfo = new ToolInfo
        {
            gameObject = obj,
            toolName = obj.name,
            originalPosition = obj.transform.position
        };

        // Simplified tool evaluation
        toolInfo.usefulness = 0.5f;
        toolInfo.accessibility = 1f - Mathf.Clamp01(Vector3.Distance(transform.position, obj.transform.position) / toolScanRange);

        return toolInfo;
    }

    private void CleanupExpiredCards()
    {
        // Remove cards that have timed out (for real-time generation)
        var expiredKeys = cardTimeouts.Where(kvp => Time.time > kvp.Value).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            cardTimeouts.Remove(key);
            toolUsageCards.Remove(key);
            toolReturnCards.Remove(key);
        }
    }

    private void ProcessSensoryInput(SensoryData sensoryData)
    {
        // Process sensory input from nervous system
        // This would trigger card regeneration or updates
        if (mode == GenerationMode.RealTime && sensoryData != null)
        {
            // Regenerate cards on significant sensory input
            GenerateCardsForTarget(sensoryData.contactObject);
        }
    }

    private RagdollState GetCurrentState()
    {
        if (ragdollSystem != null)
        {
            return ragdollSystem.GetCurrentState();
        }
        return new RagdollState();
    }

    private BehaviorTreeGoal GetCurrentGoal()
    {
        if (nervousSystem != null)
        {
            return nervousSystem.GetCurrentGoal();
        }
        return null;
    }

    private string GetCurrentTask()
    {
        var goal = GetCurrentGoal();
        return goal != null ? goal.goalName : "default_task";
    }

    private RagdollState CreateStateAtPosition(Vector3 position)
    {
        RagdollState state = GetCurrentState().CopyState();
        state.rootPosition = position;
        return state;
    }
}

/// <summary>
/// Generation mode for Consider component.
/// </summary>
public enum GenerationMode
{
    OnStart,     // Generate once on start
    RealTime     // Generate continuously with refresh interval
}
