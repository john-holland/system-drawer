using UnityEngine;

/// <summary>
/// Dynamic generator for shaders. Output: Shader or material (constant shader type).
/// Plan: what/when to apply, Bounds4, or reference to apply to and parameters.
/// </summary>
[CreateAssetMenu(fileName = "DynamicShaderGenerator", menuName = "Generated/Dynamic Shader Generator", order = 6)]
public class DynamicShaderGenerator : DynamicGeneratorBase
{
    [Header("Script / template")]
    [Tooltip("Shader template (TextAsset) or generation script (MonoScript). Double-click in Project or use Open in inspector to edit in IDE.")]
    public UnityEngine.Object scriptOrTemplate;

    [Header("Shader params")]
    [Tooltip("When/where to apply (e.g. Bounds4 reference, or description).")]
    public string applyWhen = "";
    [Tooltip("Constant shader / material type (e.g. Unlit, Lit).")]
    public string materialType = "Unlit";
    [Tooltip("Optional reference to object to apply shader to.")]
    public UnityEngine.Object applyToReference;

    [Header("LM Studio (shader generation)")]
    [Tooltip("When true, use LM Studio chat completions for shader generation instead of stub/template.")]
    public bool useLmStudioForGenerate;
    [Tooltip("LM Studio model id for completions (e.g. from LmStudioModelService). Leave empty to use first filtered model.")]
    public string lmStudioModelId = "";
    [Tooltip("Optional: maps terms (e.g. icy, wet) to shader properties/slots for prompt and dependency list.")]
    public ShaderGrammarIndex grammarIndex;
    [Tooltip("Optional: common features and map slots so prompt and dependency list align.")]
    public ShaderParameterSchema parameterSchema;

    public override string GeneratorTypeName => "Shader";
}
