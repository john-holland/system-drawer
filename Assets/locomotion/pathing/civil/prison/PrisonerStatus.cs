using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrisonerStatus
{
    Arrested = 0,
    Holding = 1,
    Trial = 2,
    Bail = 3,
    Sentenced = 4,
    Intake = 5,
    Custody = 6,
    Parole = 7,
    Rehab = 8,
    Outing = 9
}

[Serializable]
public sealed class PrisonerRecord
{
    public string prisonerId;
    public string personaKey;
    public PrisonerStatus status = PrisonerStatus.Custody;
    public string cellId;
    public string switcherooPackId;
    public float bailUsd;
    public float sentenceDays;
    public bool paroleEligible;
    public string outingCron = "0 9-15 * * 6";
}

[Serializable]
public sealed class PrisonerSwitcherooPack
{
    public string packId = "standard";
    public string label = "Standard";
    public GameObject appearancePrefab;
    public string personaPackId;
}

/// <summary>SimCity-4-style style packs on the same cell footprint (appearance / classification, not new floorplans).</summary>
[CreateAssetMenu(fileName = "PrisonerSwitcherooCatalog", menuName = "Locomotion/Civil/Prisoner Switcheroo Catalog")]
public sealed class PrisonerSwitcherooCatalog : ScriptableObject
{
    public List<PrisonerSwitcherooPack> packs = new List<PrisonerSwitcherooPack>();

    public PrisonerSwitcherooPack Find(string packId)
    {
        if (packs == null || string.IsNullOrEmpty(packId)) return null;
        for (int i = 0; i < packs.Count; i++)
        {
            if (packs[i] != null && packs[i].packId == packId)
                return packs[i];
        }
        return packs.Count > 0 ? packs[0] : null;
    }

    public PrisonerSwitcherooPack ApplyAtSpawn(PrisonerRecord record)
    {
        if (record == null) return null;
        var pack = Find(record.switcherooPackId);
        if (pack != null)
            record.switcherooPackId = pack.packId;
        return pack;
    }
}
