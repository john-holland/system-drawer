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

    [Header("Tipping Analysis")]
    [Tooltip("Enable center of mass tipping analysis")]
    public bool enableTippingAnalysis = true;

    [Tooltip("Minimum torque-to-weight ratio for viable tipping")]
    [Range(0f, 1f)]
    public float tippingViabilityThreshold = 0.3f;

    [Tooltip("Number of directions to evaluate (8 = cardinal + diagonal)")]
    public int tippingDirectionsToEvaluate = 8;

    [Tooltip("How many cards deep to predict after tip")]
    public int postTipPredictionDepth = 3;

    [Header("Mesh Surface Analysis")]
    [Tooltip("Enable mesh surface analysis for placement planes")]
    public bool enableSurfaceAnalysis = true;

    [Tooltip("Minimum angle between adjacent vectors for viable surface (degrees)")]
    public float minPlacementAngle = 50f;

    [Tooltip("Mesh sampling resolution")]
    public float surfaceSamplingDensity = 0.1f;

    [Header("Hemispherical Enclosure")]
    [Tooltip("Enable hemispherical grasp evaluation (Lego hand style)")]
    public bool enableHemisphericalGrasp = true;

    [Tooltip("Hemisphere threshold (0.55 = slightly more than half)")]
    [Range(0f, 1f)]
    public float hemisphereThreshold = 0.55f;

    [Tooltip("Tolerance for enclosure fit")]
    public float enclosureTolerance = 0.05f;

    // Internal state
    private Dictionary<GameObject, List<GoodSection>> toolUsageCards = new Dictionary<GameObject, List<GoodSection>>();
    private Dictionary<GameObject, GoodSection> toolReturnCards = new Dictionary<GameObject, GoodSection>();
    private Dictionary<GameObject, float> cardTimeouts = new Dictionary<GameObject, float>();
    private float lastRefreshTime = 0f;
    private float timeoutSeconds = 5f;

    // Advanced features state
    private Dictionary<GameObject, TippingAnalysis> tippingAnalyses = new Dictionary<GameObject, TippingAnalysis>();
    private Dictionary<GameObject, List<PlacementPlane>> placementSurfaces = new Dictionary<GameObject, List<PlacementPlane>>();
    private Dictionary<GameObject, EnclosureFeasibility> enclosureFeasibilities = new Dictionary<GameObject, EnclosureFeasibility>();

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

        // Generate advanced feature cards if enabled
        if (target != null)
        {
            // Tipping analysis
            if (enableTippingAnalysis)
            {
                var tippingCards = GenerateTippingCards(target, GetCurrentState());
                generatedCards.AddRange(tippingCards);
            }

            // Surface analysis
            if (enableSurfaceAnalysis)
            {
                var surfaces = AnalyzePlacementSurfaces(target);
                foreach (var surface in surfaces)
                {
                    var placementCards = GeneratePlacementCards(target, surface, GetCurrentState());
                    generatedCards.AddRange(placementCards);
                }
            }

            // Hemispherical enclosure
            if (enableHemisphericalGrasp)
            {
                Hand hand = GetDefaultHand(); // Would get actual hand from ragdoll system
                if (hand != null)
                {
                    var enclosureCard = GenerateHemisphericalGraspCard(target, hand, GetCurrentState());
                    if (enclosureCard != null)
                    {
                        generatedCards.Add(enclosureCard);
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

    // ========== Advanced Features: Tipping Analysis ==========

    /// <summary>
    /// Analyze object for viable tipping directions based on center of mass.
    /// </summary>
    public List<TippingCard> GenerateTippingCards(GameObject obj, RagdollState state)
    {
        List<TippingCard> tippingCards = new List<TippingCard>();

        if (obj == null)
            return tippingCards;

        // Get object's center of mass
        Vector3 centerOfMass = GetCenterOfMass(obj);
        Bounds bounds = GetObjectBounds(obj);

        // Evaluate multiple directions
        for (int i = 0; i < tippingDirectionsToEvaluate; i++)
        {
            float angle = (360f / tippingDirectionsToEvaluate) * i;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // Evaluate viability
            TippingViability viability = EvaluateTippingDirection(obj, direction, state);

            if (viability.viability >= tippingViabilityThreshold)
            {
                // Generate tipping card
                TippingCard card = new TippingCard
                {
                    targetObject = obj,
                    tipDirection = direction,
                    centerOfMass = centerOfMass,
                    tipAngle = CalculateTipAngle(obj, direction),
                    viabilityScore = viability.viability,
                    torqueRatio = viability.torqueRatio,
                    contactPoint = viability.contactPoint,
                    requiresStabilization = !viability.isStable
                };

                // Predict post-tip card tree
                card.postTipTree = GeneratePostTipCardTree(obj, direction, card.tipAngle, state);

                tippingCards.Add(card);
            }
        }

        return tippingCards;
    }

    /// <summary>
    /// Evaluate tipping viability in a direction.
    /// </summary>
    public TippingViability EvaluateTippingDirection(GameObject obj, Vector3 direction, RagdollState state)
    {
        TippingViability viability = new TippingViability
        {
            direction = direction,
            viability = 0f,
            isStable = false,
            reason = ""
        };

        if (obj == null)
        {
            viability.reason = "Object is null";
            return viability;
        }

        // Get center of mass and bounds
        Vector3 centerOfMass = GetCenterOfMass(obj);
        Bounds bounds = GetObjectBounds(obj);

        // Calculate contact point (edge of object in direction)
        Vector3 contactPoint = centerOfMass + direction * bounds.extents.magnitude;
        viability.contactPoint = contactPoint;

        // Calculate torque ratio (simplified)
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        float mass = rb != null ? rb.mass : 1f;
        float distance = Vector3.Distance(centerOfMass, contactPoint);
        float torque = mass * 9.81f * distance; // Weight * distance
        viability.torqueRatio = torque / (mass * 9.81f); // Normalized

        // Viability based on torque ratio and stability
        viability.viability = Mathf.Clamp01(viability.torqueRatio);
        viability.isStable = viability.torqueRatio > 0.5f; // More torque = more stable

        viability.reason = viability.isStable ? "Tipping is viable and stable" : "Tipping may require stabilization";

        return viability;
    }

    /// <summary>
    /// Generate predictive card tree after tipping.
    /// </summary>
    public CardTree GeneratePostTipCardTree(GameObject obj, Vector3 tipDirection, float tipAngle, RagdollState state)
    {
        CardTree tree = new CardTree();

        // Simulate object state after tipping
        RagdollState predictedState = PredictPostTipState(obj, tipDirection, tipAngle, state);

        // Generate cards available in predicted state (simplified)
        List<GoodSection> immediateCards = new List<GoodSection>();
        
        // Generate a few example post-tip cards
        GoodSection stabilizeCard = CreatePrepositionCard("stabilize_after_tip", obj.transform.position);
        immediateCards.Add(stabilizeCard);

        // Add branches to tree
        foreach (var card in immediateCards)
        {
            List<GoodSection> followUpCards = new List<GoodSection>();
            // Generate follow-up cards (simplified)
            followUpCards.Add(CreatePrepositionCard("continue_after_tip", obj.transform.position + tipDirection * 0.5f));
            
            tree.AddBranch(card, followUpCards);
        }

        return tree;
    }

    private Vector3 GetCenterOfMass(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null && rb.centerOfMass != Vector3.zero)
        {
            return obj.transform.TransformPoint(rb.centerOfMass);
        }
        return obj.transform.position;
    }

    private float CalculateTipAngle(GameObject obj, Vector3 direction)
    {
        // Simplified: calculate angle based on direction
        return Vector3.Angle(Vector3.up, direction);
    }

    private RagdollState PredictPostTipState(GameObject obj, Vector3 tipDirection, float tipAngle, RagdollState currentState)
    {
        // Simplified prediction: rotate object state
        RagdollState predicted = currentState.CopyState();
        predicted.rootRotation = Quaternion.AngleAxis(tipAngle, Vector3.Cross(Vector3.up, tipDirection)) * predicted.rootRotation;
        return predicted;
    }

    // ========== Advanced Features: Surface Analysis ==========

    /// <summary>
    /// Analyze mesh surfaces for placement planes (>50° adjacent vectors).
    /// </summary>
    public List<PlacementPlane> AnalyzePlacementSurfaces(GameObject obj)
    {
        List<PlacementPlane> placementPlanes = new List<PlacementPlane>();

        if (obj == null)
            return placementPlanes;

        // Get mesh
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return placementPlanes;

        Mesh mesh = meshFilter.sharedMesh;
        Transform objTransform = obj.transform;

        // Sample mesh vertices
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        GetSampledVertices(mesh, objTransform, surfaceSamplingDensity, out vertices, out normals);

        // Group vertices by similar normals
        Dictionary<Vector3, List<Vector3>> groupedByNormal = new Dictionary<Vector3, List<Vector3>>();

        foreach (Vector3 vertex in vertices)
        {
            Vector3 normal = GetNormalAtVertex(mesh, objTransform, vertex);

            // Find similar normal (within angle tolerance)
            bool foundGroup = false;
            foreach (Vector3 existingNormal in groupedByNormal.Keys)
            {
                float angle = Vector3.Angle(normal, existingNormal);
                if (angle < minPlacementAngle) // If >50° from adjacent, it's a distinct plane
                {
                    groupedByNormal[existingNormal].Add(vertex);
                    foundGroup = true;
                    break;
                }
            }

            if (!foundGroup)
            {
                groupedByNormal[normal] = new List<Vector3> { vertex };
            }
        }

        // Create placement planes from groups
        foreach (var group in groupedByNormal)
        {
            if (group.Value.Count >= 3) // Need at least 3 points for a plane
            {
                PlacementPlane plane = new PlacementPlane
                {
                    normal = group.Key,
                    vertices = group.Value,
                    center = CalculateCentroid(group.Value),
                    area = CalculatePlaneArea(group.Value),
                    angle = Vector3.Angle(group.Key, Vector3.up) // Angle from horizontal
                };

                // Evaluate stability
                plane.stabilityScore = 1f - (plane.angle / 90f); // 0-1 scale
                plane.isStable = plane.angle < 45f; // Stable if <45° from horizontal

                // Evaluate grasping
                plane.canGraspFromAbove = plane.angle < 30f;
                plane.canGraspFromSide = plane.angle > 60f;

                placementPlanes.Add(plane);
            }
        }

        placementSurfaces[obj] = placementPlanes;
        return placementPlanes;
    }

    /// <summary>
    /// Find viable placement surfaces for an object.
    /// </summary>
    public List<PlacementPlane> FindViablePlacementSurfaces(GameObject obj, float minAngle = 50f)
    {
        List<PlacementPlane> allSurfaces = AnalyzePlacementSurfaces(obj);
        return allSurfaces.Where(s => s.angle >= minAngle && s.isStable).ToList();
    }

    /// <summary>
    /// Generate cards for placing objects on surfaces.
    /// </summary>
    public List<GoodSection> GeneratePlacementCards(GameObject obj, PlacementPlane surface, RagdollState state)
    {
        List<GoodSection> cards = new List<GoodSection>();

        if (obj == null || surface == null)
            return cards;

        // Generate approach card to surface
        GoodSection approachCard = CreatePrepositionCard($"approach_surface_{obj.name}", surface.center);
        cards.Add(approachCard);

        // Generate placement card
        GoodSection placementCard = new GoodSection
        {
            sectionName = $"place_{obj.name}_on_surface",
            description = $"Place {obj.name} on surface",
            requiredState = state,
            limits = new SectionLimits()
        };

        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Hand",
            activation = 0.6f,
            duration = 0.5f
        };

        placementCard.impulseStack = new List<ImpulseAction> { action };
        cards.Add(placementCard);

        return cards;
    }

    private void GetSampledVertices(Mesh mesh, Transform transform, float density, out List<Vector3> vertices, out List<Vector3> normals)
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>();

        // Sample mesh vertices at specified density
        Vector3[] meshVertices = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;

        for (int i = 0; i < meshVertices.Length; i += Mathf.Max(1, Mathf.RoundToInt(1f / density)))
        {
            vertices.Add(transform.TransformPoint(meshVertices[i]));
            normals.Add(transform.TransformDirection(meshNormals[i]));
        }
    }

    private Vector3 GetNormalAtVertex(Mesh mesh, Transform transform, Vector3 worldVertex)
    {
        // Simplified: get average normal from nearby vertices
        Vector3 localVertex = transform.InverseTransformPoint(worldVertex);
        Vector3[] normals = mesh.normals;
        
        // Find closest vertex normal (simplified)
        if (normals.Length > 0)
        {
            return transform.TransformDirection(normals[0]);
        }
        
        return Vector3.up;
    }

    private Vector3 CalculateCentroid(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var v in vertices)
        {
            sum += v;
        }
        return sum / vertices.Count;
    }

    private float CalculatePlaneArea(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count < 3)
            return 0f;

        // Simplified: calculate area using cross product
        float area = 0f;
        for (int i = 0; i < vertices.Count - 2; i++)
        {
            Vector3 v1 = vertices[i + 1] - vertices[0];
            Vector3 v2 = vertices[i + 2] - vertices[0];
            area += Vector3.Cross(v1, v2).magnitude * 0.5f;
        }
        return area;
    }

    // ========== Advanced Features: Hemispherical Enclosure ==========

    /// <summary>
    /// Check if object can be enclosed with hemispherical grasp.
    /// </summary>
    public bool CanEncloseHemispherically(GameObject obj, Hand hand)
    {
        EnclosureFeasibility feasibility = EvaluateHemisphericalEnclosure(obj, hand, hemisphereThreshold);
        return feasibility.canEnclose;
    }

    /// <summary>
    /// Generate hemispherical grasp card.
    /// </summary>
    public GoodSection GenerateHemisphericalGraspCard(GameObject obj, Hand hand, RagdollState state)
    {
        EnclosureFeasibility feasibility = EvaluateHemisphericalEnclosure(obj, hand, hemisphereThreshold);

        if (!feasibility.canEnclose)
            return null;

        HemisphericalGraspCard card = new HemisphericalGraspCard
        {
            targetObject = obj,
            hand = hand,
            graspPoint = feasibility.optimalGraspPoint,
            approachDirection = feasibility.optimalGraspDirection,
            fingerSpread = feasibility.requiredFingerSpread,
            gripStrength = feasibility.gripStrengthRequired,
            enclosureRatio = feasibility.enclosureRatio,
            requiredState = state,
            limits = new SectionLimits()
        };

        // Create impulse action for grasping
        ImpulseAction action = new ImpulseAction
        {
            muscleGroup = "Hand",
            activation = Mathf.Clamp01(feasibility.gripStrengthRequired / 100f), // Normalize to 0-1
            duration = 0.5f
        };

        card.impulseStack = new List<ImpulseAction> { action };
        return card;
    }

    /// <summary>
    /// Evaluate hemispherical enclosure feasibility.
    /// </summary>
    public EnclosureFeasibility EvaluateHemisphericalEnclosure(GameObject obj, Hand hand, float hemisphereThreshold = 0.55f)
    {
        EnclosureFeasibility feasibility = new EnclosureFeasibility
        {
            canEnclose = false,
            feasibilityReason = ""
        };

        if (obj == null || hand == null)
        {
            feasibility.feasibilityReason = "Object or hand is null";
            return feasibility;
        }

        // Get object bounds
        Bounds objBounds = GetObjectBounds(obj);

        // Calculate minimum enclosing sphere
        float sphereRadius = Mathf.Max(
            objBounds.size.x,
            objBounds.size.y,
            objBounds.size.z
        ) / 2f;

        // Get hand geometry (hemisphere radius)
        float handHemisphereRadius = hand.hemisphereRadius;

        // Check if hand can enclose object (slightly more than half sphere = 55%+)
        float requiredEnclosure = sphereRadius / handHemisphereRadius;

        if (requiredEnclosure > hemisphereThreshold)
        {
            feasibility.canEnclose = false;
            feasibility.feasibilityReason = $"Object too large (requires {requiredEnclosure:P}, threshold {hemisphereThreshold:P})";
            return feasibility;
        }

        // Calculate optimal grasp point (center of object)
        feasibility.optimalGraspPoint = objBounds.center;

        // Calculate optimal approach direction (from above, toward center)
        feasibility.optimalGraspDirection = -Vector3.up;

        // Calculate required finger spread
        float angleToEdge = Mathf.Atan2(objBounds.extents.y, handHemisphereRadius) * Mathf.Rad2Deg;
        feasibility.requiredFingerSpread = angleToEdge * 2f; // Total spread angle

        // Check if finger spread is feasible
        if (feasibility.requiredFingerSpread > hand.maxFingerSpread)
        {
            feasibility.canEnclose = false;
            feasibility.feasibilityReason = $"Finger spread too large ({feasibility.requiredFingerSpread:F1}° > {hand.maxFingerSpread:F1}°)";
            return feasibility;
        }

        // Calculate required grip strength (based on object weight)
        float objectMass = GetObjectMass(obj);
        feasibility.gripStrengthRequired = objectMass * 9.81f; // Weight in Newtons

        // Check if grip strength is sufficient
        if (feasibility.gripStrengthRequired > hand.maxGripStrength)
        {
            feasibility.canEnclose = false;
            feasibility.feasibilityReason = $"Object too heavy ({feasibility.gripStrengthRequired:F1}N > {hand.maxGripStrength:F1}N)";
            return feasibility;
        }

        // Calculate enclosure ratio (how well it encloses)
        feasibility.enclosureRatio = 1f - requiredEnclosure; // Inverse: smaller required = better enclosure
        feasibility.canEnclose = true;
        feasibility.feasibilityReason = "Object can be enclosed hemispherically";

        enclosureFeasibilities[obj] = feasibility;
        return feasibility;
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }

        return new Bounds(obj.transform.position, Vector3.one);
    }

    private float GetObjectMass(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        return rb != null ? rb.mass : 1f;
    }

    private Hand GetDefaultHand()
    {
        // Simplified: create default hand
        // In practice, would get actual hand from ragdoll system
        return new Hand
        {
            maxFingerSpread = 90f,
            maxGripStrength = 100f,
            hemisphereRadius = 0.1f
        };
    }
}

/// <summary>
/// Tipping analysis data structure.
/// </summary>
[System.Serializable]
public class TippingAnalysis
{
    public GameObject object;
    public List<TippingCard> cards;
    public Vector3 centerOfMass;
}

/// <summary>
/// Generation mode for Consider component.
/// </summary>
public enum GenerationMode
{
    OnStart,     // Generate once on start
    RealTime     // Generate continuously with refresh interval
}
