using UnityEngine;

/// <summary>Submits vote casts into lockstep for the active GameSession.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("System Drawer/Networking/Vote Lockstep Bridge")]
public sealed class VoteLockstepBridge : MonoBehaviour
{
    public ServerOrchestrator orchestrator;
    public VoteLedger ledger;
    public string clientId = "host";

    void Awake()
    {
        if (orchestrator == null) orchestrator = GetComponent<ServerOrchestrator>();
        if (ledger == null) ledger = GetComponent<VoteLedger>();
        orchestrator?.EnsureReady();
        orchestrator?.GameSessions?.BindVoteNodes();
    }

    public bool TrySubmit(VoteCastRecord rec, out string reason)
    {
        reason = "";
        if (rec == null)
        {
            reason = "empty cast";
            return false;
        }
        var lockstep = GetLockstep();
        if (lockstep == null)
        {
            reason = "no lockstep";
            return false;
        }
        var host = orchestrator.GameSessions;
        host?.BindVoteNodes();
        var registry = orchestrator.TreeRegistry;
        string sessionId = host != null ? host.ActiveId : "";
        string prefix = rec.causalityLeafId ?? "";
        int dot = prefix.LastIndexOf('.');
        if (dot > 0) prefix = prefix.Substring(0, dot);
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = rec.causalityLeafId ?? rec.actorId,
            TransmitPolicy = TreeTransmitPolicy.PeerTransferable,
            OwnerClientId = clientId,
            CausalityLeafPrefix = prefix,
            GameSessionId = sessionId
        });
        return lockstep.TryValidateDecision(clientId, rec.causalityLeafId, out reason);
    }

    public bool AccountingSuccess(VoteResult local, VoteResult host)
    {
        if (ledger == null) return local != null && host != null && local.tallyHash == host.tallyHash;
        return ledger.AccountingMatches(local, host);
    }

    LockstepDecisionValidator GetLockstep()
    {
        if (orchestrator == null) return null;
        orchestrator.EnsureReady();
        return orchestrator.GameSessions != null ? orchestrator.GameSessions.lockstep : null;
    }
}
