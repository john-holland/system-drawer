using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a sequence of flying cards along a 3D curve (FlyingGoal) trajectory.
/// Uses wing/jet cards from the solver and fuel limits; generates cards when needed.
/// </summary>
public class FlyingCardSequenceGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Card solver for wing/jet filters and GenerateFlyingCard.")]
    public PhysicsCardSolver cardSolver;

    [Tooltip("Flying card config (wing AR, flap power, fuel costs).")]
    public FlyingCardConfig flyingConfig;

    [Header("Flying Goal")]
    [Tooltip("3D curve trajectory (X, Y, Z over normalized time). Edit in inspector with the curve drawer.")]
    public FlyingGoal flyingGoal;

    [Header("Generation")]
    [Tooltip("Number of samples along the trajectory (segments = samples - 1).")]
    public int trajectorySamples = 16;

    [Tooltip("Use jet mode when true; otherwise wing mode. Jet consumes more fuel.")]
    public bool useJetMode = false;

    [Tooltip("Starting fuel (from config if 0).")]
    public float initialFuel = 0f;

    /// <summary>
    /// Generate a sequence of flying cards along the flying goal trajectory.
    /// Respects fuel: stops when fuel would go negative.
    /// </summary>
    public List<GoodSection> GenerateSequence(RagdollState currentState)
    {
        List<GoodSection> sequence = new List<GoodSection>();
        if (cardSolver == null || flyingConfig == null)
            return sequence;

        float fuel = initialFuel > 0f ? initialFuel : flyingConfig.fuelCapacity;
        int n = Mathf.Max(2, trajectorySamples);
        Vector3 prev = flyingGoal.PositionAt(0f);
        if (currentState != null)
            prev = currentState.rootPosition;

        for (int i = 1; i < n; i++)
        {
            float t = (float)i / (n - 1);
            Vector3 next = flyingGoal.PositionAt(t);
            GoodSection card = cardSolver.GenerateFlyingCard(prev, next, currentState, flyingConfig, useJetMode, ref fuel);
            if (card == null)
                break;
            sequence.Add(card);
            prev = next;
            if (currentState != null)
            {
                currentState = currentState.CopyState();
                currentState.rootPosition = next;
            }
        }

        return sequence;
    }

    /// <summary>
    /// Generate sequence using an explicit goal and optional config/state.
    /// </summary>
    public static List<GoodSection> GenerateSequence(PhysicsCardSolver solver, FlyingCardConfig config, FlyingGoal goal, int samples, bool jetMode, RagdollState state, float fuelCapacity)
    {
        if (solver == null || config == null)
            return new List<GoodSection>();

        float fuel = fuelCapacity > 0f ? fuelCapacity : config.fuelCapacity;
        int n = Mathf.Max(2, samples);
        Vector3 prev = goal.PositionAt(0f);
        if (state != null)
            prev = state.rootPosition;

        var sequence = new List<GoodSection>();
        for (int i = 1; i < n; i++)
        {
            float t = (float)i / (n - 1);
            Vector3 next = goal.PositionAt(t);
            GoodSection card = solver.GenerateFlyingCard(prev, next, state, config, jetMode, ref fuel);
            if (card == null)
                break;
            sequence.Add(card);
            prev = next;
            if (state != null)
            {
                state = state.CopyState();
                state.rootPosition = next;
            }
        }
        return sequence;
    }
}
