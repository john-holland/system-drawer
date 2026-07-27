using UnityEngine;

/// <summary>
/// Urethra nozzle pee stream; bladder drain; random 3D curve over first 90° (0 disables).
/// </summary>
[AddComponentMenu("Locomotion/Bathroom/Pee Stream Director")]
public sealed class PeeStreamDirector : MonoBehaviour
{
    public GroinAnatomyRuntime groin;
    public BowelBladderRuntime bowelBladder;
    [Tooltip("Optional RollingSphereFloodSimulator with EmitFromFlow(float).")]
    public MonoBehaviour flood;
    [Tooltip("Optional DrinkStreamRenderer.")]
    public MonoBehaviour streamRenderer;
    [Tooltip("Random 3D curve adjustment over first 90 degrees of stream. 0 = off.")]
    public float peeDirectionJitterDegrees = 12f;
    public float drainRate01PerSecond = 0.35f;
    public bool releasing;

    Vector3 _baseForward;
    Vector3 _jitterAxis;
    float _streamAngleDeg;
    System.Random _rng;

    public void BeginRelease(int seed = 0)
    {
        releasing = true;
        _streamAngleDeg = 0f;
        _rng = seed != 0 ? new System.Random(seed) : new System.Random();
        _baseForward = groin != null ? groin.TipForward : transform.forward;
        _jitterAxis = Vector3.Cross(_baseForward, Vector3.up);
        if (_jitterAxis.sqrMagnitude < 1e-6f)
            _jitterAxis = Vector3.Cross(_baseForward, Vector3.right);
        _jitterAxis.Normalize();
        float yaw = (float)(_rng.NextDouble() * 2.0 - 1.0);
        float pitch = (float)(_rng.NextDouble() * 2.0 - 1.0);
        _jitterAxis = (Quaternion.AngleAxis(yaw * 40f, _baseForward) * _jitterAxis +
                       Quaternion.AngleAxis(pitch * 40f, Vector3.up) * _baseForward).normalized;
    }

    public void EndRelease() => releasing = false;

    void FixedUpdate()
    {
        if (!releasing) return;
        var bladder = bowelBladder != null
            ? bowelBladder
            : BowelBladderRuntime.FindOrCreate(gameObject);
        if (bladder.bladderFill01 <= 1e-4f)
        {
            EndRelease();
            return;
        }

        bladder.bladderFill01 = Mathf.Max(0f, bladder.bladderFill01 - drainRate01PerSecond * Time.fixedDeltaTime);
        var sheet = GetComponent<LifeSystemsSheet>() ?? GetComponentInParent<LifeSystemsSheet>();
        sheet?.Set01(LifeSystemsChannelCatalog.BladderFill, bladder.bladderFill01);

        float step = 90f * drainRate01PerSecond * Time.fixedDeltaTime;
        _streamAngleDeg = Mathf.Min(90f, _streamAngleDeg + step);

        Vector3 dir = _baseForward;
        if (peeDirectionJitterDegrees > 1e-3f && _streamAngleDeg < 90f)
        {
            float w = 1f - _streamAngleDeg / 90f;
            dir = Quaternion.AngleAxis(peeDirectionJitterDegrees * w, _jitterAxis) * _baseForward;
        }

        if (groin != null && groin.urethraTip != null)
            groin.urethraTip.rotation = Quaternion.LookRotation(dir, Vector3.up);

        float litersPerSec = groin != null ? groin.maxThroughputLitersPerSecond : 0.02f;
        if (flood != null)
        {
            var m = flood.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) });
            m?.Invoke(flood, new object[] { litersPerSec });
        }
        _ = streamRenderer;
    }
}
