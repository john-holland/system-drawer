using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainSeatEntranceSide
{
    Door = 0,
    Left = 1,
    Right = 2,
    Bottom = 3,
    Top = 4
}

[Serializable]
public sealed class TrainSeatEntranceRow
{
    public int rowIndex;
    public TrainSeatEntranceSide side = TrainSeatEntranceSide.Door;
}

/// <summary>Continuuuum seat-ticket page payload — car grid + seat prefab-id → seated BT.</summary>
[Serializable]
public sealed class TrainSeatTicketConfig
{
    public int carNumber = 1;
    public int seatTotal = 40;
    public int leftGridWidth = 2;
    public int rightGridWidth = 2;
    public List<float> rowGaps = new List<float>();
    public List<TrainSeatEntranceRow> entranceRows = new List<TrainSeatEntranceRow>();
    public string seatTypePrefabId = "train_seat_default";
    public string seatedAnimationBtPrefabId;

    public void ApplyTo(TrainVehicleRagdoll car)
    {
        if (car == null) return;
        if (car.aislePath != null)
        {
            float aisleW = Mathf.Max(0.4f, 1.2f - 0.05f * (leftGridWidth + rightGridWidth));
            car.aislePath.defaultWidth = aisleW;
            car.aislePath.Rebuild();
        }
        car.SendMessage("OnTrainSeatTicketApplied", this, SendMessageOptions.DontRequireReceiver);
    }
}
