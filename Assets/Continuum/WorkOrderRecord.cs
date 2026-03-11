using System;

/// <summary>
/// Fungible work order for dev studio: derived from causality tree (linear or hub-and-spoke).
/// </summary>
[Serializable]
public class WorkOrderRecord
{
    public string id;
    public string episodeId;
    public string causalityLeafId;
    public string assetId;
    public string narrativeType;  // "linear" or "hub_and_spoke"
    public string dependsOn;  // JSON array of work order IDs
    public string promptDescription;
    public string status = "pending";  // "pending", "assigned", "in_progress", "done"
    public string assignedTo;
}
