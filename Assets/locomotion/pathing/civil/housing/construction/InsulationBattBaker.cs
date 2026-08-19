using UnityEngine;
using Locomotion.Rendering;

/// <summary>Hair-as-fiberglass batt bake per stud bay: pleated lattice + dither edge.</summary>
public static class InsulationBattBaker
{
    public struct Result
    {
        public Texture2D radial;
        public Texture2D diffuse;
        public Texture2D specular;
        public int pleatLayers;
        public bool inactive;
    }

    public static Result BakeSlot(HairPlumeConfig config, int pleatLayers, bool inactiveUntilFrame)
    {
        config ??= ScriptableObject.CreateInstance<HairPlumeConfig>();
        config.plumeTipHold = 0.15f;
        config.maxStrandLengthM = Mathf.Max(0.05f, config.maxStrandLengthM);
        int layers = Mathf.Clamp(pleatLayers, 2, 4);
        var lattice = HairLatticeWaterfallBaker.Bake(config);
        HairFiberMaterialBaker.Bake(config, new Color(0.92f, 0.88f, 0.55f), new Color(0.85f, 0.8f, 0.45f), out var diff, out var spec);
        return new Result
        {
            radial = lattice.texture,
            diffuse = diff,
            specular = spec,
            pleatLayers = layers,
            inactive = inactiveUntilFrame
        };
    }

    public static void ApplyDither(TransparentOccluder occluder)
    {
        if (occluder == null) return;
        occluder.ditherIntensity = 0.65f;
        occluder.fadeZoneSize = 0.08f;
    }
}
