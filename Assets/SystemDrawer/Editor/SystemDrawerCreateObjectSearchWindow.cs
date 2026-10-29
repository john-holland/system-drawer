using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Search owned Assets/Create menu factories and write the chosen asset.</summary>
public sealed class SystemDrawerCreateObjectSearchWindow : EditorWindow
{
    public const string MenuPath = "Window/System Drawer/Create Object Search";
    const string PrefFilter = "SystemDrawer.CreateObjectSearch.Filter";
    const string PrefFolder = "SystemDrawer.CreateObjectSearch.Folder";

    string _filter;
    UnityEngine.Object _dest;
    string _fileName = "";
    bool _fileNameDirty;
    CreateObjectEntry _selected;
    Vector2 _listScroll;

    [MenuItem(MenuPath, false, 1)]
    public static void ShowWindow()
    {
        var win = GetWindow<SystemDrawerCreateObjectSearchWindow>("Create Object Search");
        win.minSize = new Vector2(440f, 360f);
        win.Show();
    }

    void OnEnable()
    {
        _filter = EditorPrefs.GetString(PrefFilter, "");
        string saved = EditorPrefs.GetString(PrefFolder, "");
        if (AssetDatabase.IsValidFolder(saved))
            _dest = AssetDatabase.LoadAssetAtPath<DefaultAsset>(saved);
        SystemDrawerCreateObjectCatalog.Invalidate();
    }

    void OnFocus()
    {
        SystemDrawerCreateObjectCatalog.Invalidate();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Create Object Search", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assets/Create items from CreateAssetMenu and explicit create menu methods.",
            MessageType.None);
        DrawFilterBar();
        DrawDestination();
        DrawFileNameAndPreview();
        DrawList();
        DrawActions();
    }

    void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search", GUILayout.Width(48));
        string next = GUILayout.TextField(_filter ?? "", EditorStyles.toolbarSearchField);
        if (next != _filter)
        {
            _filter = next;
            EditorPrefs.SetString(PrefFilter, _filter);
            _selected = null;
            _fileNameDirty = false;
        }
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(48)))
        {
            _filter = "";
            EditorPrefs.SetString(PrefFilter, "");
            _selected = null;
            _fileNameDirty = false;
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawDestination()
    {
        EditorGUI.BeginChangeCheck();
        _dest = EditorGUILayout.ObjectField(
            "Save folder / asset", _dest, typeof(UnityEngine.Object), false);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString(PrefFolder, SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest));
    }

    void DrawFileNameAndPreview()
    {
        if (_selected != null && !_fileNameDirty)
            _fileName = _selected.DefaultFileName ?? "";

        using (new EditorGUI.DisabledScope(_selected == null || _selected.AssetType == null))
        {
            EditorGUI.BeginChangeCheck();
            _fileName = EditorGUILayout.TextField("Filename / path", _fileName ?? "");
            if (EditorGUI.EndChangeCheck())
                _fileNameDirty = true;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Created file");
        EditorGUILayout.SelectableLabel(
            PreviewPath(),
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }

    void DrawList()
    {
        var matches = Matching();
        EditorGUILayout.LabelField(
            matches.Count == 1 ? "1 create menu" : matches.Count + " create menus",
            EditorStyles.miniLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MinHeight(180f));
        string lastCat = null;
        for (int i = 0; i < matches.Count; i++)
        {
            var entry = matches[i];
            if (entry.Category != lastCat)
            {
                lastCat = entry.Category;
                EditorGUILayout.LabelField(lastCat, EditorStyles.boldLabel);
            }
            bool selected = _selected != null && _selected.MenuPath == entry.MenuPath;
            if (GUILayout.Button(entry.Label, selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton))
                Select(entry);
        }
        EditorGUILayout.EndScrollView();
    }

    List<CreateObjectEntry> Matching()
    {
        var map = SystemDrawerCreateObjectCatalog.MapCreateContextMenu();
        var matches = new List<CreateObjectEntry>(map.Count);
        foreach (var pair in map)
        {
            if (pair.Value.Matches(_filter))
                matches.Add(pair.Value);
        }
        matches.Sort((a, b) =>
        {
            int c = string.Compare(a.Category, b.Category, System.StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Label, b.Label, System.StringComparison.OrdinalIgnoreCase);
        });
        return matches;
    }

    void DrawActions()
    {
        using (new EditorGUI.DisabledScope(_selected == null))
        {
            string caption = _selected != null && _selected.AssetType == null
                ? "Run create menu"
                : "Create asset";
            if (GUILayout.Button(caption, GUILayout.Height(26)))
                CreateSelected();
        }
        if (_selected != null)
            EditorGUILayout.LabelField("Menu", _selected.MenuPath ?? "");
    }

    void Select(CreateObjectEntry entry)
    {
        _selected = entry;
        _fileNameDirty = false;
        _fileName = entry != null ? entry.DefaultFileName ?? "" : "";
    }

    string PreviewPath()
    {
        if (_selected == null)
            return "";
        if (_selected.AssetType == null)
            return _selected.MenuPath + "  (menu chooses the file)";
        string folder = SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest);
        return SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
            folder, _fileName, _selected.Extension ?? ".asset");
    }

    void CreateSelected()
    {
        if (_selected == null)
            return;
        if (_selected.AssetType == null)
        {
            if (!EditorApplication.ExecuteMenuItem(_selected.MenuPath))
                Debug.LogWarning("[Create Object Search] Menu not found: " + _selected.MenuPath);
            return;
        }

        string folder = SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest);
        string path = SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
            folder, _fileName, _selected.Extension ?? ".asset");
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            SystemDrawerCreateObjectCatalog.EnsureAssetFolder(dir.Replace('\\', '/'));

        var inst = ScriptableObject.CreateInstance(_selected.AssetType);
        inst.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(inst, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = inst;
        EditorGUIUtility.PingObject(inst);
    }
}
