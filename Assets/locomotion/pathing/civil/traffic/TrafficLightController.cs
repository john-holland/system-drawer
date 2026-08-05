using UnityEngine;

public enum TrafficSignalPhase
{
    AllRed = 0,
    MainGreen = 1,
    MainYellow = 2,
    SideGreen = 3,
    SideYellow = 4
}

/// <summary>
/// Ladder-style controller: timed RR + side-street sensor call → PixelLightRig heads.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Light Controller")]
public sealed class TrafficLightController : MonoBehaviour
{
    public string intersectionId;
    public float mainGreenSec = 12f;
    public float sideGreenSec = 8f;
    public float yellowSec = 3f;
    public float allRedSec = 1.2f;
    public float sideSensorExtendSec = 4f;
    public float minGapSec = 2f;

    public LaneSensorVolume mainSensor;
    public LaneSensorVolume sideSensor;
    public bool pedestrianCall;

    public PixelLightRig mainRed;
    public PixelLightRig mainYellow;
    public PixelLightRig mainGreen;
    public PixelLightRig sideRed;
    public PixelLightRig sideYellow;
    public PixelLightRig sideGreen;

    public TrafficSignalPhase Phase { get; private set; } = TrafficSignalPhase.MainGreen;
    float _phaseT;
    float _sideCallHold;

    public static readonly Color Red = new Color(1f, 0.08f, 0.05f);
    public static readonly Color Yellow = new Color(1f, 0.75f, 0.05f);
    public static readonly Color Green = new Color(0.1f, 0.95f, 0.2f);

    void Awake()
    {
        if (string.IsNullOrEmpty(intersectionId))
            intersectionId = gameObject.name;
        EnsureHeadDefaults();
        ApplyPhaseVisuals();
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (sideSensor != null && sideSensor.occupied)
            _sideCallHold = sideSensorExtendSec;
        else
            _sideCallHold = Mathf.Max(0f, _sideCallHold - dt);

        _phaseT += dt;
        switch (Phase)
        {
            case TrafficSignalPhase.MainGreen:
                if (_phaseT >= mainGreenSec && (_sideCallHold > 0f || pedestrianCall || _phaseT >= mainGreenSec + minGapSec))
                    Enter(TrafficSignalPhase.MainYellow);
                break;
            case TrafficSignalPhase.MainYellow:
                if (_phaseT >= yellowSec) Enter(TrafficSignalPhase.AllRed);
                break;
            case TrafficSignalPhase.AllRed:
                if (_phaseT >= allRedSec)
                    Enter(_sideCallHold > 0f || pedestrianCall ? TrafficSignalPhase.SideGreen : TrafficSignalPhase.MainGreen);
                break;
            case TrafficSignalPhase.SideGreen:
                if (_phaseT >= sideGreenSec) Enter(TrafficSignalPhase.SideYellow);
                break;
            case TrafficSignalPhase.SideYellow:
                if (_phaseT >= yellowSec) Enter(TrafficSignalPhase.AllRed);
                break;
        }
    }

    public void Enter(TrafficSignalPhase phase)
    {
        Phase = phase;
        _phaseT = 0f;
        if (phase == TrafficSignalPhase.SideGreen)
            pedestrianCall = false;
        ApplyPhaseVisuals();
        SendMessage("OnTrafficLightPhase", this, SendMessageOptions.DontRequireReceiver);
    }

    public void SetPhaseFromLemma(string color)
    {
        if (string.IsNullOrEmpty(color)) return;
        switch (color.Trim().ToLowerInvariant())
        {
            case "red": Enter(TrafficSignalPhase.AllRed); break;
            case "yellow":
            case "amber": Enter(TrafficSignalPhase.MainYellow); break;
            case "green": Enter(TrafficSignalPhase.MainGreen); break;
        }
    }

    void ApplyPhaseVisuals()
    {
        bool mainG = Phase == TrafficSignalPhase.MainGreen;
        bool mainY = Phase == TrafficSignalPhase.MainYellow;
        bool sideG = Phase == TrafficSignalPhase.SideGreen;
        bool sideY = Phase == TrafficSignalPhase.SideYellow;
        bool allRed = Phase == TrafficSignalPhase.AllRed;

        SetHead(mainRed, !mainG && !mainY || allRed, Red);
        SetHead(mainYellow, mainY, Yellow);
        SetHead(mainGreen, mainG, Green);
        SetHead(sideRed, !sideG && !sideY || allRed, Red);
        SetHead(sideYellow, sideY, Yellow);
        SetHead(sideGreen, sideG, Green);
    }

    static void SetHead(PixelLightRig rig, bool on, Color c)
    {
        if (rig == null) return;
        rig.syncMode = PixelLightSyncMode.Free;
        rig.SetSolidChannel(c, on);
        rig.SetEnabledEmission(on);
    }

    void EnsureHeadDefaults()
    {
        // Heads may be scene-assigned; decorator creates them if missing.
    }

    public bool MainProceed => Phase == TrafficSignalPhase.MainGreen;
    public bool SideProceed => Phase == TrafficSignalPhase.SideGreen;
}
