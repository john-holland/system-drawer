#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SystemDrawer.Quest;
using UnityEditor;
using UnityEngine;

public sealed class SystemDrawerCreateObjectTests
{
    [SetUp]
    public void SetUp()
    {
        SystemDrawerCreateObjectCatalog.Invalidate();
    }

    [Test]
    public void Collect_IncludesFeatureBudgetAndLathe()
    {
        var all = SystemDrawerCreateObjectCatalog.Collect();
        Assert.IsTrue(all.Exists(e => e.Kind == CreateObjectKind.Asset && e.AssetType != null && e.AssetType.Name == "FeatureBudgetProfile"));
        Assert.IsTrue(all.Exists(e => e.Kind == CreateObjectKind.Asset && e.AssetType != null && e.AssetType.Name == "LatheSpec"));
        Assert.IsTrue(all.Exists(e => e.Kind == CreateObjectKind.Hierarchy && e.MenuPath == "GameObject/System Drawer/System Drawer Hub"));
    }

    [Test]
    public void Filter_MatchesLabelAndMenu()
    {
        SystemDrawerCreateObjectCatalog.Invalidate();
        var matches = new System.Collections.Generic.List<CreateObjectEntry>(
            SystemDrawerCreateObjectCatalog.Filter("lathe"));
        Assert.IsTrue(matches.Exists(e => e.AssetType != null && e.AssetType.Name == "LatheSpec"));
        var empty = new System.Collections.Generic.List<CreateObjectEntry>(
            SystemDrawerCreateObjectCatalog.Filter("zzz_not_a_create_menu_item"));
        Assert.AreEqual(0, empty.Count);
    }

    [Test]
    public void MapCreateContextMenu_KeysOwnedCreatePaths()
    {
        var map = SystemDrawerCreateObjectCatalog.MapCreateContextMenu();
        Assert.IsTrue(map.ContainsKey("Assets/Create/Locomotion/Civil/Lathe"));
        Assert.IsTrue(map["Assets/Create/Locomotion/Civil/Lathe"].AssetType.Name == "LatheSpec");
        Assert.IsTrue(map.ContainsKey("Assets/Create/System Drawer/Quest Set Asset"));
        Assert.IsTrue(map.ContainsKey("Assets/Create/Generated/Default Shader Grammar Index"));
        Assert.IsFalse(map.ContainsKey("Assets/Create/TextMeshPro/Font Asset"));
        Assert.AreEqual(
            "Assets/Create/Locomotion/Civil/Lathe",
            SystemDrawerCreateObjectCatalog.ToCreateContextPath("Locomotion/Civil/Lathe"));
    }

    [Test]
    public void PrefixAllowlist_RejectsUnrelatedMenus()
    {
        Assert.IsTrue(SystemDrawerCreateObjectCatalog.IsOwnedCreateMenu("System Drawer/Feature Budget Profile"));
        Assert.IsTrue(SystemDrawerCreateObjectCatalog.IsOwnedCreateMenu("Locomotion/Civil/Lathe"));
        Assert.IsFalse(SystemDrawerCreateObjectCatalog.IsOwnedCreateMenu("TextMeshPro/Font Asset"));
        Assert.IsFalse(SystemDrawerCreateObjectCatalog.IsOwnedCreateMenu(""));
        Assert.IsTrue(SystemDrawerCreateObjectCatalog.IsOwnedHierarchyMenu("GameObject/System Drawer/System Drawer Hub"));
        Assert.IsFalse(SystemDrawerCreateObjectCatalog.IsOwnedHierarchyMenu("GameObject/Create Empty"));
    }

    [Test]
    public void ResolveSaveFolder_FromAssetPath()
    {
        Assert.AreEqual("Assets/SystemDrawer",
            SystemDrawerCreateObjectCatalog.FolderOfAssetPath("Assets/SystemDrawer/SystemDrawer.asmdef"));
        Assert.AreEqual("Assets/SystemDrawer",
            SystemDrawerCreateObjectCatalog.FolderOfAssetPath("Assets/SystemDrawer"));
        var asmdef = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
            "Assets/SystemDrawer/SystemDrawer.asmdef");
        Assert.IsNotNull(asmdef);
        Assert.AreEqual("Assets/SystemDrawer", SystemDrawerCreateObjectCatalog.ResolveSaveFolder(asmdef));
    }

    [Test]
    public void CreatedFilePathPreview_BareName_FullPath_UniqueSuffix()
    {
        string bare = SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath("Assets", "Lathe", ".asset");
        Assert.AreEqual(AssetDatabase.GenerateUniqueAssetPath("Assets/Lathe.asset"), bare);

        string full = SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
            "Assets/Other", "Assets/Scenes/Scene2/Lathe.asset", ".asset");
        Assert.AreEqual(AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Scene2/Lathe.asset"), full);

        const string dir = "Assets/SystemDrawer/Editor/Tests";
        const string existing = dir + "/CreateObjectPreviewProbe.asset";
        var probe = ScriptableObject.CreateInstance<QuestSetAsset>();
        SystemDrawerCreateObjectCatalog.EnsureAssetFolder(dir);
        AssetDatabase.CreateAsset(probe, existing);
        try
        {
            string unique = SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
                dir, "CreateObjectPreviewProbe", ".asset");
            Assert.AreNotEqual(existing, unique);
            Assert.IsTrue(unique.StartsWith(dir + "/CreateObjectPreviewProbe"));
            Assert.IsTrue(unique.EndsWith(".asset"));
        }
        finally
        {
            AssetDatabase.DeleteAsset(existing);
        }
    }
}
#endif
