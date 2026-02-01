using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates climbing-specific physics cards for transitions between grab holds and preposition placements.
/// Handles: grab → reposition → climb → preposition
/// </summary>
public class ClimbingCardGenerator : MonoBehaviour
{
    [Header("Card Generation Settings")]
    [Tooltip("Grounding points required for stability")]
    public int requiredGroundingPoints = 2;

    [Tooltip("Hand grasp vs body grasp preference (0 = hand only, 1 = body only)")]
    [Range(0f, 1f)]
    public float handGraspPreference = 0.7f;

    [Tooltip("Total required muscle units (MU) for climbing")]
    public float totalRequiredMU = 50f;

    [Tooltip("Use animation mode (ignores physics beyond collision, animates ragdoll)")]
    public bool useAnimationMode = false;

    [Header("Swing Card Settings")]
    [Tooltip("Generate swing cards that calculate trajectory")]
    public bool generateSwingCards = true;

    [Tooltip("Fully release ragdoll during swing")]
    public bool fullyReleaseRagdollOnSwing = true;

    [Tooltip("Include catching (both physics implied and glue)")]
    public bool includeCatching = true;

    private PhysicsCardSolver cardSolver;
    private SurfaceGrabDetector grabDetector;

    private void Awake()
    {
        cardSolver = GetComponent<PhysicsCardSolver>();
        grabDetector = GetComponent<SurfaceGrabDetector>();
        if (grabDetector == null)
        {
            grabDetector = FindAnyObjectByType<SurfaceGrabDetector>();
        }
    }

    /// <summary>
    /// Generate cards for a climbing path from grab hold to preposition.
    /// </summary>
    public List<GoodSection> GenerateClimbingCards(
        SurfaceGrabDetector.GrabHold grabHold,
        Vector3 prepositionPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        List<GoodSection> cards = new List<GoodSection>();

        if (grabHold == null || !grabHold.isGrabbable)
        {
            Debug.LogWarning("[ClimbingCardGenerator] Invalid grab hold");
            return cards;
        }

        // Generate sequence of cards:
        // 1. Grab card - reach and grab the hold
        GoodSection grabCard = GenerateGrabCard(grabHold, currentState, ragdollActor);
        if (grabCard != null)
        {
            cards.Add(grabCard);
        }

        // 2. Reposition card - adjust body position for climbing
        GoodSection repositionCard = GenerateRepositionCard(grabHold, currentState, ragdollActor);
        if (repositionCard != null)
        {
            cards.Add(repositionCard);
        }

        // 3. Climb cards - intermediate climbing steps
        List<GoodSection> climbCards = GenerateClimbCards(grabHold, prepositionPosition, currentState, ragdollActor);
        cards.AddRange(climbCards);

        // 4. Preposition card - final positioning
        GoodSection prepositionCard = GeneratePrepositionCard(prepositionPosition, currentState, ragdollActor);
        if (prepositionCard != null)
        {
            cards.Add(prepositionCard);
        }

        // Connect cards in sequence
        ConnectCardsInSequence(cards);

        return cards;
    }

    /// <summary>
    /// Generate a grab card for reaching and grabbing a hold.
    /// </summary>
    private GoodSection GenerateGrabCard(
        SurfaceGrabDetector.GrabHold grabHold,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        GoodSection card = new GoodSection
        {
            sectionName = "Grab_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
            description = $"Grab hold at {grabHold.position}"
        };

        // Create impulse actions for grabbing
        // This would typically involve hand/arm muscle activations
        // Using reflection to create ImpulseAction if needed
        var impulseActionType = System.Type.GetType("ImpulseAction, Assembly-CSharp");
        if (impulseActionType == null)
        {
            impulseActionType = System.Type.GetType("ImpulseAction, Locomotion.Runtime");
        }

        // For now, create a basic card structure
        card.requiredState = currentState?.CopyState();
        card.targetState = currentState?.CopyState();
        if (card.targetState != null)
        {
            // Adjust target state for grab position
            card.targetState.rootPosition = grabHold.position;
        }

        card.limits = new SectionLimits
        {
            maxForce = 300f,
            maxTorque = 80f,
            maxVelocityChange = 3f
        };

        return card;
    }

    /// <summary>
    /// Generate a reposition card for adjusting body position.
    /// </summary>
    private GoodSection GenerateRepositionCard(
        SurfaceGrabDetector.GrabHold grabHold,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        GoodSection card = new GoodSection
        {
            sectionName = "Reposition_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
            description = "Reposition body for climbing"
        };

        card.requiredState = currentState?.CopyState();
        card.targetState = currentState?.CopyState();
        
        card.limits = new SectionLimits
        {
            maxForce = 200f,
            maxTorque = 60f,
            maxVelocityChange = 2f
        };

        return card;
    }

    /// <summary>
    /// Generate intermediate climb cards between grab and preposition.
    /// </summary>
    private List<GoodSection> GenerateClimbCards(
        SurfaceGrabDetector.GrabHold grabHold,
        Vector3 prepositionPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        List<GoodSection> cards = new List<GoodSection>();

        // Calculate intermediate positions
        Vector3 startPos = grabHold.position;
        Vector3 endPos = prepositionPosition;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        // Generate cards for intermediate steps
        int numSteps = Mathf.CeilToInt(distance / 0.5f); // One card per 0.5 units
        numSteps = Mathf.Max(1, Mathf.Min(numSteps, 10)); // Limit to 10 steps

        for (int i = 1; i < numSteps; i++)
        {
            float t = i / (float)numSteps;
            Vector3 intermediatePos = Vector3.Lerp(startPos, endPos, t);

            GoodSection climbCard = new GoodSection
            {
                sectionName = "Climb_" + i + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6),
                description = $"Climb step {i} of {numSteps}"
            };

            climbCard.requiredState = currentState?.CopyState();
            climbCard.targetState = currentState?.CopyState();
            if (climbCard.targetState != null)
            {
                climbCard.targetState.rootPosition = intermediatePos;
            }

            climbCard.limits = new SectionLimits
            {
                maxForce = 400f,
                maxTorque = 100f,
                maxVelocityChange = 4f
            };

            cards.Add(climbCard);
        }

        // Add swing cards if enabled
        if (generateSwingCards)
        {
            GoodSection swingCard = GenerateSwingCard(grabHold, prepositionPosition, currentState, ragdollActor);
            if (swingCard != null)
            {
                cards.Add(swingCard);
            }
        }

        return cards;
    }

    /// <summary>
    /// Generate a swing card that calculates trajectory and releases ragdoll.
    /// </summary>
    private GoodSection GenerateSwingCard(
        SurfaceGrabDetector.GrabHold grabHold,
        Vector3 targetPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        GoodSection card = new GoodSection
        {
            sectionName = "Swing_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
            description = "Swing from grab hold to target with trajectory calculation"
        };

        // Calculate swing trajectory
        Vector3 startPos = grabHold.position;
        Vector3 endPos = targetPosition;
        Vector3 gravity = Physics.gravity;

        // Simple parabolic trajectory calculation
        float timeToTarget = CalculateSwingTime(startPos, endPos, gravity);
        Vector3 initialVelocity = CalculateInitialVelocity(startPos, endPos, timeToTarget, gravity);

        card.requiredState = currentState?.CopyState();
        card.targetState = currentState?.CopyState();
        if (card.targetState != null)
        {
            card.targetState.rootPosition = endPos;
            card.targetState.rootVelocity = initialVelocity;
        }

        card.limits = new SectionLimits
        {
            maxForce = 0f, // No force during swing if fully released
            maxTorque = 0f,
            maxVelocityChange = initialVelocity.magnitude
        };

        // Mark card for full ragdoll release
        if (fullyReleaseRagdollOnSwing)
        {
            // This would be handled by the card execution system
            // to release all constraints
        }

        // Add catching if enabled
        if (includeCatching)
        {
            // Catching would be handled by a follow-up card or the execution system
        }

        return card;
    }

    /// <summary>
    /// Calculate swing time for trajectory.
    /// </summary>
    private float CalculateSwingTime(Vector3 start, Vector3 end, Vector3 gravity)
    {
        float heightDiff = end.y - start.y;
        float horizontalDist = Vector3.Distance(
            new Vector3(start.x, 0, start.z),
            new Vector3(end.x, 0, end.z)
        );

        // Simple estimation
        float time = Mathf.Sqrt(2f * Mathf.Abs(heightDiff) / Mathf.Abs(gravity.y));
        if (time < 0.1f) time = 0.5f; // Minimum time

        return time;
    }

    /// <summary>
    /// Calculate initial velocity for trajectory.
    /// </summary>
    private Vector3 CalculateInitialVelocity(Vector3 start, Vector3 end, float time, Vector3 gravity)
    {
        Vector3 displacement = end - start;
        Vector3 velocity = displacement / time - 0.5f * gravity * time;
        return velocity;
    }

    /// <summary>
    /// Generate preposition card for final positioning.
    /// </summary>
    private GoodSection GeneratePrepositionCard(
        Vector3 prepositionPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        GoodSection card = new GoodSection
        {
            sectionName = "Preposition_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
            description = $"Final preposition at {prepositionPosition}"
        };

        card.requiredState = currentState?.CopyState();
        card.targetState = currentState?.CopyState();
        if (card.targetState != null)
        {
            card.targetState.rootPosition = prepositionPosition;
            card.targetState.rootVelocity = Vector3.zero; // Come to rest
        }

        card.limits = new SectionLimits
        {
            maxForce = 500f,
            maxTorque = 120f,
            maxVelocityChange = 5f
        };

        return card;
    }

    /// <summary>
    /// Connect cards in sequence.
    /// </summary>
    private void ConnectCardsInSequence(List<GoodSection> cards)
    {
        for (int i = 0; i < cards.Count - 1; i++)
        {
            cards[i].AddConnectedSection(cards[i + 1]);
        }
    }

    /// <summary>
    /// Generate cards with animation mode (ignores physics beyond collision).
    /// </summary>
    public List<GoodSection> GenerateAnimationModeCards(
        SurfaceGrabDetector.GrabHold grabHold,
        Vector3 prepositionPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        // Animation mode cards would use kinematic keyframe animation
        // This would integrate with AnimationBehaviorTree system
        List<GoodSection> cards = GenerateClimbingCards(grabHold, prepositionPosition, currentState, ragdollActor);

        // Mark cards for animation mode
        foreach (var card in cards)
        {
            // This would be handled by a flag or metadata on the card
            // For now, we'll use the description to mark it
            card.description += " [Animation Mode]";
        }

        return cards;
    }
}
