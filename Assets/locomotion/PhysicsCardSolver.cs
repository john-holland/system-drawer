using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>
/// Physics card solver that finds applicable good sections from current state,
/// orders them by feasibility, performs topological searches, and matches cards to behavior tree goals.
/// </summary>
public class PhysicsCardSolver : MonoBehaviour
{
    [Header("Available Cards")]
    [Tooltip("Current pool of available good sections")]
    public List<PhysicsCard> availableCards = new List<PhysicsCard>();

    [Header("Feasibility Weights")]
    [Tooltip("Weight for degrees difference (default 0.3)")]
    [Range(0f, 1f)]
    public float degreesWeight = 0.3f;

    [Tooltip("Weight for torque feasibility (default 0.3)")]
    [Range(0f, 1f)]
    public float torqueWeight = 0.3f;

    [Tooltip("Weight for force feasibility (default 0.2)")]
    [Range(0f, 1f)]
    public float forceWeight = 0.2f;

    [Tooltip("Weight for velocity change likelihood (default 0.2)")]
    [Range(0f, 1f)]
    public float velocityWeight = 0.2f;

    [Header("Radial Limits Sorting")]
    [Tooltip("Weight for radial limits comfort score (default 0.3)")]
    [Range(0f, 1f)]
    public float comfortWeight = 0.3f;

    [Tooltip("Weight for feasibility score when combined with comfort (default 0.7)")]
    [Range(0f, 1f)]
    public float feasibilityWeight = 0.7f;

    [Header("Walking Card Generation")]
    [Tooltip("Only allow leg muscle groups for walking cards (restricts arm/hand/head usage unless placement card)")]
    public bool onlyAllowLegsForWalking = true;

    [Tooltip("Keywords that indicate a placement/manipulation card (case-insensitive)")]
    public List<string> placementCardKeywords = new List<string> { "placement", "grasp", "hold", "tip", "manipulate", "grip", "pick", "place" };

    [Header("Flying Card (Wing / Jet)")]
    [Tooltip("Muscle group names that indicate a wing card (case-insensitive).")]
    public List<string> wingMuscleGroupKeywords = new List<string> { "wing", "wings", "leftwing", "rightwing" };

    [Tooltip("Muscle group names that indicate a jet card (case-insensitive).")]
    public List<string> jetMuscleGroupKeywords = new List<string> { "jet", "thrust" };

    // References
    private NervousSystem nervousSystem;
    private RagdollSystem ragdollSystem;
    private TemporalGraph temporalGraph;

    private void Awake()
    {
        // Get references
        nervousSystem = GetComponent<NervousSystem>();
        ragdollSystem = GetComponent<RagdollSystem>();

        if (nervousSystem != null)
        {
            temporalGraph = nervousSystem.GetTemporalGraph();
        }

        // Initialize default placement card keywords if empty
        if (placementCardKeywords == null || placementCardKeywords.Count == 0)
        {
            placementCardKeywords = new List<string> { "placement", "grasp", "hold", "tip", "manipulate", "grip", "pick", "place" };
        }
        if (wingMuscleGroupKeywords == null || wingMuscleGroupKeywords.Count == 0)
        {
            wingMuscleGroupKeywords = new List<string> { "wing", "wings", "leftwing", "rightwing" };
        }
        if (jetMuscleGroupKeywords == null || jetMuscleGroupKeywords.Count == 0)
        {
            jetMuscleGroupKeywords = new List<string> { "jet", "thrust" };
        }
    }

    /// <summary>
    /// Find applicable cards from current state for a target object.
    /// </summary>
    public List<PhysicsCard> FindApplicableCards(RagdollState state, GameObject target = null)
    {
        List<PhysicsCard> applicable = new List<PhysicsCard>();

        // Check all available cards
        foreach (var card in availableCards)
        {
            if (card != null && card.IsFeasible(state))
            {
                applicable.Add(card);
            }
        }

        // Query nervous system for additional cards
        if (nervousSystem != null)
        {
            var nervousCards = nervousSystem.GetAvailableGoodSections(target);
            foreach (var card in nervousCards)
            {
                if (card != null && !applicable.Contains(card) && card.IsFeasible(state))
                {
                    applicable.Add(card);
                }
            }
        }

        return applicable;
    }

    /// <summary>
    /// Order cards by feasibility score and radial limits comfort (highest first).
    /// </summary>
    public List<PhysicsCard> OrderCardsByFeasibility(List<PhysicsCard> cards, RagdollState state)
    {
        if (cards == null || state == null)
            return new List<PhysicsCard>();

        // Calculate combined scores (feasibility + comfort)
        var scoredCards = cards.Select(card => new
        {
            card = card,
            feasibilityScore = CalculateFeasibilityScore(card, state),
            comfortScore = CalculateComfortScore(card, state),
            totalLimitAngle = CalculateTotalLimitAngle(card, state)
        }).Select(x => new
        {
            x.card,
            combinedScore = (x.feasibilityScore * feasibilityWeight) + (x.comfortScore * comfortWeight)
        }).OrderByDescending(x => x.combinedScore)
          .ThenByDescending(x => x.card != null && x.card.limits != null ? CalculateTotalLimitAngle(x.card, state) : 0f)
          .ToList();

        // Return ordered list
        return scoredCards.Select(x => x.card).ToList();
    }

    /// <summary>
    /// Calculate feasibility score for a card given current state.
    /// </summary>
    public float CalculateFeasibilityScore(PhysicsCard card, RagdollState state)
    {
        if (card == null || state == null)
            return 0f;

        float score = 0f;

        // Degrees difference (30% weight, or custom weight)
        float degreesDiff = CalculateDegreesDifference(card, state);
        float degreesScore = 1f - Mathf.Clamp01(degreesDiff / 180f);
        score += degreesScore * degreesWeight;

        // Torque feasibility (30% weight)
        float torqueFeasibility = CheckTorqueFeasibility(card, state);
        score += torqueFeasibility * torqueWeight;

        // Force feasibility (20% weight)
        float forceFeasibility = CheckForceFeasibility(card, state);
        score += forceFeasibility * forceWeight;

        // Velocity change likelihood (20% weight)
        float velocityLikelihood = EstimateVelocityChangeLikelihood(card, state);
        score += velocityLikelihood * velocityWeight;

        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Solve for a goal (find card sequence that achieves the goal).
    /// </summary>
    public List<PhysicsCard> SolveForGoal(BehaviorTreeGoal goal, RagdollState state)
    {
        if (goal == null || state == null)
            return new List<PhysicsCard>();

        // Find applicable cards
        List<PhysicsCard> applicable = FindApplicableCards(state, goal.target);

        // Order by feasibility
        List<PhysicsCard> ordered = OrderCardsByFeasibility(applicable, state);

        // Try to find direct path to goal
        if (temporalGraph != null && ordered.Count > 0)
        {
            // Find card that matches goal type
            PhysicsCard goalCard = FindCardMatchingGoal(ordered, goal);
            if (goalCard != null)
            {
                // Find path to goal card
                return temporalGraph.FindPath(state, goalCard);
            }
        }

        // Fallback: return most feasible card
        if (ordered.Count > 0)
        {
            return new List<PhysicsCard> { ordered[0] };
        }

        return new List<PhysicsCard>();
    }

    /// <summary>
    /// Perform topological search (find path through graph from start to goal).
    /// </summary>
    public List<PhysicsCard> TopologicalSearch(PhysicsCard start, PhysicsCard goal)
    {
        if (start == null || goal == null || temporalGraph == null)
            return new List<PhysicsCard>();

        // Use temporal graph's pathfinding
        RagdollState currentState = ragdollSystem != null ? ragdollSystem.GetCurrentState() : new RagdollState();
        return temporalGraph.FindPath(currentState, goal);
    }

    /// <summary>
    /// Add cards to available pool.
    /// </summary>
    public void AddCards(List<PhysicsCard> cards)
    {
        if (cards == null)
            return;

        foreach (var card in cards)
        {
            if (card != null && !availableCards.Contains(card))
            {
                availableCards.Add(card);
            }
        }
    }

    /// <summary>
    /// Remove cards from available pool.
    /// </summary>
    public void RemoveCards(List<PhysicsCard> cards)
    {
        if (cards == null)
            return;

        foreach (var card in cards)
        {
            availableCards.Remove(card);
        }
    }

    /// <summary>
    /// Clear all available cards.
    /// </summary>
    public void ClearCards()
    {
        availableCards.Clear();
    }

    // Helper methods for feasibility scoring

    private float CalculateDegreesDifference(PhysicsCard card, RagdollState state)
    {
        if (card.requiredState == null)
            return 0f;

        return card.requiredState.CalculateDistance(state) * 180f;
    }

    private float CheckTorqueFeasibility(PhysicsCard card, RagdollState state)
    {
        if (card.limits == null || card.requiredState == null)
            return 1f;

        return card.limits.GetLimitScore(state, card.requiredState);
    }

    private float CheckForceFeasibility(PhysicsCard card, RagdollState state)
    {
        if (card.limits == null || card.requiredState == null)
            return 1f;

        // Similar to torque, but focused on force
        return card.limits.GetLimitScore(state, card.requiredState);
    }

    private float EstimateVelocityChangeLikelihood(PhysicsCard card, RagdollState state)
    {
        if (card.requiredState == null)
            return 1f;

        float velChange = (card.requiredState.rootVelocity - state.rootVelocity).magnitude;
        float maxVelChange = card.limits != null ? card.limits.maxVelocityChange : 10f;

        return 1f - Mathf.Clamp01(velChange / maxVelChange);
    }

    private PhysicsCard FindCardMatchingGoal(List<PhysicsCard> cards, BehaviorTreeGoal goal)
    {
        // Throw goals: prefer throw-only cards (throw at goal.targetPosition / goal.target)
        if (goal != null && goal.type == GoalType.Throw)
        {
            foreach (var card in cards)
            {
                if (card == null)
                    continue;
                if (card.needsToBeThrown && card.IsThrowGoalOnly())
                    return card;
            }
        }

        // Find card that matches goal type or name
        foreach (var card in cards)
        {
            if (card == null)
                continue;

            // Check if card name matches goal name
            if (!string.IsNullOrEmpty(card.sectionName) && 
                !string.IsNullOrEmpty(goal.goalName) &&
                card.sectionName.Contains(goal.goalName))
            {
                return card;
            }

            // Check if card's behavior tree matches goal type
            if (card.behaviorTree != null && goal.type != GoalType.Composite)
            {
                // This would require behavior tree matching logic
                // For now, return first feasible card
                return card;
            }
        }

        return cards.Count > 0 ? cards[0] : null;
    }

    /// <summary>
    /// Check if a card is a placement/manipulation card (not a walking gate animation).
    /// </summary>
    public bool IsPlacementCard(PhysicsCard card)
    {
        if (card == null)
            return false;

        // Check keyword matching (case-insensitive)
        if (!string.IsNullOrEmpty(card.sectionName) && placementCardKeywords != null)
        {
            string lowerName = card.sectionName.ToLowerInvariant();
            foreach (var keyword in placementCardKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) && lowerName.Contains(keyword.ToLowerInvariant()))
                {
                    return true;
                }
            }
        }

        // Check subclass type
        if (card is HemisphericalGraspCard || card is TippingCard)
        {
            return true;
        }

        // Heuristics: check if card has properties that indicate placement/manipulation
        // Cards with target objects are likely placement cards
        if (card is HemisphericalGraspCard graspCard && graspCard.targetObject != null)
        {
            return true;
        }
        if (card is TippingCard tippingCard && tippingCard.targetObject != null)
        {
            return true;
        }

        // Check if card has force/torque directions indicating manipulation
        if (card.impulseStack != null)
        {
            bool hasManipulationForces = false;
            foreach (var action in card.impulseStack)
            {
                if (action != null && (action.forceDirection.magnitude > 0.1f || action.torqueDirection.magnitude > 0.1f))
                {
                    hasManipulationForces = true;
                    break;
                }
            }
            if (hasManipulationForces)
            {
                // Check if it's NOT just leg movement (legs can have forces for walking)
                if (!IsLegOnlyCard(card))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a card only activates leg muscle groups.
    /// </summary>
    private bool IsLegOnlyCard(PhysicsCard card)
    {
        if (card == null || card.impulseStack == null)
            return false;

        // Leg muscle group keywords
        string[] legKeywords = { "hip", "knee", "ankle", "foot", "leg", "thigh", "calf", "toe" };
        string[] armKeywords = { "arm", "hand", "wrist", "elbow", "shoulder", "finger", "thumb" };
        string[] headKeywords = { "head", "neck", "jaw", "eye" };

        bool hasLegActivation = false;
        bool hasArmActivation = false;
        bool hasHeadActivation = false;

        foreach (var action in card.impulseStack)
        {
            if (action == null || string.IsNullOrEmpty(action.muscleGroup))
                continue;

            string lowerGroup = action.muscleGroup.ToLowerInvariant();

            // Check for leg activation
            foreach (var keyword in legKeywords)
            {
                if (lowerGroup.Contains(keyword))
                {
                    hasLegActivation = true;
                    break;
                }
            }

            // Check for arm activation
            foreach (var keyword in armKeywords)
            {
                if (lowerGroup.Contains(keyword))
                {
                    hasArmActivation = true;
                    break;
                }
            }

            // Check for head activation
            foreach (var keyword in headKeywords)
            {
                if (lowerGroup.Contains(keyword))
                {
                    hasHeadActivation = true;
                    break;
                }
            }
        }

        return hasLegActivation && !hasArmActivation && !hasHeadActivation;
    }

    /// <summary>
    /// Check if a card would preclude a walking gate animation.
    /// </summary>
    public bool IsWalkingGateAnimation(PhysicsCard card)
    {
        if (card == null)
            return false;

        // Placement cards don't preclude walking gate (they're separate actions)
        if (IsPlacementCard(card))
            return false;

        // Check if card activates arm/hand/head muscle groups
        if (card.impulseStack != null)
        {
            string[] armKeywords = { "arm", "hand", "wrist", "elbow", "shoulder", "finger", "thumb", "forearm", "upperarm" };
            string[] headKeywords = { "head", "neck", "jaw", "eye" };

            foreach (var action in card.impulseStack)
            {
                if (action == null || string.IsNullOrEmpty(action.muscleGroup))
                    continue;

                string lowerGroup = action.muscleGroup.ToLowerInvariant();

                // Check for arm/hand activation
                foreach (var keyword in armKeywords)
                {
                    if (lowerGroup.Contains(keyword))
                    {
                        return true; // Precludes walking gate
                    }
                }

                // Check for head activation
                foreach (var keyword in headKeywords)
                {
                    if (lowerGroup.Contains(keyword))
                    {
                        return true; // Precludes walking gate
                    }
                }
            }
        }

        // Card only uses leg/torso, so it's a walking gate animation
        return false;
    }

    /// <summary>
    /// Filter cards for walking based on "Only Allow Legs for Walking" option.
    /// </summary>
    public List<PhysicsCard> FilterCardsForWalking(List<PhysicsCard> cards)
    {
        if (cards == null)
            return new List<PhysicsCard>();

        if (!onlyAllowLegsForWalking)
        {
            return new List<PhysicsCard>(cards); // Return all cards unchanged
        }

        List<PhysicsCard> filtered = new List<PhysicsCard>();

        foreach (var card in cards)
        {
            if (card == null)
                continue;

            // Always keep placement cards
            if (IsPlacementCard(card))
            {
                filtered.Add(card);
                continue;
            }

            // Keep cards that only activate leg muscle groups
            if (IsLegOnlyCard(card))
            {
                filtered.Add(card);
                continue;
            }

            // Remove cards that activate arm/hand/head (unless placement card)
            // (Already handled by IsPlacementCard check above)
        }

        return filtered;
    }

    /// <summary>
    /// Get cards that use wing muscle groups (for flying goal lift/direction).
    /// </summary>
    public List<PhysicsCard> GetWingCards(List<PhysicsCard> cards)
    {
        if (cards == null) return new List<PhysicsCard>();
        List<PhysicsCard> outList = new List<PhysicsCard>();
        foreach (var card in cards)
        {
            if (card != null && IsWingCard(card))
                outList.Add(card);
        }
        return outList;
    }

    /// <summary>
    /// Get cards that use jet muscle groups (directional impulse).
    /// </summary>
    public List<PhysicsCard> GetJetCards(List<PhysicsCard> cards)
    {
        if (cards == null) return new List<PhysicsCard>();
        List<PhysicsCard> outList = new List<PhysicsCard>();
        foreach (var card in cards)
        {
            if (card != null && IsJetCard(card))
                outList.Add(card);
        }
        return outList;
    }

    /// <summary>
    /// Filter to cards that are usable for flying (wing or jet).
    /// </summary>
    public List<PhysicsCard> FilterCardsForFlying(List<PhysicsCard> cards)
    {
        if (cards == null) return new List<PhysicsCard>();
        List<PhysicsCard> outList = new List<PhysicsCard>();
        foreach (var card in cards)
        {
            if (card != null && (IsWingCard(card) || IsJetCard(card)))
                outList.Add(card);
        }
        return outList;
    }

    private bool IsWingCard(PhysicsCard card)
    {
        if (card?.impulseStack == null || wingMuscleGroupKeywords == null) return false;
        foreach (var action in card.impulseStack)
        {
            if (action == null || string.IsNullOrEmpty(action.muscleGroup)) continue;
            string lower = action.muscleGroup.ToLowerInvariant();
            foreach (var kw in wingMuscleGroupKeywords)
            {
                if (!string.IsNullOrEmpty(kw) && lower.Contains(kw.ToLowerInvariant()))
                    return true;
            }
        }
        return false;
    }

    private bool IsJetCard(PhysicsCard card)
    {
        if (card?.impulseStack == null || jetMuscleGroupKeywords == null) return false;
        foreach (var action in card.impulseStack)
        {
            if (action == null || string.IsNullOrEmpty(action.muscleGroup)) continue;
            string lower = action.muscleGroup.ToLowerInvariant();
            foreach (var kw in jetMuscleGroupKeywords)
            {
                if (!string.IsNullOrEmpty(kw) && lower.Contains(kw.ToLowerInvariant()))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Aggregate lift and direction from wing cards using config (wing AR and flap power).
    /// Returns (lift vector magnitude and up-component, direction from wing orientation/force).
    /// </summary>
    public void ComputeLiftAndDirectionFromWingCards(List<PhysicsCard> wingCards, FlyingCardConfig config, out Vector3 lift, out Vector3 direction)
    {
        lift = Vector3.zero;
        direction = Vector3.zero;
        if (wingCards == null || wingCards.Count == 0 || config == null) return;

        float ar = Mathf.Max(0.1f, config.wingAspectRatio);
        float power = config.flapPower;

        foreach (var card in wingCards)
        {
            if (card?.impulseStack == null) continue;
            foreach (var action in card.impulseStack)
            {
                if (action == null) continue;
                float mag = action.activation * power * Mathf.Sqrt(ar);
                Vector3 forceDir = action.forceDirection.sqrMagnitude > 0.01f ? action.forceDirection.normalized : Vector3.up;
                lift += forceDir * mag;
                direction += forceDir;
            }
        }

        if (direction.sqrMagnitude > 0.01f)
            direction.Normalize();
        else
            direction = Vector3.up;
    }

    /// <summary>
    /// Aggregate directional impulse from jet cards (forceDirection * activation).
    /// </summary>
    public Vector3 ComputeDirectionFromJetCards(List<PhysicsCard> jetCards)
    {
        Vector3 total = Vector3.zero;
        if (jetCards == null) return total;
        foreach (var card in jetCards)
        {
            if (card?.impulseStack == null) continue;
            foreach (var action in card.impulseStack)
            {
                if (action == null) continue;
                total += action.forceDirection * action.activation;
            }
        }
        return total.sqrMagnitude > 0.01f ? total.normalized : Vector3.forward;
    }

    /// <summary>
    /// Generate one flying card (wing or jet). Consumes fuel from fuelRemaining; returns null if not enough fuel.
    /// </summary>
    public PhysicsCard GenerateFlyingCard(Vector3 from, Vector3 to, RagdollState currentState, FlyingCardConfig config, bool useJetMode, ref float fuelRemaining)
    {
        if (config == null) return null;
        float cost = useJetMode ? config.fuelCostPerJetCard : config.fuelCostPerWingCard;
        if (fuelRemaining < cost) return null;

        Vector3 direction = (to - from).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.up;

        PhysicsCard card = new PhysicsCard
        {
            sectionName = useJetMode ? "auto_jet" : "auto_wing",
            description = $"Auto-generated {(useJetMode ? "jet" : "wing")} card from {from} to {to}",
            impulseStack = new List<ImpulseAction>(),
            requiredState = currentState?.CopyState(),
            targetState = new RagdollState(),
            limits = new SectionLimits { fuelCost = cost }
        };
        card.targetState.rootPosition = to;
        card.targetState.rootVelocity = direction * (useJetMode ? config.jetImpulseStrength : config.flapPower * 2f);
        card.targetState.rootRotation = Quaternion.LookRotation(direction);

        if (useJetMode)
        {
            card.impulseStack.Add(new ImpulseAction
            {
                muscleGroup = "Jet",
                activation = 0.8f,
                duration = 0.2f,
                forceDirection = direction * config.jetImpulseStrength
            });
        }
        else
        {
            var wingGroups = (wingMuscleGroupKeywords != null && wingMuscleGroupKeywords.Count > 0)
                ? wingMuscleGroupKeywords
                : new List<string> { "Wings" };
            foreach (var kw in wingGroups)
            {
                if (string.IsNullOrEmpty(kw)) continue;
                card.impulseStack.Add(new ImpulseAction
                {
                    muscleGroup = kw,
                    activation = config.flapPower,
                    duration = 0.15f,
                    forceDirection = direction * Mathf.Sqrt(config.wingAspectRatio)
                });
            }
        }

        fuelRemaining -= cost;
        return card;
    }

    /// <summary>
    /// Auto-generate a kinematic walking card for movement from one position to another.
    /// </summary>
    public PhysicsCard GenerateWalkingCard(Vector3 from, Vector3 to, RagdollState currentState)
    {
        if (ragdollSystem == null)
            return null;

        // Calculate direction and distance
        Vector3 direction = (to - from);
        direction.y = 0f; // Keep movement on horizontal plane
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return null; // Already at destination

        direction.Normalize();

        // Create walking card
        PhysicsCard walkingCard = new PhysicsCard
        {
            sectionName = "auto_walking",
            description = $"Auto-generated walking card from {from} to {to}",
            impulseStack = new List<ImpulseAction>(),
            requiredState = currentState.CopyState(),
            targetState = new RagdollState(),
            limits = new SectionLimits()
        };

        // Set target state
        walkingCard.targetState.rootPosition = to;
        walkingCard.targetState.rootVelocity = direction * 2f; // Walking speed
        walkingCard.targetState.rootRotation = Quaternion.LookRotation(direction);

        // Generate impulse actions for leg muscle groups
        // Common leg muscle group names (will work with various naming conventions)
        string[] legMuscleGroups = { "hip", "knee", "ankle", "foot", "leg", "thigh", "calf" };

        // Create impulse actions for walking motion
        // Left leg
        foreach (var groupName in legMuscleGroups)
        {
            ImpulseAction leftAction = new ImpulseAction
            {
                muscleGroup = $"left_{groupName}",
                activation = 0.6f,
                duration = distance / 2f, // Duration based on distance
                curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                forceDirection = direction * 0.5f
            };
            walkingCard.impulseStack.Add(leftAction);
        }

        // Right leg (alternating pattern)
        foreach (var groupName in legMuscleGroups)
        {
            ImpulseAction rightAction = new ImpulseAction
            {
                muscleGroup = $"right_{groupName}",
                activation = 0.6f,
                duration = distance / 2f,
                curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                forceDirection = direction * 0.5f
            };
            walkingCard.impulseStack.Add(rightAction);
        }

        // Add torso stabilization
        ImpulseAction torsoAction = new ImpulseAction
        {
            muscleGroup = "torso",
            activation = 0.3f,
            duration = distance / 2f,
            curve = AnimationCurve.Linear(0f, 1f, 1f, 1f)
        };
        walkingCard.impulseStack.Add(torsoAction);

        // todo: note: we may want to use the path finding component
        //   and make ray casts down along foot falls on the path, then apply forces to the ground
        //     or generate additional placement cards at the foot falls - this would allow us to
        //     break the covenent for walking cards to only use leg muscle groups if the player character
        // needs to squeeze through an opening, or climb a ladder, or etc.
        // since we know the gate length, we can generate "foot accessible" chords along the path
        return walkingCard;
    }

    /// <summary>
    /// Calculate total limit angle for all joints involved in a card.
    /// Greater total limit angle = more comfortable/better fit.
    /// </summary>
    public float CalculateTotalLimitAngle(PhysicsCard card, RagdollState state)
    {
        if (card == null || card.limits == null || !card.limits.useRadialLimits)
            return 0f;

        float totalAngle = 0f;

        // Calculate angle range for each axis
        Vector3 lower = card.limits.lowerRadialReferenceRotation;
        Vector3 upper = card.limits.upperRadialReferenceRotation;

        // X axis range
        float xRange = Mathf.Abs(upper.x - lower.x);
        if (xRange > 180f) xRange = 360f - xRange; // Handle wraparound
        totalAngle += xRange;

        // Y axis range
        float yRange = Mathf.Abs(upper.y - lower.y);
        if (yRange > 180f) yRange = 360f - yRange;
        totalAngle += yRange;

        // Z axis range
        float zRange = Mathf.Abs(upper.z - lower.z);
        if (zRange > 180f) zRange = 360f - zRange;
        totalAngle += zRange;

        return totalAngle;
    }

    /// <summary>
    /// Calculate comfort score based on radial limits (0-1).
    /// Higher score = better fit within limits.
    /// </summary>
    public float CalculateComfortScore(PhysicsCard card, RagdollState state)
    {
        if (card == null || card.limits == null || state == null)
            return 0f;

        // Use SectionLimits' GetRadialLimitScore method
        if (card.limits.useRadialLimits)
        {
            return card.limits.GetRadialLimitScore(state);
        }

        // If no radial limits, return neutral score
        return 0.5f;
    }

    /// <summary>
    /// Sort cards by radial limits (total limit angle) for best fit and comfort.
    /// </summary>
    public List<PhysicsCard> SortCardsByRadialLimits(List<PhysicsCard> cards, RagdollState state)
    {
        if (cards == null || state == null)
            return new List<PhysicsCard>();

        return cards.OrderByDescending(card => CalculateTotalLimitAngle(card, state))
                   .ThenByDescending(card => CalculateComfortScore(card, state))
                   .ToList();
    }
}
