using UnityEngine;

/// <summary>
/// Stub 3D adapter: returns a placeholder prefab path. Replace with real side-loadable tool (e.g. TripoSR) via config path.
/// </summary>
[CreateAssetMenu(fileName = "Stub3DAdapter", menuName = "Generated/Adapters/Stub 3D Adapter", order = 0)]
public class Stub3DAdapter : ScriptableObject, I3DAdapter
{
    public string Generate(string prompt, string modelPath, bool isCharacter, out string error)
    {
        error = null;
        return null;
    }
}
