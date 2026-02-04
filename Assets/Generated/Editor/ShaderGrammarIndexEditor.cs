#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu items to create default ShaderGrammarIndex and ShaderParameterSchema assets.
/// </summary>
public static class ShaderGrammarIndexEditor
{
    private const string GeneratedFolder = "Assets/Generated";
    private const string GrammarPath = "Assets/Generated/ShaderGrammarIndex_Default.asset";
    private const string SchemaPath = "Assets/Generated/ShaderParameterSchema_Default.asset";

    [MenuItem("Assets/Create/Generated/Default Shader Grammar Index", false, 50)]
    public static void CreateDefaultGrammarIndex()
    {
        EnsureGeneratedFolder();
        var existing = AssetDatabase.LoadAssetAtPath<ShaderGrammarIndex>(GrammarPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return;
        }
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "icy", role = "adjective", shaderPropertyOrSlot = "_IceTint" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "wet", role = "adjective", shaderPropertyOrSlot = "_Wetness" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "dry", role = "adjective", shaderPropertyOrSlot = "_Dryness" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "hot", role = "adjective", shaderPropertyOrSlot = "_HeatTint" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "cold", role = "adjective", shaderPropertyOrSlot = "_ColdTint" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "rough", role = "adjective", shaderPropertyOrSlot = "specular" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "smooth", role = "adjective", shaderPropertyOrSlot = "specular" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "bumpy", role = "adjective", shaderPropertyOrSlot = "normal" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "flat", role = "adjective", shaderPropertyOrSlot = "normal" });
        index.entries.Add(new ShaderGrammarIndex.Entry { term = "glow", role = "material", shaderPropertyOrSlot = "emission" });
        AssetDatabase.CreateAsset(index, GrammarPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = index;
    }

    [MenuItem("Assets/Create/Generated/Default Shader Parameter Schema", false, 51)]
    public static void CreateDefaultParameterSchema()
    {
        EnsureGeneratedFolder();
        var existing = AssetDatabase.LoadAssetAtPath<ShaderParameterSchema>(SchemaPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return;
        }
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "albedo", propertyName = "_MainTex", required = true });
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "normal", propertyName = "_BumpMap", required = false });
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "specular", propertyName = "_SpecGlossMap", required = false });
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "displacement", propertyName = "_DisplacementMap", required = false });
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "emission", propertyName = "_EmissionMap", required = false });
        schema.mapSlots.Add(new ShaderParameterSchema.MapSlot { id = "occlusion", propertyName = "_OcclusionMap", required = false });
        AssetDatabase.CreateAsset(schema, SchemaPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = schema;
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
    }
}
#endif
