using UnityEngine;

/// <summary>
/// Snaps card/pose action starts to music beat subdivisions for smoother bar/club motion.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Beat Quantized Action Binder")]
public sealed class BeatQuantizedActionBinder : MonoBehaviour
{
    public bool enabledQuantize = true;
    [Tooltip("Beats per bar for subdivision (4 = quarter notes).")]
    public int beatsPerBar = 4;
    [Tooltip("Subdivision: 1=quarter, 2=eighth, 4=sixteenth.")]
    public int subdivision = 2;
    public float bpm = 120f;
    public float phase01;
    public MonoBehaviour quantizer; // optional PlayerInteractionQuantizer

    float BeatDurationSec => 60f / Mathf.Max(1f, bpm);
    float SubDurationSec => BeatDurationSec / Mathf.Max(1, subdivision);

    public float SecondsUntilNextQuantize()
    {
        if (!enabledQuantize) return 0f;
        float t = Time.time + phase01 * BeatDurationSec;
        float sub = SubDurationSec;
        float into = t % sub;
        return into <= 1e-4f ? 0f : sub - into;
    }

    /// <summary>Delay before starting an action so it lands on the next subdivision.</summary>
    public float QuantizeDelaySec() => SecondsUntilNextQuantize();

    public void ApplyBpmFromSchedule(MusicAmbianceSchedule schedule)
    {
        if (schedule?.Current == null) return;
        switch (schedule.Current.ambiance)
        {
            case MusicAmbianceTag.Club: bpm = 124f; break;
            case MusicAmbianceTag.Bar: bpm = 100f; break;
            case MusicAmbianceTag.Classical: bpm = 72f; break;
            case MusicAmbianceTag.Hushed: bpm = 60f; break;
            default: bpm = 110f; break;
        }
    }
}
