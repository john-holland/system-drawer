using System;
using System.Collections.Generic;
using UnityEngine;

public enum CombatDamageType
{
    Bullet,
    Slash,
    Pierce,
    Blunt,
    Electric,
    Heat,
    Laser,
    ContinuousCutter,
    Radiation,
    Explosion,
    Gib
}

public enum DamageHealthMode
{
    PerLimb,
    Overall
}

public enum CombatMaterialKind
{
    Human,
    Robot,
    Structure,
    Vehicle
}

/// <summary>Runtime damage event passed to CombatDamageApplier.</summary>
[Serializable]
public sealed class CombatDamageEvent
{
    public GameObject attacker;
    public GameObject target;
    public CombatDamageType type = CombatDamageType.Blunt;
    public Vector3 worldHit;
    public Vector3 direction = Vector3.forward;
    [Range(0f, 1f)] public float depth01 = 0.35f;
    [Range(0f, 1f)] public float amount01 = 0.25f;
    public bool through;
    public string cutterProfileId;
    public string cutProfileId;
    public CombatMaterialKind materialKind = CombatMaterialKind.Human;
    public DamageHealthMode healthMode = DamageHealthMode.PerLimb;
    public string limbId = "Chest";
    public string smellSignature;
    public bool createWound = true;
    public bool autoSuture;
}

/// <summary>Single clothing/gear mask layer that can absorb damage before actor trauma.</summary>
[Serializable]
public sealed class DamageMaskLayer
{
    public string layerId = "clothing";
    [Range(0f, 1f)] public float absorb01 = 0.4f;
    public List<CombatDamageType> filters = new List<CombatDamageType>();
    [Range(0f, 1f)] public float tearField01;

    public bool Matches(CombatDamageType type)
    {
        if (filters == null || filters.Count == 0) return true;
        for (int i = 0; i < filters.Count; i++)
            if (filters[i] == type) return true;
        return false;
    }
}

/// <summary>Clothing/gear damage masks on an actor or worn item.</summary>
[AddComponentMenu("Locomotion/Combat/Damage Mask")]
public sealed class DamageMask : MonoBehaviour
{
    public List<DamageMaskLayer> layers = new List<DamageMaskLayer>();

    /// <summary>Returns remaining amount01 after clothing absorb; mutates tear fields.</summary>
    public float Absorb(CombatDamageEvent evt)
    {
        if (evt == null) return 0f;
        float remaining = evt.amount01;
        if (layers == null) return remaining;
        for (int i = 0; i < layers.Count && remaining > 1e-4f; i++)
        {
            var layer = layers[i];
            if (layer == null || !layer.Matches(evt.type)) continue;
            float take = remaining * Mathf.Clamp01(layer.absorb01);
            layer.tearField01 = Mathf.Clamp01(layer.tearField01 + take);
            remaining -= take;
        }
        var cloth = GetComponent<ClothingDamageLayer>() ?? GetComponentInChildren<ClothingDamageLayer>();
        if (cloth != null && evt.amount01 - remaining > 1e-4f)
        {
            float absorbed = evt.amount01 - remaining;
            if (evt.type == CombatDamageType.Heat || evt.type == CombatDamageType.Laser ||
                evt.type == CombatDamageType.Radiation || evt.type == CombatDamageType.Explosion)
                cloth.ApplyBurn(absorbed);
            else
                cloth.ApplyTear(absorbed);
        }
        return Mathf.Max(0f, remaining);
    }
}
