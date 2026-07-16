using System.Collections.Generic;
using UnityEngine;

public enum StairwellFishPhase
{
    Idle,
    KnockOutCops,
    ClaimNightsticks,
    DescendDeflect,
    ElevatorRace,
    Success,
    Failed
}

/// <summary>
/// Spy KO → dual nightsticks → topological rail deflection descent → elevator race.
/// </summary>
public sealed class StairwellNightstickFishDirector : MonoBehaviour
{
    public StairwellTopologyAsset topology;
    public RailDingRadialCache dingCache;
    public RailDingChainPlayer dingPlayer;
    public MuscularFatigueAdrenalineState actorState;
    public List<StairwellRailingNode> railings = new List<StairwellRailingNode>();
    public NightstickWeapon stickA;
    public NightstickWeapon stickB;
    public Transform leftHand;
    public Transform rightHand;
    public GambitSelectionSession gambitSession;
    public PathingApertureRegistry apertureRegistry;

    public StairwellFishPhase phase = StairwellFishPhase.Idle;
    public int railingCursor;
    public bool lastDeflectionSucceeded = true;

    readonly List<string> _order = new List<string>();

    public void Begin()
    {
        phase = StairwellFishPhase.KnockOutCops;
        railingCursor = 0;
        _order.Clear();
        if (topology != null)
            _order.AddRange(topology.EnumerateRailingsDepthFirst());
        RegisterRailApertures();
        if (dingCache != null)
        {
            for (int i = 0; i < railings.Count; i++)
            {
                var r = railings[i];
                if (r == null) continue;
                dingCache.PrebakeRailing(string.IsNullOrEmpty(r.railingId) ? r.name : r.railingId);
            }
        }
    }

    public void NotifyCopsDown()
    {
        if (actorState != null) actorState.RegisterKo();
        phase = StairwellFishPhase.ClaimNightsticks;
        if (stickA != null && rightHand != null) stickA.Claim(rightHand, true);
        if (stickB != null && leftHand != null) stickB.Claim(leftHand, true);
        if (stickA != null && stickB != null) stickA.PairWith(stickB);
        phase = StairwellFishPhase.DescendDeflect;
    }

    public StairwellRailingNode CurrentRailing()
    {
        if (railingCursor < 0 || railingCursor >= _order.Count) return null;
        string id = _order[railingCursor];
        for (int i = 0; i < railings.Count; i++)
            if (railings[i] != null && (railings[i].railingId == id || railings[i].name == id))
                return railings[i];
        return null;
    }

    public bool TryDeflectCurrent(float nightstickImpulse)
    {
        var rail = CurrentRailing();
        if (rail == null || topology == null)
        {
            phase = StairwellFishPhase.Failed;
            return false;
        }
        if (actorState != null) actorState.RegisterSwing();

        float depth = topology.RemainingDepthNormalized(rail.floorIndex, topology.MinFloor());
        var result = RailDeflectionSuccessEstimator.Estimate(new RailDeflectionSuccessEstimator.Input
        {
            remainingStairDepthNormalized = depth,
            railingFriction = rail.manifoldFriction,
            railingMassHint = rail.massHint,
            nightstickImpulse = nightstickImpulse,
            fatigue01 = actorState != null ? actorState.fatigue01 : 0f,
            adrenaline01 = actorState != null ? actorState.adrenaline01 : 0f,
            strength01 = actorState != null ? actorState.strength01 : 0.65f
        });

        lastDeflectionSucceeded = result.likelySuccess;
        if (dingPlayer != null && dingCache != null)
        {
            var strike = rail.SampleStrikePoint(0);
            dingPlayer.PlayDingChain(
                string.IsNullOrEmpty(rail.railingId) ? rail.name : rail.railingId,
                rail.transform.position,
                strike,
                lastDeflectionSucceeded ? 3 : 1);
        }

        if (!lastDeflectionSucceeded)
        {
            if (actorState != null) actorState.RegisterNearMiss();
            phase = StairwellFishPhase.Failed;
            return false;
        }

        railingCursor++;
        if (railingCursor >= _order.Count)
        {
            phase = StairwellFishPhase.ElevatorRace;
            phase = StairwellFishPhase.Success;
        }
        return true;
    }

    void RegisterRailApertures()
    {
        if (apertureRegistry == null) return;
        apertureRegistry.apertures.Clear();
        for (int i = 0; i < railings.Count; i++)
        {
            var r = railings[i];
            if (r == null) continue;
            r.EnsureAperture();
            apertureRegistry.apertures.Add(r.pathingAperture);
        }
        if (gambitSession != null)
            gambitSession.registry = apertureRegistry;
    }
}
