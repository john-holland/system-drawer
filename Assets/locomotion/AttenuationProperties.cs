using UnityEngine;

/// <summary>
/// Animation attenuation settings extracted from animation.
/// Controls how animation curves are applied to physics cards.
/// </summary>
[System.Serializable]
public class AttenuationProperties
{
    [Header("Attenuation Curves")]
    [Tooltip("Position attenuation over time")]
    public AnimationCurve positionAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Rotation attenuation over time")]
    public AnimationCurve rotationAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Scale attenuation over time")]
    public AnimationCurve scaleAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Muscle activation curve")]
    public AnimationCurve muscleActivationAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Settings")]
    [Tooltip("Auto-extract from animation curves")]
    public bool extractFromAnimation = true;

    /// <summary>
    /// Extract attenuation curves from animation clip.
    /// </summary>
    public void ExtractFromAnimationClip(AnimationClip clip)
    {
        if (clip == null)
            return;

        // Initialize default curves
        positionAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        rotationAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        scaleAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        muscleActivationAttenuation = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        // Extract curves from animation clip
        // This is a simplified implementation - actual extraction would analyze
        // the animation's curves and derive attenuation properties
        // For now, we use default linear curves

        // TODO: Implement actual curve extraction from AnimationClip
        // This would involve:
        // 1. Getting all bindings from the clip
        // 2. Analyzing position/rotation/scale curves
        // 3. Deriving attenuation curves based on curve characteristics
    }

    /// <summary>
    /// Apply attenuation to an impulse action.
    /// </summary>
    public void ApplyToImpulseAction(ImpulseAction action)
    {
        if (action == null)
            return;

        // Modify action's curve based on muscle activation attenuation
        if (action.curve != null && muscleActivationAttenuation != null)
        {
            // Combine curves: multiply action curve by attenuation curve
            AnimationCurve combinedCurve = new AnimationCurve();
            
            // Sample both curves and combine
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                float actionValue = action.curve.Evaluate(t);
                float attenuationValue = muscleActivationAttenuation.Evaluate(t);
                combinedCurve.AddKey(t, actionValue * attenuationValue);
            }

            action.curve = combinedCurve;
        }
    }

    /// <summary>
    /// Get attenuation value for position at normalized time (0-1).
    /// </summary>
    public float GetPositionAttenuation(float normalizedTime)
    {
        if (positionAttenuation != null)
        {
            return positionAttenuation.Evaluate(normalizedTime);
        }
        return 1f;
    }

    /// <summary>
    /// Get attenuation value for rotation at normalized time (0-1).
    /// </summary>
    public float GetRotationAttenuation(float normalizedTime)
    {
        if (rotationAttenuation != null)
        {
            return rotationAttenuation.Evaluate(normalizedTime);
        }
        return 1f;
    }

    /// <summary>
    /// Get attenuation value for scale at normalized time (0-1).
    /// </summary>
    public float GetScaleAttenuation(float normalizedTime)
    {
        if (scaleAttenuation != null)
        {
            return scaleAttenuation.Evaluate(normalizedTime);
        }
        return 1f;
    }

    /// <summary>
    /// Get attenuation value for muscle activation at normalized time (0-1).
    /// </summary>
    public float GetMuscleActivationAttenuation(float normalizedTime)
    {
        if (muscleActivationAttenuation != null)
        {
            return muscleActivationAttenuation.Evaluate(normalizedTime);
        }
        return 1f;
    }
}
