using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Builds the default bread-pan PixelLight prefab hierarchy.</summary>
public static class PixelLightPrefabFactory
{
    public const string DefaultPrefabPath = "Assets/locomotion/pathing/civil/lights/Prefabs/PixelLightDefault.prefab";

    public static GameObject CreateDefaultRuntime(Transform parent = null)
    {
        var go = new GameObject("PixelLight");
        if (parent != null)
            go.transform.SetParent(parent, false);

        var optic = go.AddComponent<PixelLightOptic>();
        optic.EnsureBreadPanMesh();

        // Side metallic + top transparent via runtime materials (URP/Built-in/Unlit fallback).
        // EditMode tests often lack Standard/URP shaders — never throw on null Shader.Find.
        var side = TryCreateMaterial(new Color(0.55f, 0.55f, 0.58f), transparent: false);
        if (side != null)
        {
            side.SetFloat("_Metallic", 0.85f);
            side.SetFloat("_Glossiness", 0.65f);
            side.EnableKeyword("_EMISSION");
            side.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        var top = TryCreateMaterial(new Color(0.7f, 0.85f, 1f, 0.35f), transparent: true);

        optic.sideMaterial = side;
        optic.topMaterial = top;
        if (optic.meshRenderer != null && side != null && top != null)
            optic.meshRenderer.sharedMaterials = new[] { side, top };

        var rig = go.AddComponent<PixelLightRig>();
        rig.optic = optic;
        rig.gridWidth = 8;
        rig.gridHeight = 4;
        rig.stepMs = 80f;
        rig.pattern = PixelLightPatternAsset.CreateChasePreset();
        rig.colorPackage = PixelLightColorPackage.CreateEmergencyRed();
        rig.syncMode = PixelLightSyncMode.Free;
        return go;
    }

    static readonly string[] LitShaderCandidates =
    {
        "Universal Render Pipeline/Lit",
        "Standard",
        "Legacy Shaders/Diffuse",
        "Unlit/Color",
        "Sprites/Default",
        "UI/Default"
    };

    static Shader FindLitShader()
    {
        for (int i = 0; i < LitShaderCandidates.Length; i++)
        {
            var s = Shader.Find(LitShaderCandidates[i]);
            if (s != null) return s;
        }
        return null;
    }

    static Material TryCreateMaterial(Color color, bool transparent)
    {
        var shader = FindLitShader();
        if (shader == null) return null;
        var m = new Material(shader) { color = color };
        if (transparent) SetTransparent(m);
        return m;
    }

    static void SetTransparent(Material m)
    {
        if (m == null) return;
        m.SetFloat("_Mode", 3f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        var c = m.color;
        c.a = 0.35f;
        m.color = c;
    }

#if UNITY_EDITOR
    public static GameObject CreateDefaultPrefabAsset()
    {
        var folder = "Assets/locomotion/pathing/civil/lights/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/locomotion/pathing/civil/lights"))
                AssetDatabase.CreateFolder("Assets/locomotion/pathing/civil", "lights");
            AssetDatabase.CreateFolder("Assets/locomotion/pathing/civil/lights", "Prefabs");
        }
        var go = CreateDefaultRuntime();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, DefaultPrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        return prefab;
    }
#endif
}
