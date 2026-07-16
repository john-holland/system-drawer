using UnityEngine;

/// <summary>LMB confirm / RMB or Esc cancel for gambit aperture selection (unscaled).</summary>
public sealed class GambitMouseClickDriver : MonoBehaviour
{
    public GambitSelectionSession session;
    public GambitInputTriggerBuffer inputBuffer;
    public int confirmMouseButton = 0;
    public int cancelMouseButton = 1;
    public KeyCode cancelKey = KeyCode.Escape;
    public bool driveWhileEnabled = true;

    void Update()
    {
        if (!driveWhileEnabled || inputBuffer == null)
            return;

        if (Input.GetMouseButtonDown(confirmMouseButton))
        {
            var hover = session != null ? session.hoveredAperture : null;
            inputBuffer.Raise(GambitInputTriggerKind.MouseClickConfirm, hover);
        }
        else if (Input.GetMouseButtonDown(cancelMouseButton) || Input.GetKeyDown(cancelKey))
        {
            inputBuffer.Raise(GambitInputTriggerKind.MouseClickCancel, null);
        }
    }
}
