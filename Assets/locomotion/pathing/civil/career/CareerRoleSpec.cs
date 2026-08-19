using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CareerRoleSpec", menuName = "Locomotion/Civil/Career Role")]
public sealed class CareerRoleSpec : ScriptableObject
{
    public string roleId = "line_worker";
    public string displayName = "Line worker";
    [Tooltip("Distinction only. Same hire/train/fire path as private jobs.")]
    public bool isGovernment;
    public bool requireNoPretraining;
    public bool requiresManagement;
    public bool requiresHiringManager;
    public string[] prerequisiteRoleIds = Array.Empty<string>();
    public EducationalLane lane;
    public string[] certificationIds = Array.Empty<string>();
    public string[] degreeIds = Array.Empty<string>();
    [Range(0f, 1f)] public float[] expected01 = { 0.55f, 0.55f, 0.55f, 0.4f };
    [Range(0f, 1f)] public float[] fireLimit01 = { 0.9f, 0.9f, 0.9f, 0.85f };
    public int peckingOrder = 20;
}
