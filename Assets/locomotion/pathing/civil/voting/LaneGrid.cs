using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LaneGridCell
{
    public int x;
    public int y;
    public Transform anchor;
    public BaseAmbulatingActor occupant;
}

/// <summary>
/// Pedestrian ambulation grid for polling queues. Not a road lane grid.
/// FIFO enqueue toward booths.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Lane Grid")]
public sealed class LaneGrid : MonoBehaviour
{
    public int width = 4;
    public int height = 8;
    public float cellSize = 0.8f;
    public List<LaneGridCell> cells = new List<LaneGridCell>();
    public List<BaseAmbulatingActor> queue = new List<BaseAmbulatingActor>();

    public int OccupiedCount
    {
        get
        {
            int n = 0;
            if (cells == null) return 0;
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] != null && cells[i].occupant != null)
                    n++;
            return n;
        }
    }

    public void EnsureCells()
    {
        int need = Mathf.Max(1, width) * Mathf.Max(1, height);
        if (cells == null)
            cells = new List<LaneGridCell>(need);
        if (cells.Count >= need)
            return;
        cells.Clear();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var t = new GameObject($"lane_{x}_{y}").transform;
            t.SetParent(transform, false);
            t.localPosition = new Vector3(x * cellSize, 0f, y * cellSize);
            cells.Add(new LaneGridCell { x = x, y = y, anchor = t });
        }
    }

    public bool TryEnqueue(BaseAmbulatingActor actor)
    {
        if (actor == null) return false;
        EnsureCells();
        if (queue == null) queue = new List<BaseAmbulatingActor>();
        for (int i = 0; i < queue.Count; i++)
            if (queue[i] == actor)
                return true;
        var cell = FirstEmpty();
        if (cell == null) return false;
        cell.occupant = actor;
        queue.Add(actor);
        if (cell.anchor != null)
            actor.transform.position = cell.anchor.position;
        return true;
    }

    public BaseAmbulatingActor Peek()
    {
        if (queue == null || queue.Count == 0) return null;
        return queue[0];
    }

    public BaseAmbulatingActor TryDequeueToBooth()
    {
        if (queue == null || queue.Count == 0) return null;
        var actor = queue[0];
        queue.RemoveAt(0);
        ClearOccupant(actor);
        return actor;
    }

    void ClearOccupant(BaseAmbulatingActor actor)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Count; i++)
            if (cells[i] != null && cells[i].occupant == actor)
                cells[i].occupant = null;
    }

    LaneGridCell FirstEmpty()
    {
        if (cells == null) return null;
        for (int i = 0; i < cells.Count; i++)
            if (cells[i] != null && cells[i].occupant == null)
                return cells[i];
        return null;
    }
}
