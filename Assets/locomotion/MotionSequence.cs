using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Motion sequence data for LSTM training.
/// Contains states, cards, muscle activations, and success/failure.
/// </summary>
[System.Serializable]
public class MotionSequence
{
    [Header("Sequence Data")]
    [Tooltip("States in this sequence")]
    public List<RagdollState> states = new List<RagdollState>();

    [Tooltip("Cards executed in this sequence")]
    public List<GoodSection> cards = new List<GoodSection>();

    [Tooltip("Muscle activations throughout sequence")]
    public List<Dictionary<string, float>> muscleActivations = new List<Dictionary<string, float>>();

    [Header("Outcome")]
    [Tooltip("Was this sequence successful?")]
    public bool success = false;

    [Tooltip("Timestamp when sequence was created")]
    public float timestamp;

    public MotionSequence()
    {
        timestamp = Time.time;
    }

    /// <summary>
    /// Add a state to this sequence.
    /// </summary>
    public void AddState(RagdollState state)
    {
        if (state != null)
        {
            states.Add(state.CopyState());
        }
    }

    /// <summary>
    /// Add a card to this sequence.
    /// </summary>
    public void AddCard(GoodSection card)
    {
        if (card != null && !cards.Contains(card))
        {
            cards.Add(card);
        }
    }

    /// <summary>
    /// Get the complete sequence.
    /// </summary>
    public MotionSequence GetSequence()
    {
        return this;
    }
}
