using System;
using UnityEngine;

/// <summary>One formation slot: offset in formation-local space (X = right, Z = forward along route when oriented).</summary>
[Serializable]
public class TravelFormationSlot
{
    public Vector3 localOffset;
}
