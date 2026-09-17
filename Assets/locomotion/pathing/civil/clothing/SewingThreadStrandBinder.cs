using UnityEngine;

/// <summary>Applies sewing/serger RopeConfig onto the existing ThreadSpoolDriver RopeSystem.</summary>
[AddComponentMenu("Locomotion/Civil/Sewing Thread Strand Binder")]
public sealed class SewingThreadStrandBinder : MonoBehaviour
{
    public ThreadSpoolDriver spool;
    public SewingMachineSpec sewing;
    public SergerSpec serger;

    void Awake()
    {
        if (spool == null)
            spool = GetComponent<ThreadSpoolDriver>();
    }

    public RopeConfig BindConnectingStrands()
    {
        if (spool == null)
            spool = GetComponent<ThreadSpoolDriver>();
        float gauge = 0.4f;
        RopeConfig cfg = null;
        if (serger != null)
        {
            gauge = serger.stitchProgram != null ? serger.stitchProgram.defaultGauge01 : 0.45f;
            cfg = serger.ToRopeConfig(gauge);
        }
        else if (sewing != null)
        {
            gauge = sewing.stitchProgram != null ? sewing.stitchProgram.defaultGauge01 : 0.4f;
            cfg = sewing.ToRopeConfig(gauge);
        }
        if (cfg == null)
            return null;
        ApplyToSpool(spool, cfg);
        return cfg;
    }

    public static void ApplyToSpool(ThreadSpoolDriver spool, RopeConfig cfg)
    {
        if (spool == null || cfg == null) return;
        if (spool.rope == null)
            spool.rope = spool.GetComponent<RopeSystem>();
        if (spool.rope == null) return;
        CopyConfig(spool.rope.Config, cfg);
    }

    public static void CopyConfig(RopeConfig dest, RopeConfig src)
    {
        if (dest == null || src == null) return;
        dest.totalLengthM = src.totalLengthM;
        dest.ringBufferSize = src.ringBufferSize;
        dest.segmentLengthM = src.segmentLengthM;
        dest.ropeRadiusM = src.ropeRadiusM;
        dest.mode = src.mode;
        dest.arcBinSizeM = src.arcBinSizeM;
        dest.yieldTensionN = src.yieldTensionN;
        dest.breakTensionN = src.breakTensionN;
        dest.totalStrengthPolicy = src.totalStrengthPolicy;
        dest.maxWindRateMps = src.maxWindRateMps;
        dest.maxUnwindRateMps = src.maxUnwindRateMps;
    }
}
