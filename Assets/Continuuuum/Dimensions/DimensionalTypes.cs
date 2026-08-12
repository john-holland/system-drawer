using System;
using UnityEngine;

/// <summary>Policy for how an actor behaves across Continuuuum dimension switches.</summary>
public enum DimensionalActorPolicy
{
    KeepAlive,
    AestheticOnly,
    ReplaceActor
}

public enum DimensionalMaterialKind
{
    Auto,
    MeshLit,
    MeshTransparent,
    SkinnedMesh,
    Particle,
    WeatherWater,
    FireLava,
    SdfMax,
    HairPlume,
    SkyCubemap
}

public enum DimensionalShaderFallbackMode
{
    HardCutAtHalf,
    AlphaDither,
    Skip
}

[Serializable]
public struct DimensionalCacheKey : IEquatable<DimensionalCacheKey>
{
    public string game;
    public int dim;
    public string lemmaEntryId;
    public string instanceStableId;

    public DimensionalCacheKey(string game, int dim, string lemmaEntryId, string instanceStableId)
    {
        this.game = game ?? "main";
        this.dim = dim;
        this.lemmaEntryId = lemmaEntryId ?? "";
        this.instanceStableId = instanceStableId ?? "";
    }

    public string Compact => $"{game}|{dim}|{lemmaEntryId}|{instanceStableId}";

    public bool Equals(DimensionalCacheKey other) =>
        dim == other.dim &&
        string.Equals(game, other.game, StringComparison.Ordinal) &&
        string.Equals(lemmaEntryId, other.lemmaEntryId, StringComparison.Ordinal) &&
        string.Equals(instanceStableId, other.instanceStableId, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is DimensionalCacheKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = (game ?? "").GetHashCode();
            h = (h * 397) ^ dim;
            h = (h * 397) ^ (lemmaEntryId ?? "").GetHashCode();
            h = (h * 397) ^ (instanceStableId ?? "").GetHashCode();
            return h;
        }
    }
}

[Serializable]
public sealed class DimensionalPositionalSlot
{
    public Vector3 worldPos;
    public Quaternion worldRot = Quaternion.identity;
    public Vector3 lossyScale = Vector3.one;
    public bool hasVelocity;
    public Vector3 linearVelocity;
    public Vector3 angularVelocity;
}

[Serializable]
public sealed class DimensionalAestheticSlot
{
    public string skinKey;
    public string[] paintKeys;
    public string dimensionalActorPolicy;
}

[Serializable]
public sealed class DimensionalCacheEntry
{
    public DimensionalCacheKey key;
    public DimensionalPositionalSlot positional = new DimensionalPositionalSlot();
    public DimensionalAestheticSlot aesthetic = new DimensionalAestheticSlot();
    public DimensionalActorPolicy policy = DimensionalActorPolicy.KeepAlive;
}

[Serializable]
public sealed class DimensionalShaderGlobalFloat
{
    public string name;
    public float from;
    public float to;
}

/// <summary>Resolved fade job built from DimensionalShaderComponent.</summary>
public sealed class DimensionalShaderFadeJob
{
    public DimensionalShaderComponent source;
    public DimensionalMaterialKind kind;
    public string blendPropertyName = "_DimBlend";
    public float durationSeconds = 0.35f;
    public AnimationCurve blendCurve;
    public Renderer[] renderers;
    public ParticleSystem[] particleSystems;
    public bool useMaterialPropertyBlock = true;
    public bool commitOnComplete = true;
    public DimensionalShaderFallbackMode fallbackMode = DimensionalShaderFallbackMode.HardCutAtHalf;
    public string dissolvePropertyName;
    public DimensionalShaderGlobalFloat[] shaderGlobals;
    public MaterialPropertyBlock block;
    public bool hasBlendProperty;
}
