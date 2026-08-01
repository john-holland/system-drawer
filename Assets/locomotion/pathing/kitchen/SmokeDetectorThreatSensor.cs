using UnityEngine;

/// <summary>Simple sensor BT hook: battery fault or alarm → ThreatWarden.</summary>
[AddComponentMenu("Locomotion/Kitchen/Smoke Detector Threat Sensor")]
public sealed class SmokeDetectorThreatSensor : MonoBehaviour
{
    public ThreatWarden warden;
    public bool batteryLow;
    public bool alarmActive;

    public void ReportBatteryFault()
    {
        batteryLow = true;
        Raise(ThreatKind.SmokeDetectorBattery);
    }

    public void ReportAlarm()
    {
        alarmActive = true;
        Raise(ThreatKind.SmokeDetectorAlarm);
    }

    public void Clear()
    {
        batteryLow = false;
        alarmActive = false;
        (warden != null ? warden : ThreatWarden.Instance)?.ClearAgency(ThreatAgencyId.Kitchen);
    }

    void Raise(ThreatKind kind)
    {
        var w = warden != null ? warden : ThreatWarden.Instance;
        if (w == null)
        {
            w = FindFirstObjectByType<ThreatWarden>();
            warden = w;
        }
        w?.RaiseThreat(kind, gameObject);
    }
}
