using System.Collections.Generic;
using UnityEngine;

/// <summary>Picks catalog candidate by height-inclusive footprint leftover score.</summary>
public static class CityPlaceableBestFit
{
    public static CityPlaceableCandidate Pick(CityPlaceableChunk chunk, CityPlaceableCatalog catalog)
    {
        if (chunk == null) return null;

        int needW = Mathf.Max(1, chunk.maxX - chunk.minX + 1);
        int needD = Mathf.Max(1, chunk.maxY - chunk.minY + 1);
        int needH = Mathf.Max(1, chunk.heightCells);

        if (catalog != null && !string.IsNullOrEmpty(chunk.forcedCandidateId))
        {
            var forced = catalog.FindById(chunk.forcedCandidateId);
            if (forced != null) return forced;
        }

        if (catalog == null) return null;

        var candidates = catalog.FindMatching(chunk.placeableKind, chunk.typeKey, chunk.isSharedShell);
        if (candidates.Count == 0 && chunk.isSharedShell)
            candidates = catalog.FindMatching(chunk.placeableKind, "shared_building", sharedShell: true);

        CityPlaceableCandidate bestFit = null;
        float bestScore = float.PositiveInfinity;
        CityPlaceableCandidate leastOverflow = null;
        float bestOverflow = float.PositiveInfinity;

        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c == null) continue;
            if (chunk.isSharedShell)
            {
                if (!c.sharedBuildingCompatible) continue;
                if (c.sg3dPromptComposition != null && !c.sg3dSharedBuildingCompatible) continue;
                for (int t = 0; t < chunk.tenantTypeKeys.Count; t++)
                    if (!c.AllowsTenant(chunk.tenantTypeKeys[t]))
                        goto NextCandidate;
            }

            float leftover = (c.footprintCellsX - needW) + (c.footprintCellsZ - needD) + (c.footprintCellsY - needH);
            leftover -= c.scoreBias01;
            float aspectErr = AspectError(needW, needD, c.footprintCellsX, c.footprintCellsZ);
            float score = leftover + aspectErr * 0.25f;

            if (c.Fits(needW, needD, needH))
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestFit = c;
                }
            }
            else
            {
                // Undersized shell: how much volume is still missing.
                float overflow = Mathf.Max(0, needW - c.footprintCellsX)
                                + Mathf.Max(0, needD - c.footprintCellsZ)
                                + Mathf.Max(0, needH - c.footprintCellsY);
                if (overflow < bestOverflow)
                {
                    bestOverflow = overflow;
                    leastOverflow = c;
                }
            }

            NextCandidate: ;
        }

        if (bestFit != null) return bestFit;
        if (leastOverflow != null)
        {
            Debug.LogWarning(
                $"[CityPlaceableBestFit] No candidate fits chunk {chunk.typeKey} {needW}x{needD}x{needH}; using least overflow '{leastOverflow.id}'.");
            return leastOverflow;
        }
        return null;
    }

    static float AspectError(int needW, int needD, int cx, int cz)
    {
        float a = needW / (float)Mathf.Max(1, needD);
        float b = cx / (float)Mathf.Max(1, cz);
        return Mathf.Abs(a - b);
    }
}
