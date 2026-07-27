using UnityEngine;

/// <summary>
/// LMB confirm / RMB-Esc cancel, or hotkey instant-select of mapped move (unscaled time).
/// </summary>
[AddComponentMenu("Locomotion/Wrestling/Card Select Input Driver")]
public sealed class WrestlingCardSelectInputDriver : MonoBehaviour
{
    public WrestlingCardSelectionSession session;
    public AngularWrestlingCardSelectMode selectMode;
    public GambitInputTriggerBuffer inputBuffer;
    public WrestlingMoveInputBindings bindings;
    public int confirmMouseButton = 0;
    public int cancelMouseButton = 1;
    public KeyCode cancelKey = KeyCode.Escape;
    public bool driveWhileEnabled = true;

    void Update()
    {
        if (!driveWhileEnabled || session == null || !session.slowTimeActive)
            return;

        if (selectMode != null)
            selectMode.TryScan(Input.mousePosition, out _, out _);

        var bind = bindings != null ? bindings : session.moveBindings;
        if (bind != null && bind.TryPollPressed(session.mode, out var kind))
        {
            if (!session.TrySelectMoveKind(kind) && inputBuffer != null)
            {
                // Flash reject — leave scanning; optional cancel pulse unused.
            }
            else if (session.selectedCard != null && inputBuffer != null)
            {
                inputBuffer.Raise(GambitInputTriggerKind.MouseClickConfirm, null);
            }
        }

        // Per-card hotkey overrides
        for (int i = 0; i < session.candidates.Count; i++)
        {
            var c = session.candidates[i];
            if (c == null || c.hotkey == KeyCode.None) continue;
            if (Input.GetKeyDown(c.hotkey))
            {
                session.SetHovered(c);
                session.TryConfirmHovered();
                if (inputBuffer != null)
                    inputBuffer.Raise(GambitInputTriggerKind.MouseClickConfirm, null);
                break;
            }
        }

        if (inputBuffer == null) return;

        if (Input.GetMouseButtonDown(confirmMouseButton))
            inputBuffer.Raise(GambitInputTriggerKind.MouseClickConfirm, null);
        else if (Input.GetMouseButtonDown(cancelMouseButton) || Input.GetKeyDown(cancelKey))
            inputBuffer.Raise(GambitInputTriggerKind.MouseClickCancel, null);
    }
}
