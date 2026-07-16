using UnityEngine;

/// <summary>Estimates P(success) for nightstick rail deflection given fatigue/adrenaline and surface.</summary>
public static class RailDeflectionSuccessEstimator
{
    public struct Input
    {
        public float remainingStairDepthNormalized;
        public float railingFriction;
        public float railingMassHint;
        public float nightstickImpulse;
        public float fatigue01;
        public float adrenaline01;
        public float strength01;
    }

    public struct Result
    {
        public float probability;
        public float suggestedActivation;
        public bool likelySuccess;
    }

    public static Result Estimate(Input input)
    {
        float strength = Mathf.Clamp01(input.strength01 * (1f - 0.55f * Mathf.Clamp01(input.fatigue01)) *
                                      (1f + 0.45f * Mathf.Clamp01(input.adrenaline01)));
        float depthEase = 1f - 0.35f * Mathf.Clamp01(input.remainingStairDepthNormalized);
        float frictionPenalty = Mathf.Clamp01(input.railingFriction) * 0.25f;
        float massPenalty = Mathf.Clamp01(input.railingMassHint / 80f) * 0.2f;
        float impulseBoost = Mathf.Clamp01(input.nightstickImpulse / 25f) * 0.3f;

        float p = strength * depthEase + impulseBoost - frictionPenalty - massPenalty;
        p = Mathf.Clamp01(p);
        float activation = Mathf.Clamp01(0.45f + (1f - p) * 0.5f + input.fatigue01 * 0.2f);
        return new Result
        {
            probability = p,
            suggestedActivation = activation,
            likelySuccess = p >= 0.45f
        };
    }
}
