using System.Collections.Generic;
using UnityEngine;

public enum JusticeWardenAction
{
    Allow = 0,
    Caution = 1,
    Restrain = 2
}

/// <summary>
/// Shared justice scorer: wraps <see cref="PrisonWarden.lastScore01"/> plus
/// <see cref="JusticeCard.EffectiveViolenceThreshold01"/> and optional <see cref="RightsWarden"/>.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Justice Warden")]
public sealed class JusticeWarden : MonoBehaviour
{
    public PrisonWarden prisonWarden;
    public JusticeCard justiceCard;
    public RightsWarden rightsWarden;
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public JusticeWardenAction lastAction = JusticeWardenAction.Allow;
    public List<WardenLimitKv> limits = new System.Collections.Generic.List<WardenLimitKv>();

    public float Allow01()
    {
        Evaluate();
        return lastScore01;
    }

    public float Evaluate()
    {
        var prison = prisonWarden != null ? prisonWarden : GetComponent<PrisonWarden>();
        var rights = rightsWarden != null ? rightsWarden : GetComponent<RightsWarden>();
        float prisonAllow = prison != null ? Mathf.Clamp01(1f - prison.lastScore01) : 0.5f;
        if (rights != null)
        {
            float rightsAllow = rights.Allow01();
            lastScore01 = Mathf.Clamp01(0.5f * prisonAllow + 0.5f * rightsAllow);
        }
        else if (prison != null)
        {
            lastScore01 = prisonAllow;
        }
        else
        {
            lastScore01 = 0.5f;
        }

        if (justiceCard != null)
        {
            float thr = justiceCard.EffectiveViolenceThreshold01();
            lastScore01 = Mathf.Clamp01(lastScore01 * 0.8f + thr * 0.2f);
        }

        if (lastScore01 >= 0.67f) lastAction = JusticeWardenAction.Allow;
        else if (lastScore01 >= 0.34f) lastAction = JusticeWardenAction.Caution;
        else lastAction = JusticeWardenAction.Restrain;
        return lastScore01;
    }
}
