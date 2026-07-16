using System.Collections.Generic;
using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>
/// Drive card solver that optionally routes accepted cards through <see cref="VehicleInstrumentPhysicsProxy"/>.
/// </summary>
public sealed class ProxiedDrivingPhysicsCardSolver : DrivingPhysicsCardSolver
{
    [Tooltip("When set, applicable cards can be routed to a remote vehicle via this proxy.")]
    public VehicleInstrumentPhysicsProxy physicsProxy;

    [Tooltip("If true, FindApplicableCards also requires each channel to resolve on the proxy.")]
    public bool requireProxyBinding = true;

    public new List<PhysicsCard> FindApplicableCards(RagdollState state, GameObject target = null)
    {
        var list = base.FindApplicableCards(state, target);
        if (!requireProxyBinding || physicsProxy == null)
            return list;

        var filtered = new List<PhysicsCard>();
        for (int i = 0; i < list.Count; i++)
        {
            var card = list[i];
            if (card?.impulseStack == null) continue;
            bool ok = true;
            for (int j = 0; j < card.impulseStack.Count; j++)
            {
                var a = card.impulseStack[j];
                if (a == null || !physicsProxy.TryResolveByChannel(a.muscleGroup, out _))
                {
                    ok = false;
                    break;
                }
            }
            if (ok) filtered.Add(card);
        }
        return filtered;
    }

    /// <summary>Apply the first applicable card through the proxy (or return false).</summary>
    public bool TryRouteFirstApplicable(RagdollState state, float dt)
    {
        var cards = FindApplicableCards(state);
        if (cards.Count == 0 || physicsProxy == null)
            return false;
        return physicsProxy.RouteCard(cards[0], dt);
    }
}
