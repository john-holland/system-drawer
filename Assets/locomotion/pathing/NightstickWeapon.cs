using UnityEngine;

/// <summary>Nightstick grip + hit-tool binding for stairwell fish.</summary>
public sealed class NightstickWeapon : MonoBehaviour
{
    public Transform gripPoint;
    public GoodSection hitToolSection;
    public bool dualWield;
    public NightstickWeapon pairedStick;

    public void Claim(Transform hand, bool asDual = false)
    {
        if (hand == null) return;
        transform.SetParent(hand, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        dualWield = asDual;
    }

    public void PairWith(NightstickWeapon other)
    {
        pairedStick = other;
        if (other != null)
        {
            other.pairedStick = this;
            dualWield = true;
            other.dualWield = true;
        }
    }
}
