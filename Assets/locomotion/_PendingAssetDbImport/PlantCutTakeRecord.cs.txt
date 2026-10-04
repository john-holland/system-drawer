using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plant cut take-for-inventory: (plant, seed, time t, cut plane set ps).</summary>
[Serializable]
public sealed class PlantCutTakeRecord
{
    public string plantId;
    public string seedId;
    public float timeT;
    public List<Plane> cutPlanes = new List<Plane>();
    public string commodityKey;
    public float quantity = 1f;
    public Vector3 worldHit;
    [Range(0f, 1f)] public float severity01;
}

/// <summary>SDF-style plant cut + inventory take wired from CutTool / cooking / chainsaw.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Park/Plant Cut Take")]
public sealed class PlantCutTakeRuntime : MonoBehaviour
{
    public LotGrassGrowthController grass;
    public string plantId = "plant";
    public string seedId = "seed";
    public string commodityKey = "plant_cuttings";
    public List<PlantCutTakeRecord> takes = new List<PlantCutTakeRecord>();
    public Material sdfCutMaterial;
    public bool destructibleTrunkFall = true;

    void Awake()
    {
        if (grass == null)
            grass = GetComponent<LotGrassGrowthController>()
                    ?? GetComponentInParent<LotGrassGrowthController>();
    }

    public PlantCutTakeRecord ApplyCutTake(Vector3 worldHit, Vector3 planeNormal, float severity01, float timeT = -1f)
    {
        severity01 = Mathf.Clamp01(severity01);
        if (timeT < 0f) timeT = Time.time;
        if (grass != null)
            grass.ApplyCut(worldHit, cutLength: 0.25f + severity01, severity01: severity01, leafSectionId: grass.stageIndex);

        var rec = new PlantCutTakeRecord
        {
            plantId = plantId,
            seedId = seedId,
            timeT = timeT,
            commodityKey = commodityKey,
            quantity = Mathf.Lerp(0.25f, 2f, severity01),
            worldHit = worldHit,
            severity01 = severity01
        };
        rec.cutPlanes.Add(new Plane(planeNormal.sqrMagnitude > 1e-6f ? planeNormal.normalized : Vector3.up, worldHit));
        takes.Add(rec);

        if (sdfCutMaterial != null && sdfCutMaterial.HasProperty("_CutPlane"))
        {
            Vector4 p = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z,
                -Vector3.Dot(planeNormal.normalized, worldHit));
            sdfCutMaterial.SetVector("_CutPlane", p);
            if (sdfCutMaterial.HasProperty("_CutSeverity"))
                sdfCutMaterial.SetFloat("_CutSeverity", severity01);
        }

        if (destructibleTrunkFall && severity01 >= 0.85f)
            SendMessage("OnPlantTrunkFall", rec, SendMessageOptions.DontRequireReceiver);

        SendMessage("OnPlantCutTake", rec, SendMessageOptions.DontRequireReceiver);
        return rec;
    }

    /// <summary>Hook for CutToolComponent EmitAt — listen via SendMessage or call directly.</summary>
    public void OnCombatCut(CombatDamageEvent evt)
    {
        if (evt == null) return;
        ApplyCutTake(evt.worldHit, evt.direction, evt.amount01, Time.time);
    }
}
