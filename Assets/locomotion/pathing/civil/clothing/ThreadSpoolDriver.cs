using UnityEngine;

/// <summary>Thread on RopeSystem spool. Jam = diamond leftover, not a second solver.</summary>
[AddComponentMenu("Locomotion/Civil/Thread Spool Driver")]
public sealed class ThreadSpoolDriver : MonoBehaviour
{
    public RopeSystem rope;
    [Range(0f, 1f)] public float threadTension01 = 0.4f;
    [Range(0f, 1f)] public float tensionLimit01 = 0.9f;
    public bool knotted;
    public bool jammed;

    void Awake()
    {
        if (rope == null)
            rope = GetComponent<RopeSystem>();
    }

    public float WindRateMps => rope != null ? rope.WindRateMps : 0f;

    public bool IsJammed => jammed || threadTension01 > tensionLimit01 + 1e-4f;

    public void Pull(float amount01)
    {
        threadTension01 = Mathf.Clamp01(threadTension01 + Mathf.Max(0f, amount01));
        if (IsJammed) jammed = true;
    }

    public void TieKnot()
    {
        knotted = true;
        threadTension01 = Mathf.Clamp01(threadTension01 + 0.15f);
    }

    public void ClearJam()
    {
        jammed = false;
        knotted = false;
        threadTension01 = Mathf.Min(threadTension01, tensionLimit01 * 0.5f);
    }
}
