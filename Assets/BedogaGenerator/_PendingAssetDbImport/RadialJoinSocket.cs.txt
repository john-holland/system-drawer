using UnityEngine;

/// <summary>Records Glue / Hardware join ids after radial place. No Open.Runtime reference.</summary>
public sealed class RadialJoinSocket : MonoBehaviour
{
    public RadialJoinKind joinKind = RadialJoinKind.Natural;
    public string jointId = "";

    public static RadialJoinSocket Apply(GameObject instance, RadialBuildSpec spec)
    {
        if (instance == null || spec == null)
            return null;
        if (spec.joinKind != RadialJoinKind.Glue && spec.joinKind != RadialJoinKind.Hardware)
            return null;
        var sock = instance.GetComponent<RadialJoinSocket>();
        if (sock == null)
            sock = instance.AddComponent<RadialJoinSocket>();
        sock.joinKind = spec.joinKind;
        sock.jointId = spec.jointId ?? "";
        if (spec.joinKind == RadialJoinKind.Glue)
        {
            var selfRb = instance.GetComponent<Rigidbody>();
            var hostRb = instance.GetComponentInParent<Rigidbody>();
            if (hostRb != null && hostRb.gameObject != instance && selfRb != null)
            {
                var fj = instance.GetComponent<FixedJoint>();
                if (fj == null)
                    fj = instance.AddComponent<FixedJoint>();
                fj.connectedBody = hostRb;
            }
        }
        return sock;
    }
}
