using UnityEngine;

/// <summary>
/// Combines LSTM confidence, MVP string similarity, and an <see cref="AnimationCurve"/> acceptance band.
/// </summary>
public class CurveLiePolicy : MonoBehaviour, ILiePolicy
{
    [Tooltip("Weight of LSTM-derived score vs name similarity (0 = similarity only, 1 = LSTM only).")]
    [Range(0f, 1f)]
    public float lstmWeight = 0.45f;

    [Tooltip("Maps combined score [0,1] → acceptance; values above threshold imply honest merge.")]
    public AnimationCurve acceptanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("If evaluation sample is below this output, treat as misrepresent.")]
    [Range(0f, 1f)]
    public float honestyThreshold = 0.35f;

    public LieEvaluation Evaluate(Brain receiver, ThoughtData incomingDecision, LSTMPredictor lstmOrNull)
    {
        if (incomingDecision == null || incomingDecision.messageType != ThoughtType.Decision)
            return LieEvaluation.Honest("not_decision");

        float lstmScore = lstmOrNull != null ? lstmOrNull.EvaluateThoughtConsistencyScore(incomingDecision, receiver) : 0.5f;
        string[] tags = null;
        if (incomingDecision.data is DecisionThoughtPayload d)
            tags = d.semanticTags;

        float nameSim = ThoughtSimilarityMvp.ScoreNameOverlap(tags, receiver, incomingDecision);
        float combined = Mathf.Lerp(nameSim, lstmScore, lstmWeight);

        float curveVal = acceptanceCurve != null ? acceptanceCurve.Evaluate(combined) : combined;
        bool honestEnough = curveVal >= honestyThreshold;

        if (honestEnough)
            return LieEvaluation.Honest($"curve={curveVal:F2}");

        return LieEvaluation.Misrepresent(1f - curveVal, $"curve={curveVal:F2}_below_{honestyThreshold}");
    }
}
