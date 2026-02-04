/// <summary>
/// Adapter for image → 3D character. Model path from config; input image path; return prefab path.
/// </summary>
public interface IImageTo3DCharacterAdapter
{
    /// <summary>Generate 3D character from image. modelPath from config; imagePath from primitive store. Returns path to prefab asset.</summary>
    string Generate(string imagePath, string modelPath, out string error);
}
