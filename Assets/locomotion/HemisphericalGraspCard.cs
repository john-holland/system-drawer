using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hemispherical grasp card for Lego hand-style enclosure.
/// Represents a grasp action using hemispherical enclosure (55%+).
/// </summary>
[System.Serializable]
public class HemisphericalGraspCard : GoodSection
{
    [Header("Grasp Properties")]
    [Tooltip("Target object to grasp")]
    public GameObject targetObject;

    [Tooltip("Which hand to use")]
    public Hand hand;

    [Tooltip("Center of enclosure grasp")]
    public Vector3 graspPoint;

    [Tooltip("Direction to approach from")]
    public Vector3 approachDirection;

    [Tooltip("Required finger spread angle (degrees)")]
    public float fingerSpread;

    [Tooltip("Required grip strength (Newtons)")]
    public float gripStrength;

    [Tooltip("How well it encloses (0.55+ = good)")]
    [Range(0f, 1f)]
    public float enclosureRatio = 0.55f;

    public HemisphericalGraspCard()
    {
        // Initialize as GoodSection
        sectionName = "hemispherical_grasp";
        description = "Grasp object with hemispherical enclosure";
        limits = new SectionLimits();
        impulseStack = new List<ImpulseAction>();
    }
}

/// <summary>
/// Hand structure for grasping (placeholder - would be more detailed in full implementation).
/// </summary>
[System.Serializable]
public class Hand
{
    [Tooltip("Hand GameObject")]
    public GameObject gameObject;

    [Tooltip("Maximum finger spread angle (degrees)")]
    public float maxFingerSpread = 90f;

    [Tooltip("Maximum grip strength (Newtons)")]
    public float maxGripStrength = 100f;

    [Tooltip("Hemisphere radius for enclosure")]
    public float hemisphereRadius = 0.1f;
}
