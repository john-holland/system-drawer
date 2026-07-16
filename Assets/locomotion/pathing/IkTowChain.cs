using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ordered IK tow links. Sit: support→seat→pelvis→torso. StandOn: support→seat→feet→pelvis→torso.
/// Keeps occupant pose stable while support can move/rotate underneath.
/// </summary>
[Serializable]
public sealed class IkTowChain
{
    public SurfaceOccupancyMode mode = SurfaceOccupancyMode.Sit;
    public SitSurfaceContact surface;
    public List<IkTowLink> links = new List<IkTowLink>();
    public bool active;

    public void Clear()
    {
        links.Clear();
        active = false;
        surface = null;
    }

    public void Tick(float dt)
    {
        if (!active || links == null)
            return;
        for (int i = 0; i < links.Count; i++)
        {
            if (links[i] != null)
                links[i].Tick(dt);
        }
    }

    /// <summary>Build a sit tow chain: seat frame tows pelvis (and optional torso).</summary>
    public static IkTowChain BuildSit(SitSurfaceContact contact, Transform pelvis, Transform torso = null, Rigidbody pelvisBody = null)
    {
        var chain = new IkTowChain { mode = SurfaceOccupancyMode.Sit, surface = contact, active = true };
        if (contact == null || contact.host == null || pelvis == null)
            return chain;

        Transform seat = contact.host;
        Vector3 seatLocal = seat.InverseTransformPoint(contact.WorldPlanePoint + contact.WorldPlaneNormal * 0.05f);
        chain.links.Add(new IkTowLink
        {
            name = "seat_to_pelvis",
            parent = seat,
            child = pelvis,
            childBody = pelvisBody,
            localOffsetFromParent = seatLocal,
            stiffness = 0.9f,
            maxErrorMeters = 0.4f,
            useJointAssist = true
        });

        if (torso != null)
        {
            chain.links.Add(new IkTowLink
            {
                name = "pelvis_to_torso",
                parent = pelvis,
                child = torso,
                localOffsetFromParent = pelvis.InverseTransformPoint(torso.position),
                stiffness = 0.55f,
                maxErrorMeters = 0.25f
            });
        }
        return chain;
    }

    /// <summary>Build stand-on chain: seat tows feet, feet tow pelvis.</summary>
    public static IkTowChain BuildStandOn(
        SitSurfaceContact contact,
        Transform leftFoot,
        Transform rightFoot,
        Transform pelvis,
        Rigidbody leftFootBody = null,
        Rigidbody rightFootBody = null,
        Rigidbody pelvisBody = null)
    {
        var chain = new IkTowChain { mode = SurfaceOccupancyMode.StandOn, surface = contact, active = true };
        if (contact == null || contact.host == null)
            return chain;

        Transform seat = contact.host;
        Vector3 n = contact.WorldPlaneNormal;
        Vector3 p = contact.WorldPlanePoint;
        Vector3 right = Vector3.Cross(n, seat.forward).normalized;
        if (right.sqrMagnitude < 1e-4f)
            right = Vector3.Cross(n, Vector3.forward).normalized;

        if (leftFoot != null)
        {
            Vector3 plant = p - right * 0.1f + n * 0.02f;
            chain.links.Add(new IkTowLink
            {
                name = "seat_to_left_foot",
                parent = seat,
                child = leftFoot,
                childBody = leftFootBody,
                localOffsetFromParent = seat.InverseTransformPoint(plant),
                stiffness = 0.95f,
                maxErrorMeters = 0.3f,
                useJointAssist = true
            });
        }

        if (rightFoot != null)
        {
            Vector3 plant = p + right * 0.1f + n * 0.02f;
            chain.links.Add(new IkTowLink
            {
                name = "seat_to_right_foot",
                parent = seat,
                child = rightFoot,
                childBody = rightFootBody,
                localOffsetFromParent = seat.InverseTransformPoint(plant),
                stiffness = 0.95f,
                maxErrorMeters = 0.3f,
                useJointAssist = true
            });
        }

        if (pelvis != null && leftFoot != null)
        {
            chain.links.Add(new IkTowLink
            {
                name = "feet_to_pelvis",
                parent = leftFoot,
                child = pelvis,
                childBody = pelvisBody,
                localOffsetFromParent = leftFoot.InverseTransformPoint(pelvis.position),
                stiffness = 0.7f,
                maxErrorMeters = 0.45f
            });
        }
        return chain;
    }

    public float MaxLinkError()
    {
        float max = 0f;
        if (links == null) return 0f;
        for (int i = 0; i < links.Count; i++)
        {
            if (links[i] != null)
                max = Mathf.Max(max, links[i].ErrorMeters);
        }
        return max;
    }
}
