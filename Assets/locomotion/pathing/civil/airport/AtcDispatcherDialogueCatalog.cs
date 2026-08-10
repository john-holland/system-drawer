using System;
using System.Collections.Generic;

[Serializable]
public sealed class AtcDispatcherDialogueEntry
{
    public string dispatchKind;
    public string dialogueSetId;
    public string goalName;
}

/// <summary>Maps AirportDispatchKinds / narrative actions → ATC dispatcher dialogue-set ids.</summary>
[Serializable]
public sealed class AtcDispatcherDialogueCatalog
{
    public List<AtcDispatcherDialogueEntry> entries = new List<AtcDispatcherDialogueEntry>();

    public void EnsureDefaults()
    {
        if (entries == null) entries = new List<AtcDispatcherDialogueEntry>();
        if (entries.Count > 0) return;
        Add(AirportDispatchKinds.AtcAllClear, "atc-dispatcher-all-clear", "clearance");
        Add(AirportDispatchKinds.AtcTakeOff, "atc-dispatcher-takeoff", "clearance");
        Add(AirportDispatchKinds.AtcHolding, "atc-dispatcher-hold", "hold");
        Add(AirportDispatchKinds.AtcLanding, "atc-dispatcher-landing", "clearance");
        Add(AirportDispatchKinds.TsaDisaster, "atc-dispatcher-divert-potty", "divert");
        Add(AirportDispatchKinds.AtcHandoff, "atc-dispatcher-handoff", "handoff");
        Add(AirportDispatchKinds.AtcRefuelClearance, "atc-dispatcher-refuel", "refuel");
        Add(AirportDispatchKinds.PilotGate, "atc-dispatcher-ground-taxi", "taxi");
    }

    void Add(string kind, string setId, string goal)
    {
        entries.Add(new AtcDispatcherDialogueEntry
        {
            dispatchKind = kind,
            dialogueSetId = setId,
            goalName = goal
        });
    }

    public AtcDispatcherDialogueEntry Find(string dispatchKind)
    {
        EnsureDefaults();
        if (string.IsNullOrEmpty(dispatchKind)) return null;
        string k = dispatchKind.ToLowerInvariant();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && string.Equals(entries[i].dispatchKind, k, StringComparison.OrdinalIgnoreCase))
                return entries[i];
        }
        return null;
    }

    public string DialogueSetFor(string dispatchKind) => Find(dispatchKind)?.dialogueSetId;
}
