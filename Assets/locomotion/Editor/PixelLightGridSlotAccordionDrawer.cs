#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Shared scrollable accordion list for PixelLight / Helicoptor grid slots.</summary>
public static class PixelLightGridSlotAccordionDrawer
{
    public static void Draw(
        PixelLightMultiSlotCatalog catalog,
        ref Vector2 scroll,
        HelicopterVehicleRagdoll heli,
        System.Action<PixelLightGridSlotEntry> onSelect,
        float maxHeight = 280f,
        AirplaneVehicleRagdoll airplane = null)
    {
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("Assign a PixelLightMultiSlotCatalog.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add grid slot entry"))
        {
            catalog.AddSlot();
            EditorUtility.SetDirty(catalog);
        }
        if (heli != null && GUILayout.Button("Sync from heli children"))
        {
            catalog.SyncSlotsFromHeli(heli);
            EditorUtility.SetDirty(catalog);
        }
        if (airplane != null && GUILayout.Button("Sync from airplane children"))
        {
            catalog.SyncSlotsFromAirplane(airplane);
            EditorUtility.SetDirty(catalog);
        }
        EditorGUILayout.EndHorizontal();

        if (catalog.gridSlots.Count > catalog.maxRecommendedSlots)
            EditorGUILayout.HelpBox(
                $"Slot count {catalog.gridSlots.Count} exceeds recommended max {catalog.maxRecommendedSlots} (Feature Budget: pixel_light).",
                MessageType.Warning);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(maxHeight));
        for (int i = 0; i < catalog.gridSlots.Count; i++)
        {
            var entry = catalog.gridSlots[i];
            if (entry == null) continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string title = string.IsNullOrEmpty(entry.label)
                ? $"Slot {i} ({entry.contents})"
                : $"{entry.label} — {entry.contents}";
            entry.accordionExpanded = EditorGUILayout.Foldout(entry.accordionExpanded, title, true);
            if (entry.accordionExpanded)
            {
                EditorGUI.BeginChangeCheck();
                entry.label = EditorGUILayout.TextField("Label", entry.label);
                entry.slotId = EditorGUILayout.TextField("Slot id", entry.slotId);
                entry.cellX = EditorGUILayout.IntField("Cell X", entry.cellX);
                entry.cellY = EditorGUILayout.IntField("Cell Y", entry.cellY);
                entry.fineOffset = EditorGUILayout.Vector3Field("Fine offset", entry.fineOffset);
                entry.contents = (HelicoptorGridSlotGameObject.SlotContents)EditorGUILayout.EnumPopup(
                    "Contents", entry.contents);
                entry.mount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
                    "Mount", entry.mount, typeof(PixelLightGridMountGameObject), true);
                entry.heliSlot = (HelicoptorGridSlotGameObject)EditorGUILayout.ObjectField(
                    "Heli grid slot", entry.heliSlot, typeof(HelicoptorGridSlotGameObject), true);

                if (entry.heliSlot != null)
                {
                    entry.heliSlot.cellX = entry.cellX;
                    entry.heliSlot.cellY = entry.cellY;
                    entry.heliSlot.fineOffset = entry.fineOffset;
                    EditorUtility.SetDirty(entry.heliSlot);
                }

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(catalog);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select") && onSelect != null)
                    onSelect(entry);
                if (GUILayout.Button("Remove"))
                {
                    // Remove catalog row + destroy scene HelicoptorGridSlot so PixelLight G clears.
                    catalog.RemoveSlotAt(i, heli, airplane, destroySceneObjects: true);
                    EditorUtility.SetDirty(catalog);
                    if (heli != null) EditorUtility.SetDirty(heli);
                    if (airplane != null) EditorUtility.SetDirty(airplane);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    public static void DrawMountList(
        IList<PixelLightGridMountGameObject> mounts,
        PixelLightMultiSlotCatalog catalog,
        ref Vector2 scroll,
        float maxHeight = 280f)
    {
        if (catalog != null && mounts != null)
            catalog.SyncSlotsFromMounts(mounts);
        if (catalog == null)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(maxHeight));
            if (mounts != null)
            {
                for (int i = 0; i < mounts.Count; i++)
                    EditorGUILayout.ObjectField($"Mount {i}", mounts[i], typeof(PixelLightGridMountGameObject), true);
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        Draw(catalog, ref scroll, null, null, maxHeight);
    }
}
#endif
