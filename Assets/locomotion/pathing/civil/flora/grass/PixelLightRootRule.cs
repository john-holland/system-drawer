using System;
using System.Collections.Generic;
using UnityEngine;

public enum PixelLightRootStage
{
    Node = 0,
    Grass = 1
}

public enum PixelLightRootAccess
{
    Allow = 0,
    Conflict = 1
}

[Serializable]
public sealed class PixelLightRootRule
{
    public int cellX;
    public int cellY;
    public PixelLightRootStage stage = PixelLightRootStage.Node;
    public PixelLightRootAccess access = PixelLightRootAccess.Allow;
}

public static class PixelLightRootDesigner
{
    public static int Autopopulate(PixelLightGridMountGameObject mount, int spacing)
    {
        if (mount == null)
            return 0;
        if (mount.rootRules == null)
            mount.rootRules = new List<PixelLightRootRule>();
        int step = Mathf.Max(1, spacing);
        int placed = 0;
        for (int y = 0; y < mount.gridHeight; y += step)
        {
            for (int x = 0; x < mount.gridWidth; x += step)
            {
                PixelLightRootStage stage = ((x + y) / step) % 2 == 0
                    ? PixelLightRootStage.Node
                    : PixelLightRootStage.Grass;
                if (TryPlace(mount, x, y, stage))
                    placed++;
            }
        }
        return placed;
    }

    public static bool TryPlace(PixelLightGridMountGameObject mount, int x, int y, PixelLightRootStage stage)
    {
        if (mount == null)
            return false;
        if (mount.rootRules == null)
            mount.rootRules = new List<PixelLightRootRule>();
        PixelLightRootRule existing = Find(mount, x, y);
        if (existing == null)
        {
            mount.rootRules.Add(new PixelLightRootRule
            {
                cellX = x,
                cellY = y,
                stage = stage,
                access = PixelLightRootAccess.Allow
            });
            return true;
        }
        if (mount.allowAllRoots || existing.stage == stage)
        {
            existing.stage = stage;
            existing.access = PixelLightRootAccess.Allow;
            return true;
        }
        existing.access = PixelLightRootAccess.Conflict;
        return false;
    }

    public static PixelLightRootRule Find(PixelLightGridMountGameObject mount, int x, int y)
    {
        if (mount?.rootRules == null)
            return null;
        for (int i = 0; i < mount.rootRules.Count; i++)
        {
            PixelLightRootRule rule = mount.rootRules[i];
            if (rule != null && rule.cellX == x && rule.cellY == y)
                return rule;
        }
        return null;
    }
}
