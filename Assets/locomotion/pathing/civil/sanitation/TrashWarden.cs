using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class TrashTargetScore
{
    public GameObject target;
    public TrashBinRuntime bin;
    public float fill01;
    public float Priority => fill01;
}

/// <summary>Scores full bins/houses, releases garbage trucks; bin-empty / shake-out predicates.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Trash Warden")]
public sealed class TrashWarden : MonoBehaviour
{
    public SanitationFacilityRuntime facility;
    public SanitationFacilityBioRhythm bio;
    public float releaseFillThreshold01 = 0.45f;
    public readonly List<TrashTargetScore> targets = new List<TrashTargetScore>();
    public int trucksReleased;

    void Awake()
    {
        if (facility == null)
            facility = GetComponent<SanitationFacilityRuntime>()
                       ?? GetComponentInParent<SanitationFacilityRuntime>();
        if (bio == null)
            bio = GetComponent<SanitationFacilityBioRhythm>();
    }

    public void Tick(float dt)
    {
        ScanTargets();
        if (targets.Count > 0 && targets[0].fill01 >= releaseFillThreshold01)
            TryReleaseTrucks();
    }

    public void ScanTargets()
    {
        targets.Clear();
        var bins = FindObjectsByType<TrashBinRuntime>(FindObjectsSortMode.None);
        for (int i = 0; i < bins.Length; i++)
        {
            var b = bins[i];
            if (b == null || b.fill01 < 0.1f) continue;
            targets.Add(new TrashTargetScore
            {
                target = b.gameObject,
                bin = b,
                fill01 = b.fill01
            });
        }
        var houses = FindObjectsByType<HouseBioRhythm>(FindObjectsSortMode.None);
        for (int i = 0; i < houses.Length; i++)
        {
            var h = houses[i];
            if (h == null || h.trashFill01 < 0.1f) continue;
            if (h.GetComponentInChildren<TrashBinRuntime>() != null) continue;
            targets.Add(new TrashTargetScore
            {
                target = h.gameObject,
                fill01 = h.trashFill01
            });
        }
        targets.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public bool TryReleaseTrucks()
    {
        if (facility?.dockedTrucks == null) return false;
        trucksReleased = 0;
        for (int i = 0; i < facility.dockedTrucks.Count; i++)
        {
            var t = facility.dockedTrucks[i];
            if (t == null || !t.available) continue;
            t.DispatchToPickup(targets.Count > 0 ? targets[0].target : null);
            trucksReleased++;
            if (trucksReleased >= 2) break;
        }
        return trucksReleased > 0;
    }

    public bool IsBinEmpty(TrashBinRuntime bin) => bin == null || bin.IsEmpty;

    /// <summary>Predicate for shake-trash-out BT node — only shake when bin still has content after lift.</summary>
    public bool ShouldShakeOut(TrashBinRuntime bin)
    {
        if (bin == null) return false;
        if (IsBinEmpty(bin)) return false;
        return bin.fill01 > 0.08f || bin.contents.massKg > 0.2f;
    }

    public PetJudgment JudgeBinInteract(TrashBinRuntime bin, Object actor)
    {
        if (bin == null) return PetJudgment.Deny;
        if (IsBinEmpty(bin)) return PetJudgment.SoftRedirect;
        var rd = actor as RagdollSystem;
        if (rd == null && actor is Component c)
            rd = c.GetComponentInParent<RagdollSystem>();
        if (rd != null)
        {
            var opinion = rd.OpinionFor(bin);
            if (opinion.dislike01 > 0.85f) return PetJudgment.Deny;
        }
        return PetJudgment.Allow;
    }
}
