/// <summary>
/// Adapter for prompt → 3D object or character. Model path from config; run locally; return mesh or prefab path.
/// </summary>
public interface I3DAdapter
{
    /// <summary>Generate 3D from prompt. modelPath from GenerationAdapterConfig; isCharacter when true requests character rig. Returns path to prefab or mesh asset.</summary>
    string Generate(string prompt, string modelPath, bool isCharacter, out string error);
}
