using System;
using System.Collections.Generic;
using UnityEngine;
using Weather;

public enum ManifoldSelectChannel
{
    Temperature = 0,
    Pressure = 1,
    Density = 2,
    SurfaceFriction = 3,
    SurfaceTension = 4,
    VelocityMag = 5
}

[Serializable]
public sealed class ManifoldLerpBand
{
    public ManifoldSelectChannel channel = ManifoldSelectChannel.Temperature;
    public float sampleA;
    public float sampleB = -10f;
    public float weightAtA;
    public float weightAtB = 1f;
    public AnimationCurve response;
}

[Serializable]
public sealed class PhysicsManifoldLerpGradientSelector
{
    public WeatherPhysicsManifold manifold;
    public List<ManifoldLerpBand> bands = new List<ManifoldLerpBand>();
    [Range(0f, 1f)] public float strength01 = 1f;
    public float coldStickC = -5f;

    public ManifoldCellData SampleLerped(Vector3 worldPos, Vector3? secondaryPos, float t)
    {
        ManifoldCellData a = SampleAt(worldPos);
        if (!secondaryPos.HasValue)
            return a;
        ManifoldCellData b = SampleAt(secondaryPos.Value);
        float u = Mathf.Clamp01(t);
        return new ManifoldCellData
        {
            velocity = Vector3.Lerp(a.velocity, b.velocity, u),
            pressure = Mathf.Lerp(a.pressure, b.pressure, u),
            temperature = Mathf.Lerp(a.temperature, b.temperature, u),
            density = Mathf.Lerp(a.density, b.density, u),
            mode = u < 0.5f ? a.mode : b.mode,
            lavaVelocity = Vector3.Lerp(a.lavaVelocity, b.lavaVelocity, u),
            gasPressure = Mathf.Lerp(a.gasPressure, b.gasPressure, u),
            surfaceTensionCoeff = Mathf.Lerp(a.surfaceTensionCoeff, b.surfaceTensionCoeff, u),
            roadSurfaceType = u < 0.5f ? a.roadSurfaceType : b.roadSurfaceType,
            surfaceFriction = Mathf.Lerp(a.surfaceFriction, b.surfaceFriction, u),
            surfacePorosity = Mathf.Lerp(a.surfacePorosity, b.surfacePorosity, u)
        };
    }

    public float EvaluateWeight(Vector3 worldPos)
    {
        if (bands == null || bands.Count == 0) return 0f;
        var cell = SampleAt(worldPos);
        float sum = 0f;
        float n = 0f;
        for (int i = 0; i < bands.Count; i++)
        {
            var band = bands[i];
            if (band == null) continue;
            float sample = ChannelValue(cell, band.channel);
            float t = InverseLerpSafe(band.sampleA, band.sampleB, sample);
            if (band.response != null && band.response.length > 0)
                t = band.response.Evaluate(t);
            sum += Mathf.Lerp(band.weightAtA, band.weightAtB, Mathf.Clamp01(t));
            n += 1f;
        }
        if (n <= 0f) return 0f;
        return Mathf.Clamp01(sum / n * strength01);
    }

    public float EvaluateAdhesionGate(Vector3 worldPos)
    {
        var cell = SampleAt(worldPos);
        float cold = cell.temperature <= coldStickC ? 1f : Mathf.InverseLerp(5f, coldStickC, cell.temperature);
        float friction = Mathf.Clamp01(cell.surfaceFriction);
        float tension = Mathf.Clamp01(cell.surfaceTensionCoeff);
        return Mathf.Clamp01((cold * 0.6f + friction * 0.25f + tension * 0.15f) * strength01);
    }

    public bool TryTongueFreezeBond(
        GameObject actor, Vector3 tonguePos, Vector3 polePos, TradeDemographics trade = null,
        HealthInpaintEventRunner runner = null)
    {
        var blended = SampleLerped(tonguePos, polePos, 0.65f);
        float gate = EvaluateAdhesionGate(polePos);
        if (blended.temperature > coldStickC && gate < 0.45f)
            return false;

        if (trade != null)
        {
            trade.EnsureRelation("tongue", "flag_pole", "ice_bond", TradeRelationKind.ChemicalBond,
                affinity01: 0.9f, bondOrSens01: gate);
        }

        string partId = "Tongue";
        if (actor != null)
        {
            var tongue = actor.GetComponentInChildren<TongueRuntime>();
            if (tongue == null) partId = "Head";
        }

        if (runner != null)
            runner.Fire("tongue_stuck_flagpole", actor, partId, "flag_pole", "outdoor");
        else
            ActorHealthInpaintBridge.RequestFromRule(actor, "tongue_freeze_bond", partId, "flag_pole", gate);

        return true;
    }

    ManifoldCellData SampleAt(Vector3 worldPos)
    {
        if (manifold != null)
            return manifold.GetDataAtPosition(worldPos);
        return new ManifoldCellData
        {
            temperature = 20f,
            pressure = 1013f,
            density = 1.2f,
            surfaceFriction = 0.3f,
            surfaceTensionCoeff = 0.1f
        };
    }

    static float ChannelValue(in ManifoldCellData cell, ManifoldSelectChannel channel)
    {
        switch (channel)
        {
            case ManifoldSelectChannel.Pressure: return cell.pressure;
            case ManifoldSelectChannel.Density: return cell.density;
            case ManifoldSelectChannel.SurfaceFriction: return cell.surfaceFriction;
            case ManifoldSelectChannel.SurfaceTension: return cell.surfaceTensionCoeff;
            case ManifoldSelectChannel.VelocityMag: return cell.velocity.magnitude;
            default: return cell.temperature;
        }
    }

    static float InverseLerpSafe(float a, float b, float v)
    {
        if (Mathf.Abs(b - a) < 1e-5f) return 0f;
        return Mathf.Clamp01((v - a) / (b - a));
    }
}

[AddComponentMenu("Locomotion/Stat/Manifold Contact Retinue Hook")]
public sealed class ManifoldContactRetinueHook : MonoBehaviour
{
    public PhysicsManifoldLerpGradientSelector selector = new PhysicsManifoldLerpGradientSelector();
    public string contactTag = "flag_pole";
    public HealthInpaintEventRunner inpaintRunner;
    public StatisticalRetinueDao dao;

    public bool TryContact(GameObject actor, Vector3 contactPos)
    {
        if (selector == null) return false;
        dao ??= StatisticalRetinueDao.Resolve(this);
        Vector3 tonguePos = contactPos;
        if (actor != null)
        {
            var tongue = actor.GetComponentInChildren<TongueRuntime>();
            if (tongue != null) tonguePos = tongue.transform.position;
        }
        return selector.TryTongueFreezeBond(actor, tonguePos, contactPos,
            dao?.GetTradeDemographics(), inpaintRunner);
    }
}
