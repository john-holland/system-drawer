using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored joint + rope lash configuration for a bay or ambulatory limb section.</summary>
[CreateAssetMenu(fileName = "CargoLashProfile", menuName = "Locomotion/Civil/Rail/Cargo Lash Profile")]
public sealed class CargoLashProfile : ScriptableObject
{
    public string profileId = "lash";
    public List<CargoLashJointSpec> joints = new List<CargoLashJointSpec>();
    public List<CargoLashRopeSpec> ropes = new List<CargoLashRopeSpec>();
    [Tooltip("Soft tip-risk threshold before Nominal reports unstable.")]
    [Range(0f, 1f)] public float tipUnstable01 = 0.65f;
    [Range(0f, 1f)] public float softLashTipBias01 = 0.85f;
}
