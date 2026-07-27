using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>
/// Routes local instrument channels onto remote vehicle control surfaces (physics proxy past local instruments).
/// </summary>
public sealed class VehicleInstrumentPhysicsProxy : MonoBehaviour
{
    [Tooltip("Local map that authored cards / grips use.")]
    public VehicleInstrumentMap sourceMap;

    [Tooltip("Optional remote map used to validate remote surface channels.")]
    public VehicleInstrumentMap remoteMap;

    public List<VehicleInstrumentBinding> bindings = new List<VehicleInstrumentBinding>();

    readonly Dictionary<string, IVehicleControlSurface> _resolved = new Dictionary<string, IVehicleControlSurface>(StringComparer.OrdinalIgnoreCase);

    public bool TryResolve(string localSurfaceId, out IVehicleControlSurface surface)
    {
        surface = null;
        if (string.IsNullOrEmpty(localSurfaceId))
            return false;
        if (_resolved.TryGetValue(localSurfaceId, out surface) && surface != null)
            return true;

        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b == null || !string.Equals(b.localSurfaceId, localSurfaceId, StringComparison.OrdinalIgnoreCase))
                continue;
            surface = BuildSurface(b);
            if (surface != null)
            {
                _resolved[localSurfaceId] = surface;
                return true;
            }
        }
        return false;
    }

    public bool TryResolveByChannel(string impulseChannelKey, out IVehicleControlSurface surface)
    {
        surface = null;
        if (string.IsNullOrEmpty(impulseChannelKey) || sourceMap == null)
            return false;
        var slots = sourceMap.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!string.Equals(slots[i].impulseChannelKey, impulseChannelKey, StringComparison.OrdinalIgnoreCase))
                continue;
            return TryResolve(slots[i].id, out surface);
        }
        return false;
    }

    /// <summary>
    /// Validates the card against the source map, then applies each impulse on the remote surface if bound.
    /// Returns false when the source map rejects the stack or any channel lacks a binding.
    /// </summary>
    public bool RouteCard(PhysicsCard card, float dt)
    {
        if (card == null || card.impulseStack == null || sourceMap == null)
            return false;
        if (!InstrumentImpulseValidator.ValidateImpulseStack(card.impulseStack, sourceMap))
            return false;

        for (int i = 0; i < card.impulseStack.Count; i++)
        {
            var a = card.impulseStack[i];
            if (a == null) return false;
            if (!TryResolveByChannel(a.muscleGroup, out var surface) || surface == null)
                return false;
            if (remoteMap != null && !string.IsNullOrEmpty(surface.ImpulseChannelKey) &&
                !remoteMap.ChannelIsAllowed(surface.ImpulseChannelKey) &&
                !remoteMap.TryGetSlot(surface.Id, out _))
            {
                // Remote map present but neither channel nor id matches — reject.
                bool idOk = false;
                var remSlots = remoteMap.Slots;
                for (int r = 0; r < remSlots.Count; r++)
                {
                    if (string.Equals(remSlots[r].id, surface.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        idOk = true;
                        break;
                    }
                }
                if (!idOk) return false;
            }
            surface.ApplyImpulse(a.activation, dt);
        }
        return true;
    }

    /// <summary>
    /// One-shot fire pulse for combat/ranged cards: resolve local surface and apply a brief impulse.
    /// Falls back to RouteCard when the card already has a valid instrument impulse stack.
    /// </summary>
    public bool TryFirePulse(string localSurfaceId, PhysicsCard card, float dt)
    {
        if (!string.IsNullOrEmpty(localSurfaceId) && TryResolve(localSurfaceId, out var surface) && surface != null)
        {
            float act = 1f;
            if (card?.impulseStack != null && card.impulseStack.Count > 0 && card.impulseStack[0] != null)
                act = card.impulseStack[0].activation;
            surface.ApplyImpulse(act, Mathf.Max(dt, 1f / 60f));
            return true;
        }
        return RouteCard(card, dt);
    }

    public void InvalidateCache()
    {
        _resolved.Clear();
    }

    IVehicleControlSurface BuildSurface(VehicleInstrumentBinding b)
    {
        if (b.remoteVehicle == null || string.IsNullOrEmpty(b.remoteSurfaceId))
            return null;

        string channel = b.remoteSurfaceId;
        if (remoteMap != null && remoteMap.TryGetSlot(b.remoteSurfaceId, out var slot) &&
            !string.IsNullOrEmpty(slot.impulseChannelKey))
            channel = slot.impulseChannelKey;
        else if (sourceMap != null && sourceMap.TryGetSlot(b.localSurfaceId, out var localSlot) &&
                 !string.IsNullOrEmpty(localSlot.impulseChannelKey))
            channel = localSlot.impulseChannelKey;

        var body = b.remoteVehicle.GetComponent<Rigidbody>();
        if (body == null)
            body = b.remoteVehicle.GetComponentInChildren<Rigidbody>();
        if (body == null)
            return new NullVehicleControlSurface(b.remoteSurfaceId, channel, b.remoteVehicle);

        var origin = b.remoteForceOrigin != null ? b.remoteForceOrigin : b.remoteVehicle.transform;
        return new VehicleManifoldControlSurface(
            b.remoteSurfaceId,
            channel,
            b.remoteVehicle,
            body,
            origin,
            b.localForceAxis,
            b.maxForceNewtons,
            b.applyAsTorque);
    }
}
