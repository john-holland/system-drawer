using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>
/// Vehicle control surface card solver: only accepts good sections whose impulse stack keys exist on the <see cref="VehicleInstrumentMap"/>.
/// Pair with <see cref="PathingMode.Drive"/> paths and <see cref="DriveAnimationPhase"/>-tagged animation actions.
/// </summary>
public class DrivingPhysicsCardSolver : MonoBehaviour
{
    [Header("Vehicle")]
    [Tooltip("Root with vehicle parts / physics.")]
    public VehicleActor assignedVehicle;

    [Tooltip("Map of instrument ids to impulse channel keys.")]
    public VehicleInstrumentMap instrumentMap;

    [Header("Cards")]
    [Tooltip("Pool of drive sections (steer, throttle, etc.)")]
    public List<PhysicsCard> availableDriveCards = new List<PhysicsCard>();

    [Header("Stub state")]
    [Tooltip("Gates cards whose driveAnimationPhase is set when it does not match this mask (stub).")]
    public DriveAnimationPhase activeDrivePhaseMask = DriveAnimationPhase.Drive;

    public List<PhysicsCard> FindApplicableCards(RagdollState state, GameObject target = null)
    {
        var list = new List<PhysicsCard>();
        if (state == null || instrumentMap == null)
            return list;

        foreach (var card in availableDriveCards)
        {
            if (card == null || !card.IsFeasible(state)) continue;
            if (card.driveAnimationPhase != DriveAnimationPhase.None &&
                (card.driveAnimationPhase & activeDrivePhaseMask) == 0)
                continue;
            if (card.impulseStack == null || !InstrumentImpulseValidator.ValidateImpulseStack(card.impulseStack, instrumentMap))
                continue;
            list.Add(card);
        }

        return list;
    }

    public List<PhysicsCard> OrderCardsByFeasibility(List<PhysicsCard> cards, PhysicsCardSolver referenceFeasibility, RagdollState state)
    {
        if (cards == null || referenceFeasibility == null)
            return cards != null ? new List<PhysicsCard>(cards) : new List<PhysicsCard>();

        return cards
            .Where(c => c != null)
            .OrderByDescending(c => referenceFeasibility.CalculateFeasibilityScore(c, state))
            .ToList();
    }
}
