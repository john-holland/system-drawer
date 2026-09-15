using System;
using UnityEngine;

public enum TayloringCardKind
{
    Cut = 0,
    Fold = 1,
    Stitch = 2,
    Serge = 3,
    Dye = 4,
    StuffInvert = 5,
    ThreadPull = 6,
    ThreadKnot = 7,
    ThreadJam = 8
}

[Serializable]
public class TayloringCard : TravelAgentCard
{
    public TayloringCardKind kind;
    public ClothingStoreRagdoll store;
    public string commodityKey = ClothingCommodities.Bolt;
    public string foldCacheId;

    public TayloringCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "tayloring";
        traversabilityTag = "clothing_store";
    }

    public static void Fill(TayloringCard c, TayloringCardKind kind, DispatchRequest request, ClothingStoreRagdoll store)
    {
        if (c == null) return;
        c.kind = kind;
        c.store = store;
        c.sectionName = "tayloring_" + kind.ToString().ToLowerInvariant();
        c.description = kind.ToString();
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.physicalPathingTag = c.sectionName;
        if (request != null) c.goalWorld = request.worldTarget;
    }

    public bool ShelfAllows()
    {
        if (store?.store == null) return true;
        if (kind == TayloringCardKind.Cut)
            return store.CanCutBolt();
        return store.store.HasCommodity(commodityKey);
    }
}

[Serializable]
public sealed class TayloringCutCard : TayloringCard
{
    public static TayloringCutCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringCutCard();
        Fill(c, TayloringCardKind.Cut, request, store);
        c.commodityKey = ClothingCommodities.Bolt;
        return c;
    }
}

[Serializable]
public sealed class TayloringFoldCard : TayloringCard
{
    public static TayloringFoldCard Generate(DispatchRequest request, ClothingStoreRagdoll store, string cacheId = null)
    {
        var c = new TayloringFoldCard();
        Fill(c, TayloringCardKind.Fold, request, store);
        c.foldCacheId = cacheId;
        return c;
    }
}

[Serializable]
public sealed class TayloringStitchCard : TayloringCard
{
    public static TayloringStitchCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringStitchCard();
        Fill(c, TayloringCardKind.Stitch, request, store);
        return c;
    }
}

[Serializable]
public sealed class TayloringSergeCard : TayloringCard
{
    public static TayloringSergeCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringSergeCard();
        Fill(c, TayloringCardKind.Serge, request, store);
        return c;
    }
}

[Serializable]
public sealed class TayloringDyeCard : TayloringCard
{
    public static TayloringDyeCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringDyeCard();
        Fill(c, TayloringCardKind.Dye, request, store);
        c.commodityKey = ClothingCommodities.Dye;
        return c;
    }
}

[Serializable]
public sealed class TayloringStuffInvertCard : TayloringCard
{
    public static TayloringStuffInvertCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringStuffInvertCard();
        Fill(c, TayloringCardKind.StuffInvert, request, store);
        c.commodityKey = ClothingCommodities.Stuffing;
        return c;
    }
}

[Serializable]
public sealed class TayloringThreadPullCard : TayloringCard
{
    public static TayloringThreadPullCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringThreadPullCard();
        Fill(c, TayloringCardKind.ThreadPull, request, store);
        c.commodityKey = ClothingCommodities.Thread;
        return c;
    }
}

[Serializable]
public sealed class TayloringThreadKnotCard : TayloringCard
{
    public static TayloringThreadKnotCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringThreadKnotCard();
        Fill(c, TayloringCardKind.ThreadKnot, request, store);
        c.commodityKey = ClothingCommodities.Thread;
        return c;
    }
}

[Serializable]
public sealed class TayloringThreadJamCard : TayloringCard
{
    public static TayloringThreadJamCard Generate(DispatchRequest request, ClothingStoreRagdoll store)
    {
        var c = new TayloringThreadJamCard();
        Fill(c, TayloringCardKind.ThreadJam, request, store);
        c.commodityKey = ClothingCommodities.Thread;
        return c;
    }
}
