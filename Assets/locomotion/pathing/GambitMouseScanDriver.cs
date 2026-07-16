using UnityEngine;

/// <summary>Unscaled mouse scan over pathing apertures (hover highlight).</summary>
public sealed class GambitMouseScanDriver : MonoBehaviour
{
    public GambitSelectionSession session;
    public AngularTargetSelectMode selectMode;
    public GambitInputTriggerBuffer inputBuffer;
    public bool driveWhileEnabled = true;

    void Update()
    {
        if (!driveWhileEnabled || session == null || selectMode == null)
            return;
        // Use unscaled so scan works at timeScale 0.
        Vector2 screen = Input.mousePosition;
        if (selectMode.TryScan(screen, out var hit, out bool changed) && changed)
        {
            session.SetHovered(hit);
            if (inputBuffer != null)
                inputBuffer.Raise(GambitInputTriggerKind.MouseScan, hit);
        }
        else if (changed)
        {
            session.SetHovered(null);
            if (inputBuffer != null)
                inputBuffer.Raise(GambitInputTriggerKind.MouseScan, null);
        }
    }
}
