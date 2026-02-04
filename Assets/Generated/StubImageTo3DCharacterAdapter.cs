using UnityEngine;

/// <summary>
/// Stub image-to-3D-character adapter. Replace with real side-loadable tool (e.g. DrawingSpinUp) via config path.
/// </summary>
[CreateAssetMenu(fileName = "StubImageTo3DAdapter", menuName = "Generated/Adapters/Stub Image to 3D Character Adapter", order = 1)]
public class StubImageTo3DCharacterAdapter : ScriptableObject, IImageTo3DCharacterAdapter
{
    public string Generate(string imagePath, string modelPath, out string error)
    {
        error = null;
        return null;
    }
}
