using System;
using UnityEngine;

/// <summary>Shared elastic slide parameters for UV-masked cloth/skin stretch.</summary>
[Serializable]
public sealed class ClothElasticProperties
{
    public float stiffness = 40f;
    public float damping = 8f;
    public float maxSlipUv = 0.15f;
    [Range(0f, 1f)] public float friction01 = 0.35f;
    public float stretchGain = 1f;
    public float slideGain = 1f;
    [Range(0f, 1f)] public float recovery01 = 0.85f;
}
