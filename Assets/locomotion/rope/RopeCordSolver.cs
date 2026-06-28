using UnityEngine;

/// <summary>Lightweight post-physics correction using overlap index.</summary>
public class RopeCordSolver
{
    readonly RopeConfig _config;
    readonly RopeOverlapIndex _index;
    readonly RopeSegmentRingBuffer _ring;

    public RopeCordSolver(RopeConfig config, RopeOverlapIndex index, RopeSegmentRingBuffer ring)
    {
        _config = config;
        _index = index;
        _ring = ring;
    }

    public void Solve()
    {
        int iterations = Mathf.Clamp(_config.cordSolverIterations, 0, 8);
        float strength = _config.cordCorrectionStrength;

        for (int pass = 0; pass < iterations; pass++)
        {
            foreach (RopeOverlapEntry entry in _index.Entries)
            {
                if (entry.segmentB < 0)
                    continue;
                RopeSegmentBody a = _ring.GetBody(entry.segmentA);
                RopeSegmentBody b = _ring.GetBody(entry.segmentB);
                if (a == null || b == null)
                    continue;

                Vector3 n = entry.normal.sqrMagnitude > 1e-6f ? entry.normal.normalized : Vector3.up;
                float push = entry.penetration * strength * 0.5f;
                MoveBody(a, n * push);
                MoveBody(b, -n * push);

                if ((entry.flags & RopeOverlapFlags.Tangle) != 0)
                {
                    a.Rigidbody.linearVelocity *= 0.92f;
                    b.Rigidbody.linearVelocity *= 0.92f;
                }
            }
        }
    }

    static void MoveBody(RopeSegmentBody body, Vector3 delta)
    {
        if (body.Rigidbody.isKinematic)
            body.transform.position += delta;
        else
            body.Rigidbody.MovePosition(body.transform.position + delta);
    }
}
