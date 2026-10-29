#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SpatialVolumes;
using UnityEditor;
using UnityEngine;

public sealed class PlantRootSystemWindow : EditorWindow
{
    PlantOrganismDef _organism;
    Transform _plant;
    List<PlantRootDef> _roots = new List<PlantRootDef>();
    List<PlantRootGroundLayer> _layers = new List<PlantRootGroundLayer>
    {
        new PlantRootGroundLayer { layerId = "topsoil", heightM = 0.3f, materialClass = "loam" },
        new PlantRootGroundLayer { layerId = "subsoil", heightM = 0.7f, materialClass = "silt" }
    };
    float _soilMargin = 0.15f;
    int _particleCount = 24;
    float _scoopAmount = 0.08f;
    PlantRootBakeOutput _output = PlantRootBakeOutput.DiggableVolume;
    Vector2 _scroll;

    [MenuItem("Locomotion/Plant Root System")]
    public static void Open()
    {
        var w = GetWindow<PlantRootSystemWindow>("Plant Roots");
        w.minSize = new Vector2(420, 480);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        var next = (PlantOrganismDef)EditorGUILayout.ObjectField(
            "Organism", _organism, typeof(PlantOrganismDef), false);
        if (next != _organism)
        {
            _organism = next;
            CopyRootsFromOrganism();
        }
        _plant = (Transform)EditorGUILayout.ObjectField("Plant", _plant, typeof(Transform), true);

        EditorGUILayout.LabelField("Root tips", EditorStyles.boldLabel);
        if (_roots == null)
            _roots = new List<PlantRootDef>();
        int remove = -1;
        for (int i = 0; i < _roots.Count; i++)
        {
            if (_roots[i] == null)
                _roots[i] = new PlantRootDef();
            EditorGUILayout.BeginHorizontal();
            _roots[i].localTip = EditorGUILayout.Vector3Field("Tip " + i, _roots[i].localTip);
            if (GUILayout.Button("X", GUILayout.Width(22f)))
                remove = i;
            EditorGUILayout.EndHorizontal();
            _roots[i].radius = EditorGUILayout.FloatField("Radius", _roots[i].radius);
        }
        if (remove >= 0)
            _roots.RemoveAt(remove);
        if (GUILayout.Button("Add root"))
            _roots.Add(new PlantRootDef { localTip = new Vector3(0f, -0.4f, 0f), radius = 0.04f });
        if (_organism != null && GUILayout.Button("Write tips to organism"))
            WriteTips();

        _soilMargin = EditorGUILayout.FloatField("Soil margin", _soilMargin);
        _particleCount = EditorGUILayout.IntField("SPH particles", _particleCount);
        _scoopAmount = EditorGUILayout.FloatField("Scoop amount", _scoopAmount);
        _output = (PlantRootBakeOutput)EditorGUILayout.EnumPopup("Save", _output);

        if (_output == PlantRootBakeOutput.DiggableVolume)
            DrawGroundLayers();

        if (GUILayout.Button("Bake roots"))
            Bake();

        EditorGUILayout.EndScrollView();
    }

    void DrawGroundLayers()
    {
        EditorGUILayout.LabelField("Diggable volume", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each ground layer is a soil height. The stack starts at the crown and goes down. The top layer's material is the DiggableVolume class.",
            MessageType.None);
        if (_layers == null)
            _layers = new List<PlantRootGroundLayer>();
        int remove = -1;
        for (int i = 0; i < _layers.Count; i++)
        {
            if (_layers[i] == null)
                _layers[i] = new PlantRootGroundLayer();
            EditorGUILayout.BeginHorizontal();
            _layers[i].layerId = EditorGUILayout.TextField(_layers[i].layerId);
            _layers[i].heightM = EditorGUILayout.FloatField(_layers[i].heightM);
            if (GUILayout.Button("X", GUILayout.Width(22f)))
                remove = i;
            EditorGUILayout.EndHorizontal();
            _layers[i].materialClass = EditorGUILayout.TextField("Material", _layers[i].materialClass);
        }
        if (remove >= 0)
            _layers.RemoveAt(remove);
        if (GUILayout.Button("Add ground layer"))
            _layers.Add(new PlantRootGroundLayer());
    }

    void CopyRootsFromOrganism()
    {
        _roots = new List<PlantRootDef>();
        if (_organism?.roots == null) return;
        for (int i = 0; i < _organism.roots.Count; i++)
        {
            var src = _organism.roots[i];
            if (src == null) continue;
            _roots.Add(new PlantRootDef { localTip = src.localTip, radius = src.radius });
        }
    }

    void WriteTips()
    {
        if (_organism == null) return;
        _organism.roots = new List<PlantRootDef>();
        for (int i = 0; i < _roots.Count; i++)
        {
            var src = _roots[i];
            if (src == null) continue;
            _organism.roots.Add(new PlantRootDef { localTip = src.localTip, radius = src.radius });
        }
        EditorUtility.SetDirty(_organism);
    }

    void Bake()
    {
        if (_roots == null || _roots.Count == 0)
        {
            EditorUtility.DisplayDialog("Plant roots", "Add at least one root tip.", "OK");
            return;
        }
        string meshPath = EditorUtility.SaveFilePanelInProject(
            "Save root mesh", "PlantRoots", "asset",
            "Soil and root expression assets are saved beside this mesh when keeping a DiggableVolume.");
        if (string.IsNullOrEmpty(meshPath)) return;

        if (_organism != null)
            WriteTips();

        var host = new GameObject(_plant != null ? _plant.name + "_Roots" : "PlantRoots");
        if (_plant != null)
            host.transform.SetParent(_plant, false);

        var layers = _output == PlantRootBakeOutput.DiggableVolume ? _layers : null;
        var rootExpr = PlantRootSystemBake.BuildRootExpression(_roots);
        var volume = host.AddComponent<DiggableVolume>();
        PlantRootSystemBake.ConfigureSoilVolume(volume, _roots, _soilMargin, layers, _particleCount, _scoopAmount);
        var skin = PlantRootSystemBake.BuildSkinnedHost(host, rootExpr, _roots);
        var renderer = host.GetComponent<SkinnedMeshRenderer>();
        if (renderer != null && renderer.sharedMesh != null)
        {
            var meshCopy = Object.Instantiate(renderer.sharedMesh);
            meshCopy.name = Path.GetFileNameWithoutExtension(meshPath);
            ReplaceAsset(meshCopy, meshPath);
            renderer.sharedMesh = meshCopy;
        }
        else if (_output == PlantRootBakeOutput.MeshOnly)
        {
            EditorUtility.DisplayDialog(
                "Plant roots",
                "The root surface mesh was empty, so nothing was saved.",
                "OK");
        }

        if (_output == PlantRootBakeOutput.MeshOnly)
        {
            var soil = volume.sdf;
            var provider = host.GetComponent<SpatialVolumeProvider>();
            Object.DestroyImmediate(volume);
            if (provider != null)
                Object.DestroyImmediate(provider);
            if (skin != null)
                Object.DestroyImmediate(skin);
            if (soil != null)
                Object.DestroyImmediate(soil);
            if (rootExpr != null)
                Object.DestroyImmediate(rootExpr);
        }
        else
        {
            string dir = Path.GetDirectoryName(meshPath)?.Replace('\\', '/') ?? "Assets";
            string stem = Path.GetFileNameWithoutExtension(meshPath);
            ReplaceAsset(rootExpr, dir + "/" + stem + "_Expression.asset");
            if (volume.sdf != null)
                ReplaceAsset(volume.sdf, dir + "/" + stem + "_Soil.asset");
        }
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = host;
    }

    static void ReplaceAsset(Object asset, string path)
    {
        if (asset == null || string.IsNullOrEmpty(path)) return;
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }
}
#endif
