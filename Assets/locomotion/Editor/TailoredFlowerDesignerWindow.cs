#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class TailoredFlowerDesignerWindow : EditorWindow
{
    TailoredFlowerBinding _binding;
    ClothBoltSpec _bolt;
    Transform _actor;
    Vector2 _scroll;

    [MenuItem("Locomotion/Tailored Flower Designer")]
    public static void Open()
    {
        var w = GetWindow<TailoredFlowerDesignerWindow>("Tailored Flower");
        w.minSize = new Vector2(420, 420);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _binding = (TailoredFlowerBinding)EditorGUILayout.ObjectField(
            "Binding", _binding, typeof(TailoredFlowerBinding), false);
        _bolt = (ClothBoltSpec)EditorGUILayout.ObjectField(
            "Pattern", _bolt, typeof(ClothBoltSpec), false);
        if (_binding != null && _bolt == null)
            _bolt = _binding.bolt;
        _actor = (Transform)EditorGUILayout.ObjectField("Actor", _actor, typeof(Transform), true);

        if (_binding == null)
        {
            if (GUILayout.Button("Create binding"))
                CreateBinding();
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_bolt != null)
            _binding.bolt = _bolt;
        _binding.patternCentroid = EditorGUILayout.Vector3Field("Pattern centroid", _binding.patternCentroid);

        if (GUILayout.Button("Write centroid and rebuild organs"))
        {
            _binding.Rebuild();
            EditorUtility.SetDirty(_binding);
        }

        if (_binding.organs != null)
        {
            for (int i = 0; i < _binding.organs.Count; i++)
            {
                var organ = _binding.organs[i];
                if (organ == null) continue;
                EditorGUILayout.LabelField(organ.role + "  " + organ.sourcePathId);
            }
        }

        if (GUILayout.Button("Deform actor (sealed cloth skin)"))
            DeformActor();
        if (GUILayout.Button("Bake unveil BT"))
            BakeUnveil();

        EditorGUILayout.EndScrollView();
        if (GUI.changed && _binding != null)
            EditorUtility.SetDirty(_binding);
    }

    void CreateBinding()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Tailored Flower", "TailoredFlower", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        var binding = CreateInstance<TailoredFlowerBinding>();
        binding.bolt = _bolt;
        AssetDatabase.CreateAsset(binding, path);
        _binding = binding;
    }

    void DeformActor()
    {
        if (_actor == null) return;
        var skin = _actor.GetComponent<SdfSealedClothSkin>();
        if (skin == null)
            skin = _actor.gameObject.AddComponent<SdfSealedClothSkin>();
        skin.EncloseActor(_actor);
        EditorUtility.SetDirty(skin);
    }

    void BakeUnveil()
    {
        if (_actor == null) return;
        var parent = _actor.Find("TailoredFlowerUnveil");
        if (parent == null)
        {
            var go = new GameObject("TailoredFlowerUnveil");
            go.transform.SetParent(_actor, false);
            parent = go.transform;
        }
        Locomotion.Open.TailoredFlowerUnveilBt.Bake(parent, _actor);
    }
}
#endif
