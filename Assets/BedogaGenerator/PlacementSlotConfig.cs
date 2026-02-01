using UnityEngine;

/// <summary>Config for placement slot layout: per-axis fit (center/left/right etc.) and stack/wrap direction. Used by oct/quad solvers.</summary>
public struct PlacementSlotConfig
{
    public SGBehaviorTreeNode.FitX fitX;
    public SGBehaviorTreeNode.FitY fitY;
    public SGBehaviorTreeNode.FitZ fitZ;
    public SGBehaviorTreeNode.AxisDirection stackDirection;
    public SGBehaviorTreeNode.AxisDirection wrapDirection;

    public static PlacementSlotConfig FromNode(SGBehaviorTreeNode node)
    {
        if (node == null)
            return default;
        return new PlacementSlotConfig
        {
            fitX = node.fitX,
            fitY = node.fitY,
            fitZ = node.fitZ,
            stackDirection = node.stackDirection,
            wrapDirection = node.wrapDirection
        };
    }

    public static bool ComputeSlotCenter3D(
        Bounds searchBounds,
        Vector3 optimalSpace,
        Vector3 minSpace,
        int placementIndex,
        PlacementSlotConfig? config,
        out Vector3 center)
    {
        float stepX = Mathf.Max(optimalSpace.x, minSpace.x * 0.5f);
        float stepY = Mathf.Max(optimalSpace.y, minSpace.y * 0.5f);
        float stepZ = Mathf.Max(optimalSpace.z, minSpace.z * 0.5f);
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        if (stepZ <= 0f) stepZ = 1f;
        Vector3 halfOpt = optimalSpace * 0.5f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int numZ = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.z - optimalSpace.z) / stepZ) + 1);
        int gridCount = numX * numY * numZ;
        int totalSlots = 1 + gridCount;
        int slot = placementIndex % Mathf.Max(1, totalSlots);

        if (!config.HasValue)
        {
            // Legacy: slot 0 = center, slots 1..N = row-major from min
            if (slot == 0)
            {
                center = searchBounds.center;
                return true;
            }
            if (numX <= 1 && numY <= 1 && numZ <= 1)
            {
                center = searchBounds.center;
                return true;
            }
            if (numX <= 1 || gridCount <= 1)
            {
                float x = searchBounds.min.x + halfOpt.x + (slot - 1) * stepX;
                center = new Vector3(x, searchBounds.center.y, searchBounds.center.z);
                return true;
            }
            int idx = slot - 1;
            int ix = idx % numX;
            int iy = (idx / numX) % numY;
            int iz = idx / (numX * numY);
            center = new Vector3(
                searchBounds.min.x + halfOpt.x + ix * stepX,
                searchBounds.min.y + halfOpt.y + iy * stepY,
                searchBounds.min.z + halfOpt.z + iz * stepZ);
            return true;
        }

        var c = config.Value;
        // Slot 0: anchor from fit (Center = center, Left/Down/Backward = min+halfOpt, Right/Up/Forward = max-halfOpt)
        if (slot == 0)
        {
            center = new Vector3(
                SlotZeroAnchorX(searchBounds, c.fitX, halfOpt.x),
                SlotZeroAnchorY(searchBounds, c.fitY, halfOpt.y),
                SlotZeroAnchorZ(searchBounds, c.fitZ, halfOpt.z));
            return true;
        }

        int stackAxis = AxisFromDirection(c.stackDirection);
        int wrapAxis = AxisFromDirection(c.wrapDirection);
        int remAxis = (stackAxis == wrapAxis) ? (stackAxis + 1) % 3 : (3 - stackAxis - wrapAxis);
        if (remAxis < 0 || remAxis > 2)
            remAxis = 0;

        int[] num = { numX, numY, numZ };
        int numStack = num[stackAxis];
        int numWrap = num[wrapAxis];
        int numRem = num[remAxis];
        int idx2 = slot - 1;
        int i0 = idx2 % numStack;
        int i1 = (idx2 / numStack) % numWrap;
        int i2 = idx2 / (numStack * numWrap);
        if (i2 >= numRem) { center = searchBounds.center; return false; }

        int ix2 = stackAxis == 0 ? i0 : (wrapAxis == 0 ? i1 : i2);
        int iy2 = stackAxis == 1 ? i0 : (wrapAxis == 1 ? i1 : i2);
        int iz2 = stackAxis == 2 ? i0 : (wrapAxis == 2 ? i1 : i2);

        center = new Vector3(
            AnchorX(searchBounds, c.fitX, numX, ix2, stepX, stepX, halfOpt.x),
            AnchorY(searchBounds, c.fitY, numY, iy2, stepY, stepY, halfOpt.y),
            AnchorZ(searchBounds, c.fitZ, numZ, iz2, stepZ, stepZ, halfOpt.z));
        return true;
    }

    private static int AxisFromDirection(SGBehaviorTreeNode.AxisDirection d)
    {
        switch (d)
        {
            case SGBehaviorTreeNode.AxisDirection.PosX:
            case SGBehaviorTreeNode.AxisDirection.NegX: return 0;
            case SGBehaviorTreeNode.AxisDirection.PosY:
            case SGBehaviorTreeNode.AxisDirection.NegY: return 1;
            default: return 2;
        }
    }

    private static float SlotZeroAnchorX(Bounds b, SGBehaviorTreeNode.FitX fit, float halfOpt)
    {
        switch (fit)
        {
            case SGBehaviorTreeNode.FitX.Left: return b.min.x + halfOpt;
            case SGBehaviorTreeNode.FitX.Right: return b.max.x - halfOpt;
            default: return b.center.x;
        }
    }
    private static float SlotZeroAnchorY(Bounds b, SGBehaviorTreeNode.FitY fit, float halfOpt)
    {
        switch (fit)
        {
            case SGBehaviorTreeNode.FitY.Down: return b.min.y + halfOpt;
            case SGBehaviorTreeNode.FitY.Up: return b.max.y - halfOpt;
            default: return b.center.y;
        }
    }
    private static float SlotZeroAnchorZ(Bounds b, SGBehaviorTreeNode.FitZ fit, float halfOpt)
    {
        switch (fit)
        {
            case SGBehaviorTreeNode.FitZ.Backward: return b.min.z + halfOpt;
            case SGBehaviorTreeNode.FitZ.Forward: return b.max.z - halfOpt;
            default: return b.center.z;
        }
    }

    private static float AnchorX(Bounds b, SGBehaviorTreeNode.FitX fit, int num, int i, float step, float stepUnused, float halfOpt)
    {
        if (num == 0) num = 1;
        switch (fit)
        {
            case SGBehaviorTreeNode.FitX.Left: return b.min.x + halfOpt + i * step;
            case SGBehaviorTreeNode.FitX.Right: return b.max.x - halfOpt - i * step;
            default: return b.center.x - (num - 1) * step * 0.5f + halfOpt + i * step;
        }
    }

    private static float AnchorY(Bounds b, SGBehaviorTreeNode.FitY fit, int num, int i, float step, float stepUnused, float halfOpt)
    {
        if (num == 0) num = 1;
        switch (fit)
        {
            case SGBehaviorTreeNode.FitY.Down: return b.min.y + halfOpt + i * step;
            case SGBehaviorTreeNode.FitY.Up: return b.max.y - halfOpt - i * step;
            default: return b.center.y - (num - 1) * step * 0.5f + halfOpt + i * step;
        }
    }

    private static float AnchorZ(Bounds b, SGBehaviorTreeNode.FitZ fit, int num, int i, float step, float stepUnused, float halfOpt)
    {
        if (num == 0) num = 1;
        switch (fit)
        {
            case SGBehaviorTreeNode.FitZ.Backward: return b.min.z + halfOpt + i * step;
            case SGBehaviorTreeNode.FitZ.Forward: return b.max.z - halfOpt - i * step;
            default: return b.center.z - (num - 1) * step * 0.5f + halfOpt + i * step;
        }
    }

    /// <summary>2D variant for quad solver: fitX, fitY; Z = searchBounds.center.z. Stack and wrap use X and Y only.</summary>
    public static bool ComputeSlotCenter2D(
        Bounds searchBounds,
        Vector3 optimalSpace,
        Vector3 minSpace,
        int placementIndex,
        PlacementSlotConfig? config,
        out Vector3 center)
    {
        float stepX = Mathf.Max(optimalSpace.x, minSpace.x * 0.5f);
        float stepY = Mathf.Max(optimalSpace.y, minSpace.y * 0.5f);
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        float halfXOpt = optimalSpace.x * 0.5f;
        float halfYOpt = optimalSpace.y * 0.5f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int gridCount = numX * numY;
        int totalSlots = 1 + gridCount;
        int slot = placementIndex % Mathf.Max(1, totalSlots);
        float z = searchBounds.center.z;

        if (!config.HasValue)
        {
            if (slot == 0) { center = new Vector3(searchBounds.center.x, searchBounds.center.y, z); return true; }
            if (numX <= 1 || gridCount <= 1)
            {
                float x = searchBounds.min.x + halfXOpt + (slot - 1) * stepX;
                center = new Vector3(x, searchBounds.center.y, z);
                return true;
            }
            int idx = slot - 1;
            int col = idx % numX;
            int row = idx / numX;
            center = new Vector3(
                searchBounds.min.x + halfXOpt + col * stepX,
                searchBounds.min.y + halfYOpt + row * stepY,
                z);
            return true;
        }

        var c = config.Value;
        if (slot == 0)
        {
            center = new Vector3(
                SlotZeroAnchorX(searchBounds, c.fitX, halfXOpt),
                SlotZeroAnchorY(searchBounds, c.fitY, halfYOpt),
                z);
            return true;
        }

        // 2D: stack and wrap are the two axes (0=X, 1=Y). Determine which is first/second from stackDirection/wrapDirection.
        int stackAxis = AxisFromDirection(c.stackDirection);
        int wrapAxis = AxisFromDirection(c.wrapDirection);
        if (stackAxis == 2) stackAxis = 0;
        if (wrapAxis == 2) wrapAxis = 1;
        if (wrapAxis == stackAxis) wrapAxis = (stackAxis == 0) ? 1 : 0;
        int numStack = stackAxis == 0 ? numX : numY;
        int numWrap = wrapAxis == 0 ? numX : numY;
        int idx2 = slot - 1;
        int i0 = idx2 % numStack;
        int i1 = idx2 / numStack;
        int ix2 = stackAxis == 0 ? i0 : i1;
        int iy2 = stackAxis == 1 ? i0 : i1;

        center = new Vector3(
            AnchorX(searchBounds, c.fitX, numX, ix2, stepX, stepX, halfXOpt),
            AnchorY(searchBounds, c.fitY, numY, iy2, stepY, stepY, halfYOpt),
            z);
        return true;
    }
}
