using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior tree node for moving to a specific waypoint.
/// Queries PhysicsCardSolver for movement cards, executes them, and falls back to impulses if needed.
/// </summary>
public class MoveToWaypointNode : BehaviorTreeNode
{
    [Header("Waypoint Properties")]
    [Tooltip("Target waypoint position")]
    public Vector3 waypoint;

    [Tooltip("Distance threshold for waypoint completion")]
    public float reachedDistance = 0.5f;

    [Header("References")]
    [Tooltip("Reference to PhysicsCardSolver (auto-found if null)")]
    public PhysicsCardSolver cardSolver;

    [Tooltip("Reference to NervousSystem (auto-found if null)")]
    public NervousSystem nervousSystem;

    [Tooltip("Reference to RagdollSystem (auto-found if null)")]
    public RagdollSystem ragdollSystem;

    [Tooltip("Optional proxy system for impulse fallback")]
    public MonoBehaviour fallbackImpulseProxy;

    [Header("Execution Settings")]
    [Tooltip("Timeout for waypoint movement (0 = no timeout)")]
    public float timeoutSeconds = 10f;

    [Tooltip("Use auto-generated walking cards if no cards available")]
    public bool useAutoWalkingCards = true;

    // Execution state
    private GoodSection currentCard;
    private bool isExecutingCard = false;
    private bool isUsingImpulseFallback = false;
    private float executionStartTime = 0f;
    private Vector3 lastPosition;
    private float stuckTime = 0f;
    private const float STUCK_THRESHOLD = 0.1f; // Meters
    private const float STUCK_TIME_LIMIT = 2f; // Seconds

    private void Awake()
    {
        nodeType = NodeType.Action;

        // Auto-find references if not assigned
        if (cardSolver == null)
        {
            cardSolver = GetComponentInParent<PhysicsCardSolver>();
        }
        if (nervousSystem == null)
        {
            nervousSystem = GetComponentInParent<NervousSystem>();
        }
        if (ragdollSystem == null)
        {
            ragdollSystem = GetComponentInParent<RagdollSystem>();
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        // Check if already at waypoint
        if (IsWaypointReached())
        {
            return BehaviorTreeStatus.Success;
        }

        // Reset execution state
        isExecutingCard = false;
        isUsingImpulseFallback = false;
        executionStartTime = Time.time;
        stuckTime = 0f;

        if (ragdollSystem != null)
        {
            lastPosition = ragdollSystem.GetCurrentState().rootPosition;
        }

        // Try to execute cards
        if (TryExecuteCards(tree))
        {
            isExecutingCard = true;
            return BehaviorTreeStatus.Running;
        }

        // Fallback to impulses
        if (TryImpulseFallback(tree))
        {
            isUsingImpulseFallback = true;
            return BehaviorTreeStatus.Running;
        }

        // No movement method available
        return BehaviorTreeStatus.Failure;
    }

    public override void OnUpdate(BehaviorTree tree)
    {
        // Check if waypoint reached
        if (IsWaypointReached())
        {
            StopExecution();
            return;
        }

        // Check timeout
        if (timeoutSeconds > 0f && Time.time - executionStartTime > timeoutSeconds)
        {
            StopExecution();
            return;
        }

        // Check if stuck
        if (ragdollSystem != null)
        {
            Vector3 currentPos = ragdollSystem.GetCurrentState().rootPosition;
            float distanceMoved = Vector3.Distance(currentPos, lastPosition);
            
            if (distanceMoved < STUCK_THRESHOLD)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime > STUCK_TIME_LIMIT)
                {
                    // Try to recover by switching to impulse fallback
                    if (isExecutingCard && !isUsingImpulseFallback)
                    {
                        StopExecution();
                        if (TryImpulseFallback(tree))
                        {
                            isUsingImpulseFallback = true;
                            isExecutingCard = false;
                        }
                    }
                }
            }
            else
            {
                stuckTime = 0f;
                lastPosition = currentPos;
            }
        }

        // Update card execution
        if (isExecutingCard && currentCard != null && ragdollSystem != null)
        {
            RagdollState currentState = ragdollSystem.GetCurrentState();
            bool stillExecuting = currentCard.Update(currentState, Time.deltaTime);
            
            if (!stillExecuting)
            {
                // Card execution complete, check if we need another card
                isExecutingCard = false;
                currentCard = null;
                
                // Try to get next card or fallback
                if (!TryExecuteCards(tree))
                {
                    if (!TryImpulseFallback(tree))
                    {
                        // No more movement options
                        StopExecution();
                    }
                    else
                    {
                        isUsingImpulseFallback = true;
                    }
                }
                else
                {
                    isExecutingCard = true;
                }
            }
        }
    }

    /// <summary>
    /// Try to execute movement cards.
    /// </summary>
    private bool TryExecuteCards(BehaviorTree tree)
    {
        if (cardSolver == null || ragdollSystem == null)
            return false;

        RagdollState currentState = ragdollSystem.GetCurrentState();

        // Find applicable cards
        List<GoodSection> applicableCards = cardSolver.FindApplicableCards(currentState);

        // Filter for walking if enabled
        if (cardSolver.onlyAllowLegsForWalking)
        {
            applicableCards = cardSolver.FilterCardsForWalking(applicableCards);
        }

        // Try to find a card that moves toward waypoint
        GoodSection bestCard = null;
        float bestScore = float.MinValue;

        foreach (var card in applicableCards)
        {
            if (card == null || !card.IsFeasible(currentState))
                continue;

            // Score card based on how well it moves toward waypoint
            float score = ScoreCardForWaypoint(card, currentState);
            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }

        // If no cards found and auto-generation enabled, generate walking card
        if (bestCard == null && useAutoWalkingCards)
        {
            bestCard = cardSolver.GenerateWalkingCard(currentState.rootPosition, waypoint, currentState);
            if (bestCard != null)
            {
                // Add generated card to available cards temporarily
                if (!cardSolver.availableCards.Contains(bestCard))
                {
                    cardSolver.availableCards.Add(bestCard);
                }
            }
        }

        // Execute best card
        if (bestCard != null)
        {
            currentCard = bestCard;
            currentCard.Execute(currentState);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Score a card based on how well it moves toward the waypoint.
    /// </summary>
    private float ScoreCardForWaypoint(GoodSection card, RagdollState currentState)
    {
        if (card == null || card.targetState == null)
            return 0f;

        Vector3 currentPos = currentState.rootPosition;
        Vector3 targetPos = card.targetState.rootPosition;
        Vector3 waypointDir = (waypoint - currentPos).normalized;
        Vector3 cardDir = (targetPos - currentPos).normalized;

        // Score based on direction alignment
        float alignment = Vector3.Dot(waypointDir, cardDir);
        
        // Score based on distance to waypoint (closer is better)
        float distanceToWaypoint = Vector3.Distance(targetPos, waypoint);
        float distanceScore = 1f / (1f + distanceToWaypoint);

        // Combined score
        return alignment * 0.7f + distanceScore * 0.3f;
    }

    /// <summary>
    /// Try impulse fallback for movement.
    /// </summary>
    private bool TryImpulseFallback(BehaviorTree tree)
    {
        if (nervousSystem == null || ragdollSystem == null)
            return false;

        RagdollState currentState = ragdollSystem.GetCurrentState();
        Vector3 currentPos = currentState.rootPosition;
        Vector3 direction = (waypoint - currentPos);
        direction.y = 0f; // Keep on horizontal plane
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return false; // Already at waypoint

        direction.Normalize();

        // Create motor data for movement
        MotorData motorData = new MotorData(
            "legs", // muscle group
            Mathf.Clamp01(distance / 5f), // activation strength
            0.5f, // duration
            null // curve
        );
        motorData.forceDirection = direction;

        // Create impulse data for movement
        ImpulseData impulseData = new ImpulseData(
            ImpulseType.Motor,
            "PathfindingNode",
            "Limb",
            motorData,
            0
        );

        // Send impulse down to limb channel
        nervousSystem.SendImpulseDown("Limb", impulseData);

        // If proxy system available, also use it
        if (fallbackImpulseProxy != null)
        {
            // Try to call a method on the proxy if it has one
            var proxyType = fallbackImpulseProxy.GetType();
            var method = proxyType.GetMethod("HandleImpulse", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(fallbackImpulseProxy, new object[] { impulseData });
            }
        }

        return true;
    }

    /// <summary>
    /// Check if waypoint has been reached.
    /// </summary>
    private bool IsWaypointReached()
    {
        if (ragdollSystem == null)
            return false;

        RagdollState currentState = ragdollSystem.GetCurrentState();
        Vector3 currentPos = currentState.rootPosition;
        float distance = Vector3.Distance(currentPos, waypoint);

        return distance <= reachedDistance;
    }

    /// <summary>
    /// Stop current execution.
    /// </summary>
    private void StopExecution()
    {
        if (currentCard != null && isExecutingCard)
        {
            currentCard.Stop();
            currentCard = null;
        }
        isExecutingCard = false;
        isUsingImpulseFallback = false;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        // Reset state
        isExecutingCard = false;
        isUsingImpulseFallback = false;
        currentCard = null;
        executionStartTime = Time.time;
        stuckTime = 0f;
    }

    public override void OnExit(BehaviorTree tree)
    {
        StopExecution();
    }
}
