#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>Creates structured chat SG2D ragdoll hierarchy from the current lexicon field.</summary>
public static class StructuredChatRagdollSetupMenu
{
    const string CreateMenuPath = "Window/System Drawer/Networking/Create Structured Chat Ragdoll";
    const string UpdateMenuPath = "Window/System Drawer/Networking/Update Structured Chat for Lexicon";

    [MenuItem(CreateMenuPath)]
    public static void CreateStructuredChatRagdoll()
    {
        var root = new GameObject("StructuredChatRagdollRoot");
        var ragdoll = root.AddComponent<StructuredChatRagdoll>();
        var gen = root.AddComponent<StructuredChatSpatialGenerator>();
        gen.chatRoot = root.transform;
        gen.generateLayoutAfterUpdate = false;
        gen.lexiconWords = ragdoll.LexiconWords;
        gen.UpdateForLexicon();

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create Structured Chat Ragdoll");
    }

    [MenuItem(UpdateMenuPath)]
    public static void UpdateStructuredChatForLexicon()
    {
        var gen = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<StructuredChatSpatialGenerator>()
            : null;
        if (gen == null)
            gen = Object.FindAnyObjectByType<StructuredChatSpatialGenerator>();
        if (gen == null)
        {
            EditorUtility.DisplayDialog("Structured Chat Spatial Generator",
                "Select a GameObject with StructuredChatSpatialGenerator or create a structured chat ragdoll first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Update Structured Chat Lexicon Nodes");
        gen.UpdateForLexicon();
        EditorUtility.SetDirty(gen);
    }

    [MenuItem(UpdateMenuPath, true)]
    static bool UpdateStructuredChatForLexiconValidate() =>
        Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<StructuredChatSpatialGenerator>() != null
            : Object.FindAnyObjectByType<StructuredChatSpatialGenerator>() != null;
}

#endif
