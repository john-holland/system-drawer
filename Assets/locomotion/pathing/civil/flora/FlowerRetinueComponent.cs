using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pollen field from wind and flower positions. Pollination chance uses intake cone, foreign mass, and economy.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Flower Retinue")]
public sealed class FlowerRetinueComponent : MonoBehaviour
{
    public FlowerTravelAgent flower;
    public Weather.Wind wind;
    public string sourceTreeId = "tree";
    public string foreignTreeId = "other_tree";
    public string fromRetinueId;
    public string toRetinueId;
    public string commodityId = "pollen";
    public float empowerMult = 1f;
    public List<FlowerPollenGrain> grains = new List<FlowerPollenGrain>();
    [System.NonSerialized] public FlowerRadialCache radialCache = new FlowerRadialCache(16);
    public AnimationCurve windBend = FlowerWindBendLut.Cherry();
    public Color interiorLight = new Color(1f, 0.72f, 0.82f, 1f);
    public Material particleMaterial;
    [Range(0f, 1f)] public float pressure01;
    Texture2D _windTex;

    void Awake()
    {
        if (flower == null)
            flower = GetComponent<FlowerTravelAgent>();
        if (radialCache == null)
            radialCache = new FlowerRadialCache(16);
    }

    public FlowerPollenGrain Release(Vector3 antherPos, Vector3 axis, bool foreign)
    {
        Vector3 windV = Vector3.zero;
        if (wind != null)
            windV = wind.GetWindAtPosition(antherPos, 0f);
        float impulse = 0f;
        var step = flower != null ? flower.SelectedStep : null;
        if (step != null)
            impulse = step.releaseImpulse;
        var grain = new FlowerPollenGrain
        {
            position = antherPos,
            velocity = (axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.up) * impulse,
            sourceTreeId = foreign ? foreignTreeId : sourceTreeId,
            foreign = foreign
        };
        grain.position += windV * 0.05f;
        grains.Add(grain);
        RefreshPressure();
        return grain;
    }

    public void Distribute(float dt)
    {
        if (dt <= 0f || grains.Count == 0) return;
        Vector3 windV = Vector3.zero;
        Vector3 origin = flower != null ? flower.transform.position : transform.position;
        if (wind != null)
            windV = wind.GetWindAtPosition(origin, 0f);
        for (int i = 0; i < grains.Count; i++)
        {
            var g = grains[i];
            g.position += (g.velocity + windV) * dt;
            grains[i] = g;
        }
        RefreshPressure();
    }

    void Update()
    {
        Distribute(Time.deltaTime);
    }

    void LateUpdate()
    {
        if (particleMaterial != null)
            BindShader(particleMaterial);
    }

    public float ForeignMassInCone(Vector3 stigmaPos, Vector3 stigmaForward)
    {
        var step = flower != null ? flower.SelectedStep : null;
        float angle = step != null ? step.intakeAngleDeg : 48f;
        float radius = step != null ? step.intakeRadius : 0.045f;
        float mass = 0f;
        for (int i = 0; i < grains.Count; i++)
        {
            if (!grains[i].foreign) continue;
            if (FlowerPollination.InsideIntakeCone(stigmaPos, stigmaForward, grains[i].position, angle, radius))
                mass += 1f;
        }
        return mass;
    }

    public float PollinationChance(Vector3 stigmaPos, Vector3 stigmaForward, Vector3 grainPos)
    {
        var step = flower != null ? flower.SelectedStep : null;
        float angle = step != null ? step.intakeAngleDeg : 48f;
        float radius = step != null ? step.intakeRadius : 0.045f;
        bool inside = FlowerPollination.InsideIntakeCone(stigmaPos, stigmaForward, grainPos, angle, radius);
        float foreign = ForeignMassInCone(stigmaPos, stigmaForward);
        float affinity = 0.5f;
        float empower = empowerMult;
        var dao = StatisticalRetinueDao.Resolve(this);
        if (dao != null)
        {
            var trade = dao.GetTradeDemographics();
            string commodity = commodityId;
            if (flower != null && flower.organism != null && !string.IsNullOrEmpty(flower.organism.commodityId))
                commodity = flower.organism.commodityId;
            affinity = FlowerPollination.Affinity(trade, fromRetinueId, toRetinueId, commodity);
        }
        float economy = FlowerPollination.Economy01(affinity, empower);
        float align = FlowerPollination.Alignment01(stigmaForward, grainPos, stigmaPos);
        float chance = FlowerPollination.Chance(foreign, economy, inside, align);
        if (flower != null && chance > 0.5f)
            flower.fertilized = true;
        return chance;
    }

    public void RefreshPressure()
    {
        int foreign = 0;
        for (int i = 0; i < grains.Count; i++)
            if (grains[i].foreign) foreign++;
        pressure01 = Mathf.Clamp01(foreign / 4f);
        if (flower != null)
            flower.retinuePressure01 = pressure01;
    }

    public bool UseLiveCache =>
        FeatureBudget.GetGranularity(FeatureBudgetIds.Flowers) >= 0.5f;

    public bool TryClaimPetalSector(float azimuth01)
    {
        if (!UseLiveCache || radialCache == null) return false;
        return radialCache.TryClaim(radialCache.Sector(azimuth01), interiorLight);
    }

    public void BindShader(Material mat)
    {
        if (mat == null) return;
        bool live = UseLiveCache;
        mat.SetFloat("_UseLiveCache", live ? 1f : 0f);
        mat.SetColor("_InteriorLight", interiorLight);
        mat.SetFloat("_WindBend", FlowerWindBendLut.Sample(windBend, 0.25f));
        if (live && radialCache != null)
            mat.SetTexture("_RadialCache", radialCache.Upload());
        if (live && windBend != null)
        {
            if (_windTex == null)
                _windTex = FlowerWindBendLut.Bake(windBend);
            mat.SetTexture("_WindLut", _windTex);
        }
    }
}
