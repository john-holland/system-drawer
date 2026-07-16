using UnityEngine;

/// <summary>Seat slot on a computer periphery station: contact + approach + default occupancy.</summary>
[System.Serializable]
public sealed class PeripherySeatSlot
{
    public string slotId = "primary";
    public SitSurfaceContact contact = new SitSurfaceContact();
    public Transform approachWaypoint;
    public SurfaceOccupancyMode defaultMode = SurfaceOccupancyMode.Sit;
    public bool allowStandOn = true;
}

/// <summary>
/// Desk + chair + optional monitor/keyboard/mouse anchors. Consumes seated IK (does not invent a second sit system).
/// </summary>
[AddComponentMenu("Locomotion/Computer Periphery Station")]
public sealed class ComputerPeripheryStation : MonoBehaviour
{
    public PeripherySeatSlot seat = new PeripherySeatSlot();
    public Transform deskAnchor;
    public Transform monitorAnchor;
    public Transform keyboardAnchor;
    public Transform mouseAnchor;
    public Transform chairHost;

    [Tooltip("When true, stand-on at this desk also opens the tool-use gate.")]
    public bool allowToolUseWhileStandOn;

    public PeripheryToolUseGate toolUseGate = new PeripheryToolUseGate();

    void Awake()
    {
        EnsureSeatContact();
    }

    public void EnsureSeatContact()
    {
        if (seat == null)
            seat = new PeripherySeatSlot();
        if (seat.contact == null)
            seat.contact = new SitSurfaceContact();
        if (seat.contact.host == null && chairHost != null)
            seat.contact.host = chairHost;
        if (seat.contact.host == null)
            seat.contact.host = transform;
        seat.contact.EnsureDefaultPolygon();
        if (seat.contact.hostBody == null && seat.contact.host != null)
            seat.contact.hostBody = seat.contact.host.GetComponentInParent<Rigidbody>();
    }

    public Vector3 ApproachPosition =>
        seat != null && seat.approachWaypoint != null
            ? seat.approachWaypoint.position
            : (seat != null && seat.contact != null
                ? seat.contact.WorldPlanePoint - seat.contact.WorldPlaneNormal * 0.1f + Vector3.back * 0.6f
                : transform.position);

    public ComputerKeyboardRuntime EnsureKeyboard(ComputerKeyboardSpec spec = null)
    {
        if (keyboardAnchor == null)
        {
            var go = new GameObject("KeyboardAnchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.7f, 0.35f);
            keyboardAnchor = go.transform;
        }
        var existing = keyboardAnchor.GetComponentInChildren<ComputerKeyboardRuntime>();
        if (existing != null)
            return existing;
        return ComputerKeyboardBuilder.Build(spec ?? new ComputerKeyboardSpec(), keyboardAnchor);
    }

    public void Occupy(GameObject actor, SurfaceOccupancyMode? modeOverride = null)
    {
        EnsureSeatContact();
        EnsureKeyboard();
        if (actor == null) return;
        var runtime = actor.GetComponent<SeatedOccupancyRuntime>();
        if (runtime == null)
            runtime = actor.AddComponent<SeatedOccupancyRuntime>();
        SurfaceOccupancyMode mode = modeOverride ?? seat.defaultMode;
        if (mode == SurfaceOccupancyMode.StandOn && !seat.allowStandOn)
            mode = SurfaceOccupancyMode.Sit;

        if (mode == SurfaceOccupancyMode.StandOn)
            runtime.BeginStandOn(seat.contact);
        else
            runtime.BeginSit(seat.contact);

        bool gateOpen = mode == SurfaceOccupancyMode.Sit || (mode == SurfaceOccupancyMode.StandOn && allowToolUseWhileStandOn);
        toolUseGate.SetOpen(gateOpen);
    }

    public void Vacate(GameObject actor)
    {
        if (actor != null)
        {
            var runtime = actor.GetComponent<SeatedOccupancyRuntime>();
            if (runtime != null)
                runtime.EndOccupancy();
        }
        toolUseGate.SetOpen(false);
    }
}

/// <summary>While occupied, keyboard/mouse / telecom tool cards may run.</summary>
[System.Serializable]
public sealed class PeripheryToolUseGate
{
    public bool isOpen { get; private set; }

    public void SetOpen(bool open) => isOpen = open;

    public bool AllowsToolUse() => isOpen;
}
