using UnityEngine;

/// <summary>Seatback / cabin webtop USC video — open topology → play → close topology.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Webtop USC Video Player")]
public sealed class WebtopUscVideoPlayer : MonoBehaviour
{
    public AirplaneVehicleRagdoll airplane;
    public string openCloseTopologyId = "cabin_webtop";
    public BehaviorTree openCloseBt;
    public string mediaAssetId;
    public bool loop;
    public float durationSec = 30f;
    public bool playing;
    public bool seatbackTarget;
    float _t;
    enum Phase { Idle, Opening, Playing, Closing }
    Phase _phase;

    void Awake()
    {
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>() ?? GetComponentInParent<AirplaneVehicleRagdoll>();
    }

    public void OpenAndPlay(string assetId = null, bool asSeatback = false)
    {
        if (!string.IsNullOrEmpty(assetId))
            mediaAssetId = assetId;
        seatbackTarget = asSeatback;
        _phase = Phase.Opening;
        _t = 0f;
        playing = false;
        airplane?.NotifyNarrative(asSeatback
            ? AirplaneNarrativeActionIds.SeatbackWebtop
            : AirplaneNarrativeActionIds.WebtopOpen);
        SendMessage("OnWebtopTopologyOpen", openCloseTopologyId ?? "", SendMessageOptions.DontRequireReceiver);
    }

    public void Close()
    {
        _phase = Phase.Closing;
        _t = 0f;
        playing = false;
        airplane?.NotifyNarrative(AirplaneNarrativeActionIds.WebtopClose);
        SendMessage("OnWebtopTopologyClose", openCloseTopologyId ?? "", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        if (_phase == Phase.Idle) return;
        _t += Time.deltaTime;
        switch (_phase)
        {
            case Phase.Opening:
                if (_t >= 0.35f)
                {
                    _phase = Phase.Playing;
                    _t = 0f;
                    playing = true;
                    airplane?.NotifyNarrative(AirplaneNarrativeActionIds.WebtopPlay);
                    SendMessage("OnWebtopUscPlay", mediaAssetId ?? "", SendMessageOptions.DontRequireReceiver);
                }
                break;
            case Phase.Playing:
                if (!loop && _t >= Mathf.Max(0.1f, durationSec))
                    Close();
                break;
            case Phase.Closing:
                if (_t >= 0.35f)
                {
                    _phase = Phase.Idle;
                    playing = false;
                }
                break;
        }
    }
}
