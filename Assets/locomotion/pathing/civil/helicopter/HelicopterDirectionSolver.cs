using UnityEngine;
using PhysicsCard = GoodSection;

/// <summary>VTOL thrust / yaw / climb from magneto efficacy for TravelAgent fly legs.</summary>
public static class HelicopterDirectionSolver
{
    public struct Result
    {
        public Vector3 thrustDirection;
        public float yawRateDegPerSec;
        public float climbMs;
        public float collective01;
        public float efficacy01;
    }

    public static Result Solve(HelicopterVehicleRagdoll heli, Vector3 from, Vector3 to)
    {
        var r = new Result
        {
            thrustDirection = Vector3.up,
            yawRateDegPerSec = 0f,
            climbMs = 0f,
            collective01 = 0.6f,
            efficacy01 = 1f
        };
        if (heli == null) return r;
        var main = heli.MainMagneto ?? heli.tailRotor;
        if (main != null)
        {
            main.RefreshEfficacyFromLastApplied();
            r.efficacy01 = main.efficacy01;
            r.yawRateDegPerSec = main.EstimateYawRate() * r.efficacy01;
        }

        Vector3 delta = to - from;
        float horiz = new Vector3(delta.x, 0f, delta.z).magnitude;
        float vert = delta.y;
        r.collective01 = Mathf.Clamp01(0.45f + Mathf.Abs(vert) * 0.02f + horiz * 0.001f);
        r.climbMs = (main != null ? main.EstimateClimbMs(r.collective01) : 2f) * Mathf.Sign(vert + 0.001f);
        Vector3 desired = delta.sqrMagnitude > 1e-4f ? delta.normalized : Vector3.up;
        // Bias thrust toward up for VTOL; blend in travel direction.
        r.thrustDirection = Vector3.Slerp(Vector3.up, desired, 0.35f).normalized;
        if (heli.tailRotor != null)
            r.yawRateDegPerSec *= Mathf.Max(0.1f, heli.tailRotor.tailRotorGain);
        return r;
    }

    public static PhysicsCard GenerateHelicopterCard(
        PhysicsCardSolver solver,
        Vector3 from,
        Vector3 to,
        HelicopterVehicleRagdoll heli)
    {
        var solved = Solve(heli, from, to);
        if (solver == null) return null;
        float fuel = 1f;
        var config = solver.flyingCardConfig;
        var card = solver.GenerateFlyingCard(from, to, new RagdollState(), config, false, ref fuel);
        if (card != null)
        {
            card.sectionName = "heli_vtol";
            card.description = "heli|eff=" + solved.efficacy01.ToString("F2")
                               + "|yaw=" + solved.yawRateDegPerSec.ToString("F1")
                               + "|climb=" + solved.climbMs.ToString("F2");
        }
        return card;
    }
}
