using UnityEngine;
using Locomotion.Senses;

/// <summary>How hard an actor brakes after they read a sign. 0 = no slow, 1 = complete stop; &gt;1 stretches hold.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic/Sign Stop Potential")]
public sealed class SignStopPotential : MonoBehaviour
{
    [Tooltip("0 = no slow, 1 = complete stop. Values &gt; 1 stretch hold / cellLength.")]
    public float stopPotential01 = 1f;
    public float approachConeDeg = 70f;
    public float readRangeM = 28f;
    public TAStopSignCard stopCard;
    public TASignCard signCard;
    [Tooltip("-1 auto (Eyes/Visual), 0 unread, 1 read.")]
    public int visualReadOverride = -1;
    public bool lastRead;

    public static float DefaultForKind(TASignKind kind)
    {
        switch (kind)
        {
            case TASignKind.Stop: return 1f;
            case TASignKind.Yield: return 0.65f;
            case TASignKind.SlowChildren: return 0.45f;
            case TASignKind.BlindDrive: return 0.3f;
            default: return 0f;
        }
    }

    public static float DefaultForBrush(CityPixelBrushKind kind)
    {
        if (kind == CityPixelBrushKind.StopSign) return 1f;
        if (kind == CityPixelBrushKind.Detour) return 0.85f;
        if (kind == CityPixelBrushKind.Sign) return 0f;
        return 1f;
    }

    public bool ActorCanRead(TravelAgent agent)
    {
        if (visualReadOverride == 0) return false;
        if (visualReadOverride == 1) return true;
        if (agent == null) return false;
        var eyes = agent.GetComponentInChildren<Eyes>();
        if (eyes == null) return false;
        Vector3 origin = eyes.transform.position;
        Vector3 to = transform.position - origin;
        float dist = to.magnitude;
        if (dist < 0.01f || dist > readRangeM) return false;
        if (Vector3.Angle(eyes.transform.forward, to) > approachConeDeg) return false;
        if (Physics.Raycast(origin, to / dist, out RaycastHit hit, dist))
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        return true;
    }

    public bool TryApply(TravelAgent agent)
    {
        lastRead = ActorCanRead(agent);
        if (!lastRead || agent == null) return false;
        if (!PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(agent))
            return false;

        float sat = Mathf.Clamp01(stopPotential01);
        float surplus = Mathf.Max(0f, stopPotential01 - 1f);
        agent.travelSpeedScale = 1f - sat;
        agent.followTimeSec = Mathf.Max(agent.followTimeSec, 3f * (1f + sat + surplus));
        if (signCard != null)
        {
            signCard.slowRadius *= Mathf.Max(0.01f, sat + 0.01f);
            signCard.avoidCostMultiplier *= Mathf.Max(0.01f, sat + 0.01f);
            signCard.ApplyHintsTo(agent);
        }
        if (sat >= 1f)
        {
            float hold = stopCard != null ? stopCard.holdSec : 2f;
            agent.holdUntilUnscaledTime = Time.unscaledTime + hold * (1f + surplus);
        }
        return true;
    }
}
