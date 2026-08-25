using UnityEngine;

public enum CabinPedalIntent
{
    None = 0,
    Throttle = 1,
    Brake = 2
}

public struct CabinInstrumentHints
{
    public float steerSigned01;
    public float throttle01;
    public float brake01;
    public float shoulderLeanAp;
    public float polarAccel;
    public float residualLean;
    public bool shoulderAgreesWithPolar;
    public bool footOverride;
    public CabinPedalIntent pedal;
}

/// <summary>
/// Maps cabin pose to steer/throttle/brake. Hands → steer.
/// Optional shoulder-shift residual vs polar cabin accel for gas/brake.
/// Direct foot (brake/clutch/accelerator) motion overrides inferred pedals.
/// </summary>
public static class CabinPoseInstrumentSolver
{
    public const string LeftHand = "Human:LeftHand";
    public const string RightHand = "Human:RightHand";
    public const string LeftShoulder = "Human:LeftShoulder";
    public const string RightShoulder = "Human:RightShoulder";
    public const string Hips = "Human:Hips";
    public const string LeftFoot = "Human:LeftFoot";
    public const string RightFoot = "Human:RightFoot";

    public const float FootOverrideThreshold = 0.08f;
    public const float ResidualDeadzone = 0.12f;
    public const float PolarAccelNorm = 4f;

    public static CabinInstrumentHints Evaluate(
        PoseTrack pose,
        float timeMs,
        CabinPolarVelocity polar,
        bool inferShoulderShifts,
        Transform steeringWheel = null)
    {
        var hints = new CabinInstrumentHints { shoulderAgreesWithPolar = true };
        hints.steerSigned01 = SteerFromHands(pose, timeMs, steeringWheel);

        float polarAccel = polar != null ? polar.AccelAt(timeMs) : 0f;
        hints.polarAccel = polarAccel;
        float polarAccel01 = Mathf.Clamp(polarAccel / PolarAccelNorm, -1f, 1f);

        float shoulderAp = ShoulderAp(pose, timeMs);
        hints.shoulderLeanAp = shoulderAp;

        bool feet = TryFootPedal(pose, timeMs, out CabinPedalIntent footIntent, out float footMag);
        hints.footOverride = feet;

        if (feet)
        {
            hints.pedal = footIntent;
            if (footIntent == CabinPedalIntent.Throttle)
                hints.throttle01 = Mathf.Clamp01(footMag);
            else if (footIntent == CabinPedalIntent.Brake)
                hints.brake01 = Mathf.Clamp01(footMag);
            hints.residualLean = shoulderAp - polarAccel01;
            hints.shoulderAgreesWithPolar = Mathf.Abs(hints.residualLean) < 0.45f;
            return hints;
        }

        if (inferShoulderShifts)
        {
            float residual = shoulderAp - polarAccel01;
            hints.residualLean = residual;
            hints.shoulderAgreesWithPolar = Mathf.Abs(residual) < 0.45f || Mathf.Sign(residual) == Mathf.Sign(polarAccel01) || Mathf.Abs(polarAccel01) < 0.08f;
            if (residual > ResidualDeadzone)
            {
                hints.pedal = CabinPedalIntent.Throttle;
                hints.throttle01 = Mathf.Clamp01(residual);
            }
            else if (residual < -ResidualDeadzone)
            {
                hints.pedal = CabinPedalIntent.Brake;
                hints.brake01 = Mathf.Clamp01(-residual);
            }
            return hints;
        }

        if (polar != null)
        {
            var frame = polar.FrameAt(timeMs);
            float speed = frame != null ? frame.speedHint : 0f;
            float accel = polarAccel;
            if (accel < -0.5f || speed < 0.25f)
            {
                hints.pedal = CabinPedalIntent.Brake;
                hints.brake01 = Mathf.Clamp01(accel < 0f ? -accel / PolarAccelNorm : 1f - speed / 8f);
            }
            else if (speed > 0.5f)
            {
                hints.pedal = CabinPedalIntent.Throttle;
                hints.throttle01 = Mathf.Clamp01(speed / 12f);
            }
        }

        hints.residualLean = shoulderAp - polarAccel01;
        return hints;
    }

    public static GoodSection CardFor(CabinInstrumentHints hints)
    {
        if (Mathf.Abs(hints.steerSigned01) >= 0.15f)
            return PhysicalPathingGoodSectionStubs.CreateDriveSteerStub("vehicle_steering");
        if (hints.pedal == CabinPedalIntent.Brake || hints.brake01 >= 0.15f)
            return PhysicalPathingGoodSectionStubs.CreateDriveBrakeStub("vehicle_steering");
        return PhysicalPathingGoodSectionStubs.CreateDriveThrottleStub("vehicle_throttle");
    }

    public static bool TryRoute(
        CabinInstrumentHints hints,
        VehicleInstrumentPhysicsProxy proxy,
        float dt)
    {
        if (proxy == null)
            return false;
        GoodSection card = CardFor(hints);
        return card != null && proxy.RouteCard(card, dt);
    }

    public static float SteerFromHands(PoseTrack pose, float timeMs, Transform steeringWheel)
    {
        if (pose == null)
            return 0f;
        bool hasL = pose.TrySample(LeftHand, timeMs, out Vector3 l, out _);
        bool hasR = pose.TrySample(RightHand, timeMs, out Vector3 r, out _);
        if (!hasL && !hasR)
            return 0f;
        Vector3 hands = hasL && hasR ? (l + r) * 0.5f : (hasL ? l : r);
        Vector3 origin = Vector3.zero;
        if (pose.TrySample(Hips, timeMs, out Vector3 hips, out _))
            origin = hips;
        if (steeringWheel != null)
            origin = steeringWheel.position;
        float lateral = hands.x - origin.x;
        return Mathf.Clamp(lateral / 0.25f, -1f, 1f);
    }

    public static float ShoulderAp(PoseTrack pose, float timeMs)
    {
        if (pose == null)
            return 0f;
        Vector3 sh = Vector3.zero;
        int n = 0;
        if (pose.TrySample(LeftShoulder, timeMs, out Vector3 ls, out _))
        {
            sh += ls;
            n++;
        }
        if (pose.TrySample(RightShoulder, timeMs, out Vector3 rs, out _))
        {
            sh += rs;
            n++;
        }
        if (n == 0)
            return 0f;
        sh /= n;
        Vector3 hips = Vector3.zero;
        pose.TrySample(Hips, timeMs, out hips, out _);
        // Dash camera looking at driver: lean toward windshield/camera is +z in MediaPipe-style samples.
        return Mathf.Clamp(-(sh.z - hips.z) / 0.2f, -1f, 1f);
    }

    public static bool TryFootPedal(PoseTrack pose, float timeMs, out CabinPedalIntent intent, out float mag)
    {
        intent = CabinPedalIntent.None;
        mag = 0f;
        if (pose == null)
            return false;
        Vector3 hips = Vector3.zero;
        pose.TrySample(Hips, timeMs, out hips, out _);
        float ap = 0f;
        int n = 0;
        if (pose.TrySample(RightFoot, timeMs, out Vector3 rf, out _))
        {
            ap += -(rf.z - hips.z);
            n++;
        }
        if (pose.TrySample(LeftFoot, timeMs, out Vector3 lf, out _))
        {
            ap += -(lf.z - hips.z);
            n++;
        }
        if (n == 0)
            return false;
        ap /= n;
        if (Mathf.Abs(ap) < FootOverrideThreshold)
            return false;
        mag = Mathf.Clamp01(Mathf.Abs(ap) / 0.2f);
        intent = ap > 0f ? CabinPedalIntent.Throttle : CabinPedalIntent.Brake;
        return true;
    }
}
