using UnityEngine;

/// <summary>
/// Holds <see cref="RagdollPlayerControllerOptions"/> and the latest <see cref="RagdollPlayerInputState"/> for player BT nodes (per-actor, no statics).
/// </summary>
[DisallowMultipleComponent]
public class RagdollPlayerInputBuffer : MonoBehaviour
{
    public RagdollPlayerControllerOptions options = new RagdollPlayerControllerOptions();

    public RagdollPlayerInputState State { get; private set; }

    public void WriteState(RagdollPlayerInputState s) => State = s;

    public bool ConsumeJumpPressed()
    {
        if (!State.jumpPressedThisFrame)
            return false;
        State = new RagdollPlayerInputState
        {
            horizontal = State.horizontal,
            vertical = State.vertical,
            sprint = State.sprint,
            jumpPressedThisFrame = false,
            uiMode = State.uiMode,
            brake01 = State.brake01,
            selfDriving = State.selfDriving
        };
        return true;
    }

    private void OnEnable()
    {
        if (options != null && options.lockCursorOnEnable && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
