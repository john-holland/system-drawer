using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LotGrassGrowthStage
{
    public string stageId;
    public Mesh meshA;
    public Mesh meshB;
    public float durationSec = 2f;
    public ParticleSystem changeParticles;
    public BehaviorTree stageBt;
}

[CreateAssetMenu(fileName = "LotGrassPlantDef", menuName = "Locomotion/Civil/Lot Grass Plant Def")]
public sealed class LotGrassPlantDef : ScriptableObject
{
    public string speciesId = "lot_grass";
    public GameObject speedTreePrefab;
    [Range(0f, 1f)] public float startGrowth01;
    [Range(0f, 1f)] public float endGrowth01 = 1f;
    public List<LotGrassGrowthStage> grownStages = new List<LotGrassGrowthStage>();
}

[Serializable]
public sealed class LotGrassCutMemory
{
    public int sectionId;
    public Vector3 localPoint;
    public float cutLength;
    [Range(0f, 1f)] public float severity01;
    public int parentSectionId = -1;
    public bool forgotten;
}

/// <summary>Lot grass growth + cut memory (leaf→root, severity 0–1).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Lot Grass Growth")]
public sealed class LotGrassGrowthController : MonoBehaviour
{
    public LotGrassPlantDef plantDef;
    public RoadLot lot;
    public int stageIndex;
    [Range(0f, 1f)] public float growth01;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material morphMaterial;
    public List<LotGrassCutMemory> cuts = new List<LotGrassCutMemory>();
    public List<int> sectionParent = new List<int>();
    public float nextSectionSpawnChance = 1f;

    public GameObject spawnedPrefabInstance;
    public PlantCutTakeRuntime cutTake;

    void Awake()
    {
        if (lot == null)
            lot = GetComponentInParent<RoadLot>();
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (plantDef != null)
            growth01 = plantDef.startGrowth01;
        if (cutTake == null)
            cutTake = GetComponent<PlantCutTakeRuntime>() ?? gameObject.AddComponent<PlantCutTakeRuntime>();
        cutTake.grass = this;
        EnsureSpeedTreeInstance();
    }

    public void EnsureSpeedTreeInstance()
    {
        if (plantDef?.speedTreePrefab == null) return;
        if (spawnedPrefabInstance != null) return;
        spawnedPrefabInstance = Instantiate(plantDef.speedTreePrefab, transform);
        spawnedPrefabInstance.transform.localPosition = Vector3.zero;
        spawnedPrefabInstance.transform.localRotation = Quaternion.identity;
        float s = Mathf.Lerp(0.25f, 1f, growth01);
        spawnedPrefabInstance.transform.localScale = Vector3.one * s;
    }

    public void TickGrowth(float dt)
    {
        if (plantDef == null) return;
        growth01 = Mathf.MoveTowards(growth01, plantDef.endGrowth01, dt * 0.05f);
        if (spawnedPrefabInstance != null)
        {
            float s = Mathf.Lerp(0.25f, 1f, growth01);
            spawnedPrefabInstance.transform.localScale = Vector3.one * s;
        }
        else
            EnsureSpeedTreeInstance();
        TryAdvanceStage();
    }

    public bool TryAdvanceStage()
    {
        if (plantDef == null || plantDef.grownStages == null) return false;
        if (stageIndex >= plantDef.grownStages.Count - 1) return false;
        if (UnityEngine.Random.value > Mathf.Clamp01(nextSectionSpawnChance))
            return false;

        // Severity 1 cuts block next section growth on associated section.
        for (int i = 0; i < cuts.Count; i++)
        {
            if (cuts[i] == null || cuts[i].forgotten) continue;
            if (cuts[i].severity01 >= 1f - 1e-4f && cuts[i].sectionId == stageIndex)
                return false;
        }

        var prevCuts = CaptureCutsForCarry();
        stageIndex++;
        var stage = plantDef.grownStages[stageIndex];
        ApplyStageMeshes(stage);
        if (stage.changeParticles != null)
            stage.changeParticles.Play();
        if (stage.stageBt != null)
            stage.stageBt.SendMessage("OnLotGrassStage", stage.stageId, SendMessageOptions.DontRequireReceiver);

        // Carry cut length + severity onto new section; reduce spawn chance.
        for (int i = 0; i < prevCuts.Count; i++)
        {
            var c = prevCuts[i];
            cuts.Add(new LotGrassCutMemory
            {
                sectionId = stageIndex,
                localPoint = c.localPoint,
                cutLength = c.cutLength,
                severity01 = c.severity01,
                parentSectionId = c.sectionId
            });
            nextSectionSpawnChance *= Mathf.Lerp(1f, 0.35f, c.severity01);
        }
        return true;
    }

    void ApplyStageMeshes(LotGrassGrowthStage stage)
    {
        if (meshFilter == null || stage == null) return;
        if (stage.meshB != null)
            meshFilter.sharedMesh = stage.meshB;
        else if (stage.meshA != null)
            meshFilter.sharedMesh = stage.meshA;
        if (morphMaterial != null && meshRenderer != null)
        {
            meshRenderer.sharedMaterial = morphMaterial;
            if (morphMaterial.HasProperty("_Blend"))
                morphMaterial.SetFloat("_Blend", 1f);
        }
    }

    public void ApplyCut(Vector3 worldPoint, float cutLength, float severity01, int leafSectionId)
    {
        severity01 = Mathf.Clamp01(severity01);
        int section = ResolveSectionLeafToRoot(leafSectionId);
        if (severity01 <= 1e-4f)
        {
            // 0 = cannot cut through
            return;
        }

        ForgetCutsAbove(section);

        cuts.Add(new LotGrassCutMemory
        {
            sectionId = section,
            localPoint = transform.InverseTransformPoint(worldPoint),
            cutLength = cutLength,
            severity01 = severity01,
            parentSectionId = sectionParent != null && section >= 0 && section < sectionParent.Count
                ? sectionParent[section]
                : -1
        });
        nextSectionSpawnChance *= Mathf.Lerp(1f, 0.5f, severity01);
        SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.CutGrass,
            SendMessageOptions.DontRequireReceiver);
    }

    public int ResolveSectionLeafToRoot(int leafSectionId)
    {
        int cur = leafSectionId;
        int guard = 32;
        while (guard-- > 0 && cur >= 0 && sectionParent != null && cur < sectionParent.Count)
        {
            int parent = sectionParent[cur];
            if (parent < 0) break;
            cur = parent;
        }
        return Mathf.Max(0, cur);
    }

    public void ForgetCutsAbove(int keepSectionId)
    {
        for (int i = 0; i < cuts.Count; i++)
        {
            if (cuts[i] == null || cuts[i].forgotten) continue;
            if (cuts[i].sectionId > keepSectionId)
                cuts[i].forgotten = true;
        }
    }

    List<LotGrassCutMemory> CaptureCutsForCarry()
    {
        var list = new List<LotGrassCutMemory>();
        for (int i = 0; i < cuts.Count; i++)
        {
            if (cuts[i] == null || cuts[i].forgotten) continue;
            if (cuts[i].sectionId == stageIndex)
                list.Add(cuts[i]);
        }
        return list;
    }
}
