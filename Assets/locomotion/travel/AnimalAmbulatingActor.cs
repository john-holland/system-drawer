using UnityEngine;

/// <summary>Non-human ambulating actor (animals). Higher ambulation-cache likelihood than humans.</summary>
[AddComponentMenu("Locomotion/Travel/Animal Ambulating Actor")]
public sealed class AnimalAmbulatingActor : BaseAmbulatingActor
{
    [Tooltip("Footprint radius used by crowd / cache likelihood.")]
    public float footprintRadiusM = 0.45f;
}
