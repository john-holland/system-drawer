using System;
using UnityEngine;

/// <summary>Parameterized vote gate. Empty list of gates means allowed (unless in-paint).</summary>
[Serializable]
public sealed class VoteCausalityGate
{
    public string requiredEventId;
    public bool requireWebtopAccess;
    public string dialogTreeSetId;
    public string openCloseTopologyId;
    public string unlockBeforeOpenLemma = OpenCloseLemmaPropertyKeys.UnlockBeforeOpen;
    [Tooltip("Set without referencing Open.Runtime. True when open/close topology is unlocked.")]
    public bool openCloseUnlocked = true;

    public bool Evaluate(
        Locomotion.Narrative.NarrativeExecutor executor,
        ComputerPeripheryStation webtop)
    {
        if (!string.IsNullOrEmpty(requiredEventId))
        {
            var state = executor != null ? executor.GetRuntimeState() : null;
            if (state == null || state.triggeredEventIds == null
                || !state.triggeredEventIds.Contains(requiredEventId))
                return false;
        }
        if (requireWebtopAccess)
        {
            if (webtop == null || webtop.toolUseGate == null || !webtop.toolUseGate.AllowsToolUse())
                return false;
        }
        if (!string.IsNullOrEmpty(openCloseTopologyId) && !openCloseUnlocked)
            return false;
        return true;
    }
}
