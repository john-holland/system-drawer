using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Store hours + tayloring cards. Hours via StationShiftSchedule.</summary>
[AddComponentMenu("Locomotion/Civil/Tayloring Bio Rhythm")]
public sealed class TayloringBioRhythm : DispatchBioRhythm
{
    public StoreBase store;
    public ClothingStoreRagdoll clothingStore;
    public StationShiftSchedule shifts;
    [Range(0f, 1f)] public float floorActivity01 = 0.45f;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "tayloring";
        hoursCron = "* 10-21 * * *";
        base.Awake();
        if (store == null)
            store = GetComponent<StoreBase>();
        if (clothingStore == null)
            clothingStore = GetComponent<ClothingStoreRagdoll>();
        if (shifts == null)
            shifts = GetComponent<StationShiftSchedule>();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        if (shifts != null)
            hoursCron = shifts.ResolvedHoursCron(hoursCron);
        base.Tick(utcNow, dt);
        store?.TickHours(utcNow);
        bool open = store != null ? store.isOpen : CronDue.IsActiveSchedule(hoursCron, utcNow);
        unitsAvailable01 = open ? Mathf.Clamp01(floorActivity01) : 0.05f;
        if (venueBio != null)
            venueBio.activity01 = open ? 0.35f + queueDepth01 * 0.4f : 0.05f;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = base.FacilitateCards(request);
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        var venue = clothingStore;
        if (kind == "cut" || kind == "tayloring_cut")
            cards.Add(TayloringCutCard.Generate(request, venue));
        if (kind == "fold" || kind == "tayloring_fold")
            cards.Add(TayloringFoldCard.Generate(request, venue));
        if (kind == "stitch" || kind == "tayloring_stitch")
            cards.Add(TayloringStitchCard.Generate(request, venue));
        if (kind == "serge" || kind == "tayloring_serge")
            cards.Add(TayloringSergeCard.Generate(request, venue));
        if (kind == "dye" || kind == "tayloring_dye")
            cards.Add(TayloringDyeCard.Generate(request, venue));
        if (kind == "stuff" || kind == "invert" || kind == "tayloring_stuff")
            cards.Add(TayloringStuffInvertCard.Generate(request, venue));
        if (kind == "thread_pull" || kind == "pull")
            cards.Add(TayloringThreadPullCard.Generate(request, venue));
        if (kind == "thread_knot" || kind == "knot")
            cards.Add(TayloringThreadKnotCard.Generate(request, venue));
        if (kind == "thread_jam" || kind == "jam")
            cards.Add(TayloringThreadJamCard.Generate(request, venue));
        if (kind == "delivery" || kind == "ship" || kind == "vehicle")
            cards.Add(TAVehicleDeliveryCard.Generate(request, "vehicle"));
        return cards;
    }
}
