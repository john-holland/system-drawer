using System;
using System.Collections.Generic;
using UnityEngine;

public enum AuthAccessTier
{
    Public = 0,
    Staff = 1,
    Secure = 2,
    Restricted = 3
}

[Serializable]
public sealed class AuthZone
{
    public string locationId;
    public Transform anchor;
    public Bounds localBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
    public AuthAccessTier requiredTier = AuthAccessTier.Staff;
    public bool publicAccess;
    public bool privateIntended;
}

public sealed class AuthEventArgs : EventArgs
{
    public string locationId;
    public string personaKey;
    public AuthAccessTier tier;
    public bool granted;
}

/// <summary>Issues subscribable location-authorization events for airport rooms, checkpoints, and gates.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Auth Warden")]
public sealed class AuthWarden : MonoBehaviour
{
    public List<AuthZone> zones = new List<AuthZone>();

    public event EventHandler<AuthEventArgs> OnAuthGranted;
    public event EventHandler<AuthEventArgs> OnAuthDenied;
    public event EventHandler<AuthEventArgs> OnAuthRevoked;

    readonly Dictionary<string, AuthAccessTier> _grants = new Dictionary<string, AuthAccessTier>(StringComparer.OrdinalIgnoreCase);

    public void Subscribe(EventHandler<AuthEventArgs> granted, EventHandler<AuthEventArgs> denied, EventHandler<AuthEventArgs> revoked)
    {
        if (granted != null) OnAuthGranted += granted;
        if (denied != null) OnAuthDenied += denied;
        if (revoked != null) OnAuthRevoked += revoked;
    }

    public void Unsubscribe(EventHandler<AuthEventArgs> granted, EventHandler<AuthEventArgs> denied, EventHandler<AuthEventArgs> revoked)
    {
        if (granted != null) OnAuthGranted -= granted;
        if (denied != null) OnAuthDenied -= denied;
        if (revoked != null) OnAuthRevoked -= revoked;
    }

    public bool TryAuthorize(string locationId, string personaKey, AuthAccessTier requesterTier)
    {
        AuthZone zone = FindZone(locationId);
        var args = new AuthEventArgs
        {
            locationId = locationId ?? "",
            personaKey = personaKey ?? "",
            tier = requesterTier
        };

        bool ok = zone == null
                  || zone.publicAccess
                  || requesterTier >= zone.requiredTier;

        args.granted = ok;
        if (ok)
        {
            string key = GrantKey(locationId, personaKey);
            _grants[key] = requesterTier;
            OnAuthGranted?.Invoke(this, args);
        }
        else
        {
            OnAuthDenied?.Invoke(this, args);
        }
        return ok;
    }

    public void Revoke(string locationId, string personaKey)
    {
        string key = GrantKey(locationId, personaKey);
        if (!_grants.Remove(key)) return;
        OnAuthRevoked?.Invoke(this, new AuthEventArgs
        {
            locationId = locationId ?? "",
            personaKey = personaKey ?? "",
            granted = false
        });
    }

    public bool HasGrant(string locationId, string personaKey) =>
        _grants.ContainsKey(GrantKey(locationId, personaKey));

    public AuthZone FindZone(string locationId)
    {
        if (string.IsNullOrEmpty(locationId) || zones == null) return null;
        for (int i = 0; i < zones.Count; i++)
        {
            AuthZone z = zones[i];
            if (z != null && string.Equals(z.locationId, locationId, StringComparison.OrdinalIgnoreCase))
                return z;
        }
        return null;
    }

    static string GrantKey(string locationId, string personaKey) =>
        (locationId ?? "") + "|" + (personaKey ?? "");
}
