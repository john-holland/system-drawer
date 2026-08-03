#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Building requirements checklist editor (ragdoll-wizard style select-to-assign).</summary>
public sealed class BuildingRequirementsEditorWindow : EditorWindow
{
    BuildingRequirementSpec _spec;
    GameObject _buildingRoot;
    Vector2 _scroll;
    string _status;
    HousingArchitectureSize _archSize = HousingArchitectureSize.GoodSize;

    [MenuItem("Window/System Drawer/Civil/Building Requirements", false, 320)]
    public static void ShowWindow()
    {
        var w = GetWindow<BuildingRequirementsEditorWindow>("Building Requirements");
        w.minSize = new Vector2(480, 420);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Building Requirements", EditorStyles.boldLabel);
        _buildingRoot = (GameObject)EditorGUILayout.ObjectField("Building root", _buildingRoot, typeof(GameObject), true);
        _spec = (BuildingRequirementSpec)EditorGUILayout.ObjectField("Spec", _spec, typeof(BuildingRequirementSpec), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Spec From Type"))
        {
            string typeId = _buildingRoot != null ? _buildingRoot.name : "house";
            var kind = CivilSystemLattice.KindFromBuildingType(typeId);
            _spec = BuildingRequirementSpec.CreateDefault(typeId, kind);
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Building Requirement Spec",
                $"BuildingReq_{typeId}",
                "asset",
                "Choose save path");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(_spec, path);
                AssetDatabase.SaveAssets();
                _status = "Created " + path;
            }
        }
        if (GUILayout.Button("Load Defaults Into Spec") && _spec != null)
        {
            Undo.RecordObject(_spec, "Load Default Slots");
            _spec.slots = BuildingRequirementSpec.DefaultSlotsFor(_spec.buildingTypeId);
            EditorUtility.SetDirty(_spec);
        }
        EditorGUILayout.EndHorizontal();

        if (_spec == null)
        {
            EditorGUILayout.HelpBox("Create or assign a BuildingRequirementSpec.", MessageType.Info);
            return;
        }

        Undo.RecordObject(_spec, "Edit Building Requirements");
        _spec.buildingTypeId = EditorGUILayout.TextField("Building type id", _spec.buildingTypeId);
        _spec.civilKind = (CivilSystemKind)EditorGUILayout.EnumPopup("Civil kind", _spec.civilKind);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_spec.slots == null) _spec.slots = new List<BuildingRequirementSlot>();
        for (int i = 0; i < _spec.slots.Count; i++)
        {
            var slot = _spec.slots[i];
            if (slot == null) continue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(slot.required ? $"● {slot.label}" : $"○ {slot.label}", EditorStyles.boldLabel);
            slot.slotId = EditorGUILayout.TextField("Slot id", slot.slotId);
            slot.label = EditorGUILayout.TextField("Label", slot.label);
            slot.required = EditorGUILayout.Toggle("Required", slot.required);
            slot.reference = (Transform)EditorGUILayout.ObjectField("Transform", slot.reference, typeof(Transform), true);
            slot.referenceObject = (GameObject)EditorGUILayout.ObjectField("GameObject", slot.referenceObject, typeof(GameObject), true);
            if (GUILayout.Button("Assign Selection"))
            {
                if (Selection.activeTransform != null)
                {
                    slot.reference = Selection.activeTransform;
                    slot.referenceObject = Selection.activeGameObject;
                }
            }
            slot.notes = EditorGUILayout.TextField("Notes", slot.notes);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Validate"))
        {
            _status = _spec.Validate(out var err) ? "OK — all required slots filled." : err;
        }
        _archSize = (HousingArchitectureSize)EditorGUILayout.EnumPopup("House architecture lemma", _archSize);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("House Preset Spec"))
        {
            _spec = BuildingRequirementSpec.CreateDefault("house", CivilSystemKind.House);
            _status = "Loaded house requirement defaults (assign & save).";
        }
        if (GUILayout.Button("Ensure HousingBuildingRagdoll") && _buildingRoot != null)
        {
            var housing = _buildingRoot.GetComponent<HousingBuildingRagdoll>();
            if (housing == null)
            {
                // Prefer housing specialization over bare BuildingRagdoll
                var bare = _buildingRoot.GetComponent<BuildingRagdoll>();
                if (bare != null && !(bare is HousingBuildingRagdoll))
                    Undo.DestroyObjectImmediate(bare);
                housing = Undo.AddComponent<HousingBuildingRagdoll>(_buildingRoot);
            }
            if (_buildingRoot.GetComponent<BuildingBeast>() == null)
                Undo.AddComponent<BuildingBeast>(_buildingRoot);
            housing.ApplyArchitectureLemma(_archSize.ToString());
            _status = "HousingBuildingRagdoll + architecture " + _archSize;
        }
        if (GUILayout.Button("Ensure BuildingRagdoll On Root") && _buildingRoot != null)
        {
            if (_buildingRoot.GetComponent<BuildingRagdoll>() == null)
                Undo.AddComponent<BuildingRagdoll>(_buildingRoot);
            if (_buildingRoot.GetComponent<BuildingBeast>() == null)
                Undo.AddComponent<BuildingBeast>(_buildingRoot);
            _status = "BuildingRagdoll + BuildingBeast stub present.";
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        if (_spec != null)
            EditorUtility.SetDirty(_spec);
    }
}
#endif
