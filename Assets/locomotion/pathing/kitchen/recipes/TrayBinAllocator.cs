using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Allocates tray batches from servesAmount × actors with capacity limits.</summary>
public static class TrayBinAllocator
{
    public sealed class Batch
    {
        public int plateCount;
        public int batchIndex;
        public bool useTray;
        public bool singlePersonLoad;
    }

    public static int PlatesNeeded(float servesAmount, int actorCount, float platesPerActor = 1f)
    {
        int actors = Mathf.Max(1, actorCount);
        float per = Mathf.Max(0.01f, platesPerActor);
        return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0.01f, servesAmount) * per / actors) * actors);
    }

    public static List<Batch> BuildBatches(float servesAmount, int actorCount, TrayBinSettings settings, float platesPerActor = 1f)
    {
        var batches = new List<Batch>();
        settings = settings ?? new TrayBinSettings();
        int remaining = PlatesNeeded(servesAmount, actorCount, platesPerActor);
        int capacity = Mathf.Max(1, Mathf.Min(settings.maxPlateSlots, settings.maxCount));
        bool preferTray = servesAmount > 1f && capacity > 1;
        int index = 0;

        while (remaining > 0)
        {
            int take = preferTray ? Mathf.Min(capacity, remaining) : 1;
            bool single = take == 1 && settings.allowSinglePersonLoads;
            bool useTray = preferTray && take > 0;
            if (!useTray && !settings.allowSansTrayFallback && settings.allowSinglePersonLoads)
            {
                take = 1;
                single = true;
                useTray = false;
            }
            batches.Add(new Batch
            {
                plateCount = take,
                batchIndex = index++,
                useTray = useTray && take > 1,
                singlePersonLoad = single
            });
            remaining -= take;
            if (!preferTray || (take < capacity && settings.allowSansTrayFallback && remaining > 0 && capacity > 1))
            {
                // shrink toward single-person after pressure
                if (remaining > 0 && settings.allowSinglePersonLoads && capacity > 1 && index > 0)
                    capacity = 1;
            }
        }
        return batches;
    }

    /// <summary>After bailout: reduce tray size to single-person or sans-tray.</summary>
    public static List<Batch> ReduceAfterBailout(int remainingPlates, TrayBinSettings settings)
    {
        settings = settings ?? new TrayBinSettings();
        var batches = new List<Batch>();
        int rem = Mathf.Max(0, remainingPlates);
        int index = 0;
        while (rem > 0)
        {
            batches.Add(new Batch
            {
                plateCount = 1,
                batchIndex = index++,
                useTray = false,
                singlePersonLoad = true
            });
            rem--;
            if (!settings.allowSansTrayFallback && !settings.allowSinglePersonLoads)
                break;
        }
        return batches;
    }
}
