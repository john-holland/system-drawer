using UnityEngine;

/// <summary>
/// Result of lie / trust evaluation before merging an incoming <see cref="ThoughtType.Decision"/>.
/// </summary>
public struct LieEvaluation
{
    public bool shouldMisrepresent;
    [Range(0f, 1f)] public float confidence;
    public string reason;

    public static LieEvaluation Honest(string why = "accepted")
    {
        return new LieEvaluation { shouldMisrepresent = false, confidence = 1f, reason = why };
    }

    public static LieEvaluation Misrepresent(float confidence, string why)
    {
        return new LieEvaluation { shouldMisrepresent = true, confidence = Mathf.Clamp01(confidence), reason = why };
    }
}

/// <summary>
/// Evaluates whether an actor should accept or misrepresent an incoming decision (trust / manipulation).
/// </summary>
public interface ILiePolicy
{
    LieEvaluation Evaluate(Brain receiver, ThoughtData incomingDecision, LSTMPredictor lstmOrNull);
}
