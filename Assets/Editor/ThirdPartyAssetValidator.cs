using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Validates that required third-party assets are present in the project.
/// Use this to check if all assets from THIRD_PARTY_ASSETS.md have been imported.
/// </summary>
public class ThirdPartyAssetValidator : EditorWindow
{
    private Dictionary<string, bool> assetStatus = new Dictionary<string, bool>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Validate Third-Party Assets")]
    public static void ShowWindow()
    {
        GetWindow<ThirdPartyAssetValidator>("Third-Party Assets Validator");
    }

    private void OnEnable()
    {
        ValidateAssets();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Third-Party Assets Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool checks if required third-party assets are present.\n" +
            "See THIRD_PARTY_ASSETS.md for import instructions.",
            MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Revalidate Assets", GUILayout.Height(30)))
        {
            ValidateAssets();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Asset Status:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int missingCount = 0;
        foreach (var kvp in assetStatus)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = kvp.Value ? Color.green : Color.red;
            EditorGUILayout.LabelField(kvp.Value ? "✓" : "✗", statusStyle, GUILayout.Width(20));
            
            // Asset name
            EditorGUILayout.LabelField(kvp.Key, kvp.Value ? EditorStyles.label : EditorStyles.boldLabel);
            
            if (!kvp.Value)
            {
                missingCount++;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Missing Assets: {missingCount} / {assetStatus.Count}", EditorStyles.boldLabel);

        if (missingCount > 0)
        {
            EditorGUILayout.HelpBox(
                "Some required assets are missing. Please import them from the Unity Asset Store.\n" +
                "See THIRD_PARTY_ASSETS.md for detailed instructions.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("All required third-party assets are present!", MessageType.Info);
        }
    }

    private void ValidateAssets()
    {
        assetStatus.Clear();

        // Character Models & Animations
        assetStatus["unity-chan!"] = Directory.Exists("Assets/unity-chan!");
        assetStatus["CityPeople_Free"] = Directory.Exists("Assets/CityPeople_Free");
        assetStatus["Kiki"] = Directory.Exists("Assets/Kiki");
        assetStatus["DevilWoman"] = Directory.Exists("Assets/DevilWoman");
        assetStatus["Tiger"] = Directory.Exists("Assets/Tiger");
        assetStatus["UrsaAnimation"] = Directory.Exists("Assets/UrsaAnimation");

        // Environment Assets
        assetStatus["Enviroment"] = Directory.Exists("Assets/Enviroment");
        assetStatus["Mars Landscape 3D"] = Directory.Exists("Assets/Mars Landscape 3D");
        assetStatus["Mountain Terrain rocks and tree"] = Directory.Exists("Assets/Mountain Terrain rocks and tree");
        assetStatus["Terrain Assets"] = Directory.Exists("Assets/Terrain Assets");
        assetStatus["Sci-Fi Styled Modular Pack"] = Directory.Exists("Assets/Sci-Fi Styled Modular Pack");
        assetStatus["WhiteCity"] = Directory.Exists("Assets/WhiteCity");
        assetStatus["Studio Horizon"] = Directory.Exists("Assets/Studio Horizon");

        // Effects & Utilities
        assetStatus["Pixelation"] = Directory.Exists("Assets/Pixelation");
        assetStatus["LargeBitmaskSystem"] = Directory.Exists("Assets/LargeBitmaskSystem");
    }
}
