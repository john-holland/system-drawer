using UnityEngine;

/// <summary>
/// Config for flying cards: wing aspect ratio, flap power, jet impulse, fuel capacity and costs.
/// Used when generating or scoring wing/jet cards for flying goals.
/// </summary>
[CreateAssetMenu(fileName = "FlyingCardConfig", menuName = "Locomotion/Flying Card Config")]
public class FlyingCardConfig : ScriptableObject
{
    [Header("Wing")]
    [Tooltip("Wing aspect ratio (affects lift/direction contribution when aggregating wing cards).")]
    [Range(0.1f, 20f)]
    public float wingAspectRatio = 4f;

    [Tooltip("Flap power (scales lift from wing cards).")]
    [Range(0f, 2f)]
    public float flapPower = 1f;

    [Header("Jet")]
    [Tooltip("When true, jet mode uses directional impulse instead of wing AR for thrust.")]
    public bool jetModeAvailable = true;

    [Tooltip("Jet impulse strength (directional force magnitude).")]
    [Range(0f, 10f)]
    public float jetImpulseStrength = 2f;

    [Header("Fuel")]
    [Tooltip("Starting fuel capacity (e.g. long for wings, short for jet).")]
    public float fuelCapacity = 100f;

    [Tooltip("Fuel cost per wing card when generating flying sequence.")]
    public float fuelCostPerWingCard = 2f;

    [Tooltip("Fuel cost per jet card when generating flying sequence.")]
    public float fuelCostPerJetCard = 15f;
}
