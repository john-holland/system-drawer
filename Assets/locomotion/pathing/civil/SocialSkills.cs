using System;
using System.Collections.Generic;
using UnityEngine;

public enum SocialRequestChannel
{
    Telecom = 0,
    RetinueRts = 1,
    Local = 2
}

[Serializable]
public sealed class SocialInterpretResult
{
    public string intent;
    public string dialogueHint;
    public bool requestTravel;
    public bool requestCallToArms;
    public Vector3 travelTarget;
    public string troupeId;
}

/// <summary>Interpret telecom or retinue RTS requests into dialogue / travel / CallToArms.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Social Skills")]
public sealed class SocialSkills : MonoBehaviour
{
    public string actorId;
    public float deescalateBias01 = 0.55f;

    public SocialInterpretResult Interpret(SocialRequestChannel channel, string rawRequest, Vector3 hintTarget = default)
    {
        var result = new SocialInterpretResult
        {
            intent = "acknowledge",
            dialogueHint = string.IsNullOrEmpty(rawRequest) ? "…" : rawRequest
        };
        if (string.IsNullOrEmpty(rawRequest))
            return result;

        var lower = rawRequest.ToLowerInvariant();
        if (lower.Contains("help") || lower.Contains("backup") || lower.Contains("arms"))
        {
            result.intent = "call_to_arms";
            result.requestCallToArms = true;
            result.troupeId = actorId;
            result.travelTarget = hintTarget;
        }
        else if (lower.Contains("come") || lower.Contains("meet") || lower.Contains("patrol"))
        {
            result.intent = "travel";
            result.requestTravel = true;
            result.travelTarget = hintTarget;
        }
        else if (lower.Contains("calm") || lower.Contains("talk") || lower.Contains("deescalat"))
        {
            result.intent = "deescalate";
            result.dialogueHint = "Let's talk this through.";
        }

        if (channel == SocialRequestChannel.Telecom)
            result.dialogueHint = "[telecom] " + result.dialogueHint;
        else if (channel == SocialRequestChannel.RetinueRts)
            result.dialogueHint = "[rts] " + result.dialogueHint;

        return result;
    }

    public void Apply(SocialInterpretResult result)
    {
        if (result == null) return;
        if (!string.IsNullOrEmpty(result.dialogueHint))
            SendMessage("SendThought", result.dialogueHint, SendMessageOptions.DontRequireReceiver);
        if (result.requestCallToArms && !string.IsNullOrEmpty(result.troupeId))
        {
            var facilitator = FindFirstObjectByType<CombatRulesFacilitatorService>();
            facilitator?.CallToArms(result.troupeId, result.travelTarget);
        }
        if (result.requestTravel)
        {
            var ta = GetComponent<TravelAgent>();
            if (ta != null)
            {
                ta.previewGoalWorld = result.travelTarget;
                ta.RebuildCachedPlan();
            }
        }
    }
}
