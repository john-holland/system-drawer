using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Audio;

/// <summary>Mill grade card: Scribe/PenInk on UV bounds, user dialog grade, OCR scaffold fallback.</summary>
[Serializable]
public class WritingCard : GoodSection
{
    public ScribeCard scribe;
    public Rect uvBounds = new Rect(0f, 0f, 1f, 1f);
    public string dialogGrade = "select";
    public string ocrText;
    public bool usedDialogFallback;
    public PaintCanvas canvasSlot;
    public Texture2D stampImage;

    public WritingCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "writing_grade";
        sectionName = "writing_grade";
    }

    public static WritingCard GenerateGrade(string presetGrade = "select", Rect? uv = null)
    {
        var card = new WritingCard
        {
            dialogGrade = string.IsNullOrEmpty(presetGrade) ? "select" : presetGrade,
            uvBounds = uv ?? new Rect(0f, 0f, 1f, 1f),
            scribe = ScribeCard.Generate(ScribeActivity.Copy, "lumber_grade"),
            sectionName = "writing_grade",
            description = "Grade lumber face"
        };
        return card;
    }

    public string TryOcrThenDialog(Texture2D image)
    {
        stampImage = image;
        var doc = OcrSheetMusicImporter.ImportFromImage(image);
        bool empty = doc == null || doc.events == null || doc.events.Length == 0
                     || string.Equals(doc.title, "ocr-disabled", StringComparison.Ordinal);
        if (empty)
        {
            usedDialogFallback = true;
            ocrText = dialogGrade;
            return dialogGrade;
        }
        usedDialogFallback = false;
        ocrText = doc.title;
        return ocrText;
    }
}

[Serializable]
public sealed class WoodPileSlot
{
    public int depthIndex;
    public Vector3 localPos;
    public float fallImpulse = 2f;
    public string moveLogIkModeId = "move_log";
}

[CreateAssetMenu(fileName = "WoodPile", menuName = "Locomotion/Civil/Wood Pile")]
public sealed class WoodPileSpec : ScriptableObject
{
    public List<WoodPileSlot> slots = new List<WoodPileSlot>();
    public GarageDoorSgPackSettings pack = new GarageDoorSgPackSettings();
    public PixelLightGridMountGameObject brushMount;
    public int depthLayers = 3;
    public string moveLogIkModeId = "move_log";

    public void AddQuadtreeBucket(int depthIndex, Vector3 localPos)
    {
        slots.Add(new WoodPileSlot
        {
            depthIndex = Mathf.Max(0, depthIndex),
            localPos = localPos,
            moveLogIkModeId = moveLogIkModeId
        });
        slots.Sort((a, b) => a.depthIndex.CompareTo(b.depthIndex));
    }

    public int SlotCountAtDepth(int depth)
    {
        int n = 0;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null && slots[i].depthIndex == depth)
                n++;
        return n;
    }
}

/// <summary>Rope bind around logs with high-μ capsules and optional planar-skew bake.</summary>
[AddComponentMenu("Locomotion/Civil/Physics Super Deformo")]
public sealed class PhysicsSuperDeformo : MonoBehaviour
{
    public RopeSystem bind;
    public List<CapsuleCollider> logCapsules = new List<CapsuleCollider>();
    public float frictionMu = 1.4f;
    public float tensionOverrideN;
    public Renderer bakeMesh;
    public Material skewMaterial;

    public void BindLogs(Transform head, Transform tail)
    {
        if (bind == null)
            bind = GetComponent<RopeSystem>();
        if (bind != null)
            bind.BindAnchors(head, tail);
        ApplyHighMu();
    }

    public void ApplyHighMu()
    {
        var mat = new PhysicsMaterial
        {
            name = "wood_pile_mu",
            dynamicFriction = frictionMu,
            staticFriction = frictionMu,
            frictionCombine = PhysicsMaterialCombine.Maximum
        };
        for (int i = 0; i < logCapsules.Count; i++)
        {
            if (logCapsules[i] == null) continue;
            logCapsules[i].sharedMaterial = mat;
        }
    }

    public float TensionN()
    {
        if (bind == null) return tensionOverrideN;
        return tensionOverrideN > 1e-3f ? tensionOverrideN : bind.MaxTensionN;
    }
}

[CreateAssetMenu(fileName = "SawdustHose", menuName = "Locomotion/Civil/Sawdust Hose")]
public sealed class SawdustHoseSpec : ScriptableObject
{
    public Vector3 hoseStart;
    public Vector3 hoseEnd;
    public float nozzleRadiusM = 0.08f;
    [Range(0f, 1f)] public float vacuumThroughput01 = 0.6f;

    public int Prebake(DiggableVolume volume, DigScoopSph sph)
    {
        if (volume == null) return 0;
        sph ??= new DigScoopSph();
        float amount = Mathf.Lerp(0.04f, 0.25f, vacuumThroughput01);
        return volume.ApplyScoop(sph, hoseEnd, amount);
    }

    public float HoseLengthM() => Vector3.Distance(hoseStart, hoseEnd);
}

/// <summary>Chipper station: SPH infeed + SDF Subtract.</summary>
[AddComponentMenu("Locomotion/Civil/Chipper Station")]
public sealed class ChipperStation : MonoBehaviour
{
    public Transform infeed;
    public DiggableVolume volume;
    public DigScoopSph sph = new DigScoopSph();
    [Range(0f, 1f)] public float infeed01 = 0.5f;

    public int TickInfeed()
    {
        if (volume == null) return 0;
        Vector3 hit = infeed != null ? infeed.position : transform.position;
        return volume.ApplyScoop(sph, hit, Mathf.Lerp(0.05f, 0.3f, infeed01));
    }
}
