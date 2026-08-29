using System.Collections.Generic;

/// <summary>Classic lockstep decision validation against server registry.</summary>
public sealed class LockstepDecisionValidator
{
    readonly NetworkTreeRegistry _registry;
    readonly HashSet<string> _approvedLeaves = new HashSet<string>();

    public string ActiveGameSessionId = "";

    public LockstepDecisionValidator(NetworkTreeRegistry registry)
    {
        _registry = registry;
    }

    public bool TryValidateDecision(string clientId, string causalityLeafId, out string reason)
    {
        reason = "";
        if (string.IsNullOrEmpty(causalityLeafId))
        {
            reason = "empty leaf";
            return false;
        }

        foreach (var pair in _registry.Trees)
        {
            string prefix = pair.Value.CausalityLeafPrefix;
            if (string.IsNullOrEmpty(prefix))
                continue;
            if (!CausalityFamilyAudit.IsCompatiblePrefix(prefix, causalityLeafId))
                continue;
            if (!string.IsNullOrEmpty(ActiveGameSessionId)
                && !string.IsNullOrEmpty(pair.Value.GameSessionId)
                && pair.Value.GameSessionId != ActiveGameSessionId)
                continue;
            if (pair.Value.TransmitPolicy == TreeTransmitPolicy.LocalOnly &&
                pair.Value.OwnerClientId != clientId)
            {
                reason = "local-only tree owned by another client";
                return false;
            }
            _approvedLeaves.Add(causalityLeafId);
            return true;
        }

        reason = "leaf not assignable from registry";
        return false;
    }

    public IReadOnlyCollection<string> ApprovedLeaves => _approvedLeaves;
}
