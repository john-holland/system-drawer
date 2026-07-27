using System;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>Combat communique over voice / handheld / phone / webtop with optional dialogue BT.</summary>
[Serializable]
public sealed class CombatCommuniqueNarrativeAction : NarrativeActionSpec
{
    public string facilitatorKey = "combat.facilitator";
    public string issuerKey = "issuer";
    public string troupeId = "default";
    public CombatCommuniqueChannel channel = CombatCommuniqueChannel.Voice;
    public bool callToArms = true;
    public bool ignoreCallToArmsRange;
    public string dialogueSpanRef;
    public string topologicalConversationBtKey = "combat.communique";

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;

        CombatRulesFacilitatorService facilitator = null;
        if (ctx.TryResolveGameObject(facilitatorKey, out var facGo) && facGo != null)
            facilitator = facGo.GetComponent<CombatRulesFacilitatorService>()
                          ?? facGo.GetComponentInChildren<CombatRulesFacilitatorService>();

        if (facilitator == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        Vector3 origin = Vector3.zero;
        if (ctx.TryResolveGameObject(issuerKey, out var issuer) && issuer != null)
        {
            origin = issuer.transform.position;
            if (!facilitator.CanIssueOrders(issuer, troupeId))
                return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        }

        TryNotifyTelecom(channel, troupeId);

        if (callToArms)
            facilitator.CallToArms(troupeId, origin, ignoreCallToArmsRange);

        if (!string.IsNullOrEmpty(dialogueSpanRef) &&
            ctx.TryResolveGameObject(issuerKey, out var speaker) && speaker != null)
        {
            var runner = speaker.GetComponent<DialogueRunner>()
                         ?? speaker.GetComponentInChildren<DialogueRunner>();
            // Span ref is a compile hint; DialogueRunner may pick it up via bindings.
            if (runner != null)
                runner.SendMessage("BeginFromSpanRef", dialogueSpanRef, SendMessageOptions.DontRequireReceiver);
        }

        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }

    static void TryNotifyTelecom(CombatCommuniqueChannel channel, string troupeId)
    {
        var bridgeType = Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Continuuuum.Runtime")
                         ?? Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Assembly-CSharp");
        if (bridgeType == null) return;
        var bridge = UnityEngine.Object.FindAnyObjectByType(bridgeType) as MonoBehaviour;
        if (bridge == null) return;
        bridge.SendMessage(
            "NotifyVisual",
            $"{channel}:{troupeId}",
            SendMessageOptions.DontRequireReceiver);
    }
}
