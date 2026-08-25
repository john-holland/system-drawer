using System.Collections.Generic;
using UnityEngine;

/// <summary>Editor-only cell selection for CityPixelGrid designers. Click or drag a rectangle; Shift adds, Ctrl/Cmd toggles.</summary>
public sealed class CityPixelGridCellSelection
{
    public readonly HashSet<Vector2Int> Cells = new HashSet<Vector2Int>();

    Vector2Int _anchor;
    HashSet<Vector2Int> _strokeBase = new HashSet<Vector2Int>();
    bool _dragging;
    bool _additive;
    bool _toggle;

    public int Count => Cells.Count;
    public bool Dragging => _dragging;

    public bool Contains(int x, int y) => Cells.Contains(new Vector2Int(x, y));

    public void Clear()
    {
        Cells.Clear();
        _strokeBase.Clear();
        _dragging = false;
    }

    public void Begin(int x, int y, bool additive, bool toggle)
    {
        _anchor = new Vector2Int(x, y);
        _additive = additive;
        _toggle = toggle && !additive;
        _dragging = true;
        _strokeBase.Clear();
        if (_additive || _toggle)
        {
            foreach (var c in Cells)
                _strokeBase.Add(c);
        }
        else
            Cells.Clear();
        ApplyRect(_anchor.x, _anchor.y, x, y);
    }

    public void DragTo(int x, int y)
    {
        if (!_dragging)
        {
            Begin(x, y, false, false);
            return;
        }
        ApplyRect(_anchor.x, _anchor.y, x, y);
    }

    public void EndDrag()
    {
        _dragging = false;
        _strokeBase.Clear();
    }

    void ApplyRect(int ax, int ay, int bx, int by)
    {
        Cells.Clear();
        foreach (var c in _strokeBase)
            Cells.Add(c);

        int x0 = ax < bx ? ax : bx;
        int x1 = ax < bx ? bx : ax;
        int y0 = ay < by ? ay : by;
        int y1 = ay < by ? by : ay;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var p = new Vector2Int(x, y);
            if (_toggle && _strokeBase.Contains(p))
                Cells.Remove(p);
            else
                Cells.Add(p);
        }
    }
}
