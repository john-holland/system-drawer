#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Planetary.Editor
{
    public static class ImportPlanarScanWizard
    {
        [MenuItem("Window/System Drawer/Planet/Import Planar Scan")]
        public static void ImportScan()
        {
            string path = EditorUtility.OpenFilePanel("Import height scan", "", "png,jpg");
            if (string.IsNullOrEmpty(path))
                return;
            var tex = new Texture2D(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(path));
            var feature = ScriptableObject.CreateInstance<PlanetaryPlanarFeature>();
            feature.heightMap = tex;
            feature.featureId = System.IO.Path.GetFileNameWithoutExtension(path);
            string outPath = EditorUtility.SaveFilePanelInProject("Save planar feature", feature.featureId, "asset", "");
            if (!string.IsNullOrEmpty(outPath))
            {
                AssetDatabase.CreateAsset(feature, outPath);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
