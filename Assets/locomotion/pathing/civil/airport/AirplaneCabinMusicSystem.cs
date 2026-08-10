using UnityEngine;

/// <summary>Cabin music bus — Chorus ↔ PA ↔ seat aux ↔ pilot telecom; ducks under PA.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airplane Cabin Music System")]
public sealed class AirplaneCabinMusicSystem : MonoBehaviour
{
    public AirplaneVehicleRagdoll airplane;
    public AirplaneCabinMusicSource source = AirplaneCabinMusicSource.Chorus;
    public bool paDucksMusic = true;
    public string paProgramTrackId;
    public string seatAuxTrackId;
    public Component causalityMusicBridge;
    public bool paActive;

    void Awake()
    {
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>() ?? GetComponentInParent<AirplaneVehicleRagdoll>();
        if (causalityMusicBridge == null)
            causalityMusicBridge = GetComponent("CausalityMusicBridge");
    }

    public void SetMusicSource(AirplaneCabinMusicSource next)
    {
        source = next;
        if (airplane != null)
            airplane.defaultMusicSource = next;

        switch (next)
        {
            case AirplaneCabinMusicSource.Silent:
                airplane?.SetCabinMusic(null, false);
                airplane?.NotifyNarrative(AirplaneNarrativeActionIds.CabinMusicSourceChorus);
                break;
            case AirplaneCabinMusicSource.Chorus:
                airplane?.SetCabinMusic(airplane.cabinMusicTrackId, true);
                airplane?.NotifyNarrative(AirplaneNarrativeActionIds.CabinMusicSourceChorus);
                break;
            case AirplaneCabinMusicSource.PaProgram:
                airplane?.SetCabinMusic(paProgramTrackId ?? airplane?.cabinMusicTrackId, true);
                airplane?.NotifyNarrative(AirplaneNarrativeActionIds.CabinMusicSourcePa);
                break;
            case AirplaneCabinMusicSource.SeatAux:
                airplane?.SetCabinMusic(seatAuxTrackId ?? "seat_aux", true);
                airplane?.NotifyNarrative(AirplaneNarrativeActionIds.SeatAuxConnect);
                break;
            case AirplaneCabinMusicSource.PilotTelecom:
                airplane?.SetCabinMusic("pilot_telecom", true);
                airplane?.NotifyNarrative(AirplaneNarrativeActionIds.PaAnnounce);
                break;
        }
    }

    public void AnnouncePa(string utterance, bool duck = true)
    {
        paActive = true;
        if (duck || paDucksMusic)
            SendMessage("OnAirplanePaDuck", utterance ?? "", SendMessageOptions.DontRequireReceiver);
        airplane?.NotifyNarrative(AirplaneNarrativeActionIds.PaAnnounce);
        SendMessage("OnAirplanePaAnnounce", utterance ?? "", SendMessageOptions.DontRequireReceiver);
        paActive = false;
    }
}
