using UnityEngine;

/// <summary>Default camera / control perspective for <see cref="PlayerVocabBuiltIn"/>.</summary>
public enum RagdollPlayerPerspective
{
    FirstPerson,
    ThirdPerson
}

/// <summary>
/// Serializable options shared by first- and third-person ragdoll player behavior-tree nodes.
/// </summary>
[System.Serializable]
public class RagdollPlayerControllerOptions
{
    public bool enableMovement = true;
    public bool enableMouseLook = true;
    public bool enableAnimations = true;
    public bool invertY;
    [Min(0.01f)] public float mouseSensitivity = 2f;
    [Min(0.01f)] public float verticalLookLimit = 80f;
    [Min(0.01f)] public float moveSpeed = 5f;
    [Min(1f)] public float sprintMultiplier = 2f;
    [Tooltip("When true, holding this key unlocks the cursor and pauses look + movement (same idea as Misc FirstPersonController).")]
    public bool altHeldEnablesUIMode = true;
    public KeyCode uiModeHoldKey = KeyCode.LeftAlt;
    public bool lockCursorOnEnable = true;
    [Min(0.1f)] public float orbitDistance = 4f;
    [Min(0.01f)] public float orbitMouseSensitivity = 2f;
    [Range(-80f, 0f)] public float minOrbitPitch = -40f;
    [Range(0f, 85f)] public float maxOrbitPitch = 70f;
    [Min(0.05f)] public float groundProbeDistance = 1.2f;
    public LayerMask groundLayers = ~0;
    [Min(0.01f)] public float jumpImpulseStrength = 4f;
    [Tooltip("Dedicated brake — not Vertical reverse.")]
    public KeyCode brakeKey = KeyCode.LeftControl;
    public bool selfDrivingEnabled;
    public KeyCode selfDrivingToggleKey = KeyCode.K;
    [Tooltip("When true, player-driven vehicles skip TravelAgent speed/hold unless braking or self-driving.")]
    public bool overrideTravelAgentSlow = true;
}

/// <summary>Per-frame input sampled by <see cref="ReadRagdollPlayerMovementInputNode"/> for locomotion / animation nodes.</summary>
public struct RagdollPlayerInputState
{
    public float horizontal;
    public float vertical;
    public bool sprint;
    public bool jumpPressedThisFrame;
    public bool uiMode;
    [Range(0f, 1f)] public float brake01;
    public bool selfDriving;
}
