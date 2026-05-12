using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TravelMultibodyPathAdjusterTests
{
    [Test]
    public void EffectiveClearanceRadius_LowConfidence_IsLargerThanHighConfidence()
    {
        float baseR = 0.5f;
        float low = TravelMultibodyPathAdjuster.EffectiveClearanceRadius(baseR, 0f);
        float high = TravelMultibodyPathAdjuster.EffectiveClearanceRadius(baseR, 1f);
        Assert.Greater(low, high);
    }

    [Test]
    public void PaceLongitudinalExtraSep_LeadWhenPeerAhead_IsPositive()
    {
        float extra = TravelMultibodyPathAdjuster.PaceLongitudinalExtraSep(TravelPaceMode.Lead, 1f);
        Assert.Greater(extra, 0f);
    }

    [Test]
    public void PaceLongitudinalExtraSep_TailWhenPeerBehind_IsPositive()
    {
        float extra = TravelMultibodyPathAdjuster.PaceLongitudinalExtraSep(TravelPaceMode.Tail, -1f);
        Assert.Greater(extra, 0f);
    }

    [Test]
    public void PaceLongitudinalExtraSep_Keep_IsZero()
    {
        Assert.AreEqual(0f, TravelMultibodyPathAdjuster.PaceLongitudinalExtraSep(TravelPaceMode.Keep, 1f));
    }

    [Test]
    public void RelaxPolyline_IncreasesSeparationFromClosePeer()
    {
        var self = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(2f, 0f, 1f),
            new Vector3(4f, 0f, 2f)
        };
        var originals = new List<Vector3>(self);
        var peer = new List<Vector3>
        {
            new Vector3(0f, 0f, 0.4f),
            new Vector3(2f, 0f, 1.35f),
            new Vector3(4f, 0f, 2.3f)
        };
        var peers = new List<List<Vector3>> { peer };
        var settings = new TravelAgentMultibodySettings
        {
            aggressiveness01 = 0f,
            confidence01 = 1f,
            clearanceRadius = 0.45f,
            relaxationIterations = 10,
            paceMode = TravelPaceMode.Keep
        };
        Vector3 fwd = new Vector3(4f, 0f, 2f) - new Vector3(0f, 0f, 0f);
        TravelMultibodyPathAdjuster.RelaxPolylineAgainstPeersForTests(self, originals, peers, 0.45f, settings, fwd);

        float before = Vector3.Distance(originals[1], peer[1]);
        float after = Vector3.Distance(self[1], peer[1]);
        Assert.Greater(after, before - 0.01f);
    }
}
