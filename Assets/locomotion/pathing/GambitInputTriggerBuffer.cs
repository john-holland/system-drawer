using System;
using UnityEngine;

public enum GambitInputTriggerKind
{
    None,
    MouseScan,
    MouseClickConfirm,
    MouseClickCancel
}

/// <summary>Frame events for gambit mouse scan / click (works with unscaled time).</summary>
public sealed class GambitInputTriggerBuffer : MonoBehaviour
{
    public GambitInputTriggerKind lastTrigger = GambitInputTriggerKind.None;
    public PathingAperture lastScanAperture;
    public bool consumeOnRead = true;

    public event Action<GambitInputTriggerKind, PathingAperture> Triggered;

    public void Raise(GambitInputTriggerKind kind, PathingAperture aperture = null)
    {
        lastTrigger = kind;
        lastScanAperture = aperture;
        Triggered?.Invoke(kind, aperture);
    }

    public bool TryConsume(out GambitInputTriggerKind kind, out PathingAperture aperture)
    {
        kind = lastTrigger;
        aperture = lastScanAperture;
        if (kind == GambitInputTriggerKind.None)
            return false;
        if (consumeOnRead)
        {
            lastTrigger = GambitInputTriggerKind.None;
            lastScanAperture = null;
        }
        return true;
    }

    public void Inject(GambitInputTriggerKind kind, PathingAperture aperture = null)
    {
        Raise(kind, aperture);
    }

    public void Clear()
    {
        lastTrigger = GambitInputTriggerKind.None;
        lastScanAperture = null;
    }
}
