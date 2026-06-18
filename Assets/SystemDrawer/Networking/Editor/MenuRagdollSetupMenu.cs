#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>Creates main menu MenuRagdoll hierarchy with optional hanging physics.</summary>
public static class MenuRagdollSetupMenu
{
    const string CreateMenuPath = "Window/System Drawer/Networking/Create Main Menu Ragdoll";
    const string UpdateMenuPath = "Window/System Drawer/Networking/Update Main Menu for Network Requirements";

    [MenuItem(CreateMenuPath)]
    public static void CreateMainMenuRagdoll()
    {
        var root = new GameObject("MenuRagdollRoot");
        var menu = root.AddComponent<MenuRagdoll>();
        menu.enableHangingPhysics = true;

        var menuGen = root.AddComponent<MainMenuSpatialGenerator>();
        menuGen.menuRoot = root.transform;
        menuGen.syncNetworkRequirements = true;
        menuGen.generateLayoutAfterUpdate = false;
        menuGen.UpdateMainMenuForNetworkRequirements();

        var wizardGo = new GameObject("MenuRagdollServiceWizard");
        wizardGo.transform.SetParent(root.transform, false);
        var wiz = wizardGo.AddComponent<MenuRagdollServiceWizard>();
        wiz.menuRagdoll = menu;
        wiz.menuGenerator = menuGen;

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create Main Menu Ragdoll");
    }

    [MenuItem(UpdateMenuPath)]
    public static void UpdateMainMenuForNetworkRequirements()
    {
        var gen = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<MainMenuSpatialGenerator>()
            : null;
        if (gen == null)
            gen = Object.FindAnyObjectByType<MainMenuSpatialGenerator>();
        if (gen == null)
        {
            EditorUtility.DisplayDialog("Main Menu Spatial Generator",
                "Select a GameObject with MainMenuSpatialGenerator or create a main menu ragdoll first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Update Main Menu Network Requirements");
        gen.UpdateMainMenuForNetworkRequirements();
        EditorUtility.SetDirty(gen);
    }

    [MenuItem(UpdateMenuPath, true)]
    static bool UpdateMainMenuForNetworkRequirementsValidate() =>
        Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<MainMenuSpatialGenerator>() != null
            : Object.FindAnyObjectByType<MainMenuSpatialGenerator>() != null;
}

#endif
