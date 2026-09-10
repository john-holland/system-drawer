using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal enum CreateObjectKind
{
    Asset,
    Prefab,
    Hierarchy
}

internal sealed class CreateObjectEntry
{
    public CreateObjectKind Kind;
    public string Category;
    public string Label;
    public string MenuPath;
    public string SearchHaystack;
    public Type AssetType;
    public string DefaultFileName;
    public string Extension;
    public string PrefabPath;
    public GameObject Prefab;

    internal bool Matches(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;
        string q = filter.Trim();
        return Contains(Category, q)
               || Contains(Label, q)
               || Contains(MenuPath, q)
               || Contains(SearchHaystack, q)
               || Contains(PrefabPath, q)
               || (AssetType != null && Contains(AssetType.Name, q));
    }

    static bool Contains(string hay, string needle) =>
        !string.IsNullOrEmpty(hay) &&
        hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}

/// <summary>First-party CreateAssetMenu types, prefabs, and GameObject factory menus.</summary>
internal static class SystemDrawerCreateObjectCatalog
{
    internal static readonly string[] CreateMenuPrefixes =
    {
        "System Drawer/",
        "Locomotion/",
        "Continuuuum/",
        "Planetary/",
        "Horizon/",
        "Weather/",
        "SDF Max/"
    };

    static readonly string[] PrefabRoots =
    {
        "Assets/SystemDrawer",
        "Assets/locomotion",
        "Assets/Continuuuum",
        "Assets/Planetary",
        "Assets/Weather",
        "Assets/SdfMax"
    };

    static readonly string[] HierarchyPrefixes =
    {
        "GameObject/System Drawer",
        "GameObject/Locomotion"
    };

    static List<CreateObjectEntry> _cached;

    internal static IReadOnlyList<CreateObjectEntry> All()
    {
        if (_cached == null)
            _cached = Collect();
        return _cached;
    }

    internal static void Invalidate() => _cached = null;

    internal static IEnumerable<CreateObjectEntry> Filter(string filter)
    {
        var all = All();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Matches(filter))
                yield return all[i];
        }
    }

    internal static bool IsOwnedCreateMenu(string menuName)
    {
        if (string.IsNullOrEmpty(menuName))
            return false;
        for (int i = 0; i < CreateMenuPrefixes.Length; i++)
        {
            if (menuName.StartsWith(CreateMenuPrefixes[i], StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    internal static bool IsOwnedHierarchyMenu(string menuPath)
    {
        if (string.IsNullOrEmpty(menuPath))
            return false;
        for (int i = 0; i < HierarchyPrefixes.Length; i++)
        {
            if (menuPath.StartsWith(HierarchyPrefixes[i], StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    internal static List<CreateObjectEntry> Collect()
    {
        var list = new List<CreateObjectEntry>(128);
        CollectAssets(list);
        CollectPrefabs(list);
        CollectHierarchy(list);
        list.Sort((a, b) =>
        {
            int c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        });
        return list;
    }

    static void CollectAssets(List<CreateObjectEntry> list)
    {
        var types = TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>();
        for (int i = 0; i < types.Count; i++)
        {
            var t = types[i];
            if (t == null || t.IsAbstract)
                continue;
            var attr = t.GetCustomAttribute<CreateAssetMenuAttribute>();
            if (attr == null || !IsOwnedCreateMenu(attr.menuName))
                continue;
            string fileName = string.IsNullOrEmpty(attr.fileName) ? t.Name : attr.fileName;
            string category = FirstSegment(attr.menuName);
            list.Add(new CreateObjectEntry
            {
                Kind = CreateObjectKind.Asset,
                Category = category,
                Label = LastSegment(attr.menuName),
                MenuPath = attr.menuName,
                SearchHaystack = category + " " + attr.menuName + " " + t.Name + " " + fileName,
                AssetType = t,
                DefaultFileName = fileName,
                Extension = ".asset"
            });
        }
    }

    static void CollectPrefabs(List<CreateObjectEntry> list)
    {
        var roots = ExistingRoots(PrefabRoots);
        if (roots.Length == 0)
            return;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", roots);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path) || IsTestPath(path) || !seen.Add(path))
                continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;
            string category = FolderCategory(path);
            list.Add(new CreateObjectEntry
            {
                Kind = CreateObjectKind.Prefab,
                Category = category,
                Label = prefab.name,
                MenuPath = path,
                SearchHaystack = category + " " + prefab.name + " " + path,
                DefaultFileName = prefab.name,
                Extension = ".prefab",
                PrefabPath = path,
                Prefab = prefab
            });
        }
    }

    static void CollectHierarchy(List<CreateObjectEntry> list)
    {
        var methods = TypeCache.GetMethodsWithAttribute<MenuItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < methods.Count; i++)
        {
            var attrs = methods[i].GetCustomAttributes(typeof(MenuItem), false);
            for (int a = 0; a < attrs.Length; a++)
            {
                var item = (MenuItem)attrs[a];
                if (item.validate || !IsOwnedHierarchyMenu(item.menuItem) || !seen.Add(item.menuItem))
                    continue;
                string label = LastSegment(item.menuItem);
                list.Add(new CreateObjectEntry
                {
                    Kind = CreateObjectKind.Hierarchy,
                    Category = "Hierarchy",
                    Label = label,
                    MenuPath = item.menuItem,
                    SearchHaystack = item.menuItem + " " + label,
                    DefaultFileName = label
                });
            }
        }
    }

    internal static string ResolveSaveFolder(UnityEngine.Object dest)
    {
        if (dest != null)
        {
            string path = AssetDatabase.GetAssetPath(dest);
            string folder = FolderOfAssetPath(path);
            if (!string.IsNullOrEmpty(folder))
                return folder;
        }
        return FallbackFolder();
    }

    internal static string FallbackFolder()
    {
        var objs = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        for (int i = 0; i < objs.Length; i++)
        {
            string folder = FolderOfAssetPath(AssetDatabase.GetAssetPath(objs[i]));
            if (!string.IsNullOrEmpty(folder))
                return folder;
        }
        return "Assets";
    }

    internal static string FolderOfAssetPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path))
            return path;
        string dir = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(dir) ? null : dir.Replace('\\', '/');
    }

    internal static string ResolveCreatedFilePath(string folder, string typedNameOrPath, string extension)
    {
        string ext = NormalizeExtension(extension);
        string typed = (typedNameOrPath ?? "").Trim();
        if (string.IsNullOrEmpty(typed))
            typed = "NewAsset";

        string candidate = ToProjectAssetPath(typed, folder ?? "Assets");
        if (!HasExtension(candidate, ext))
            candidate += ext;
        return AssetDatabase.GenerateUniqueAssetPath(candidate);
    }

    internal static string ToProjectAssetPath(string typed, string folder)
    {
        string normalized = typed.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        string projectRoot = dataPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase)
            ? dataPath.Substring(0, dataPath.Length - "Assets".Length)
            : dataPath + "/";

        if (Path.IsPathRooted(normalized))
        {
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return ("Assets" + normalized.Substring(dataPath.Length)).TrimEnd('/');
            if (normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(projectRoot.Length).TrimStart('/');
            return CombineFolder(folder, Path.GetFileName(normalized));
        }

        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return CombineFolder(folder, normalized);
    }

    internal static void EnsureAssetFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder))
            return;
        assetFolder = assetFolder.Replace('\\', '/');
        var parts = assetFolder.Split('/');
        if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.OrdinalIgnoreCase))
            return;
        string acc = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
                continue;
            string next = acc + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(acc, parts[i]);
            acc = next;
        }
    }

    static string CombineFolder(string folder, string name)
    {
        folder = string.IsNullOrEmpty(folder) ? "Assets" : folder.Replace('\\', '/').TrimEnd('/');
        name = (name ?? "").Replace('\\', '/').TrimStart('/');
        return string.IsNullOrEmpty(name) ? folder : folder + "/" + name;
    }

    static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return ".asset";
        return extension[0] == '.' ? extension : "." + extension;
    }

    static bool HasExtension(string path, string ext)
    {
        return path.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsTestPath(string path)
    {
        return path.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0
               || path.IndexOf("/Editor/Tests/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string[] ExistingRoots(string[] roots)
    {
        var found = new List<string>(roots.Length);
        for (int i = 0; i < roots.Length; i++)
        {
            if (AssetDatabase.IsValidFolder(roots[i]))
                found.Add(roots[i]);
        }
        return found.ToArray();
    }

    static string FirstSegment(string menu)
    {
        if (string.IsNullOrEmpty(menu))
            return "Other";
        int slash = menu.IndexOf('/');
        return slash < 0 ? menu : menu.Substring(0, slash);
    }

    static string LastSegment(string menu)
    {
        if (string.IsNullOrEmpty(menu))
            return "";
        int slash = menu.LastIndexOf('/');
        return slash < 0 ? menu : menu.Substring(slash + 1);
    }

    static string FolderCategory(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        return parts.Length > 1 ? parts[1] : "Prefab";
    }
}
