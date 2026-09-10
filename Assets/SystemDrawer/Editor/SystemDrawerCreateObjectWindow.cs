using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Search first-party create options and write assets to a picked folder.</summary>
public sealed class SystemDrawerCreateObjectWindow : EditorWindow
{
    const string MenuPath = "Window/System Drawer/Create Object";
    const string PrefFilter = "SystemDrawer.CreateObject.Filter";
    const string PrefFolder = "SystemDrawer.CreateObject.Folder";

    string _filter;
    UnityEngine.Object _dest;
    string _fileName = "";
    bool _fileNameDirty;
    CreateObjectEntry _selected;
    Vector2 _listScroll;
    Vector2 _previewScroll;
    Editor _prefabEditor;
    readonly Dictionary<string, bool> _folds = new Dictionary<string, bool>();

    [MenuItem(MenuPath, false, 1)]
    public static void ShowWindow()
    {
        var win = GetWindow<SystemDrawerCreateObjectWindow>("Create Object");
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

    void OnDisable()
    {
        DestroyPrefabEditor();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("System Drawer Create Object", EditorStyles.boldLabel);
        DrawFilterBar();
        DrawDestination();
        DrawFileNameAndPreview();
        DrawList();
        DrawDetail();
        DrawActions();
    }

    void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Filter", GUILayout.Width(44));
        string next = GUILayout.TextField(_filter ?? "", EditorStyles.toolbarSearchField);
        if (next != _filter)
        {
            _filter = next;
            EditorPrefs.SetString(PrefFilter, _filter);
        }
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(48)))
        {
            _filter = "";
            EditorPrefs.SetString(PrefFilter, "");
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawDestination()
    {
        EditorGUI.BeginChangeCheck();
        _dest = EditorGUILayout.ObjectField(
            "Save folder / asset", _dest, typeof(UnityEngine.Object), false);
        if (EditorGUI.EndChangeCheck())
        {
            string folder = SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest);
            EditorPrefs.SetString(PrefFolder, folder);
        }
    }

    void DrawFileNameAndPreview()
    {
        if (_selected != null && !_fileNameDirty)
            _fileName = _selected.DefaultFileName ?? "";

        EditorGUI.BeginChangeCheck();
        _fileName = EditorGUILayout.TextField("Filename / path", _fileName ?? "");
        if (EditorGUI.EndChangeCheck())
            _fileNameDirty = true;

        string preview = PreviewPath();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Created File Path Preview");
        EditorGUILayout.SelectableLabel(preview, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }

    void DrawList()
    {
        var matches = new List<CreateObjectEntry>();
        foreach (var e in SystemDrawerCreateObjectCatalog.Filter(_filter))
            matches.Add(e);

        EditorGUILayout.LabelField(
            matches.Count == 1 ? "1 matching item" : $"{matches.Count} matching items",
            EditorStyles.miniLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MinHeight(160f));
        bool filtering = !string.IsNullOrWhiteSpace(_filter);
        if (filtering)
        {
            for (int i = 0; i < matches.Count; i++)
                DrawEntryButton(matches[i]);
        }
        else
        {
            string lastCat = null;
            bool fold = true;
            for (int i = 0; i < matches.Count; i++)
            {
                var e = matches[i];
                if (e.Category != lastCat)
                {
                    lastCat = e.Category;
                    if (!_folds.TryGetValue(lastCat, out fold))
                        fold = true;
                    fold = EditorGUILayout.Foldout(fold, lastCat, true);
                    _folds[lastCat] = fold;
                }
                if (!fold)
                    continue;
                EditorGUI.indentLevel++;
                DrawEntryButton(e);
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawEntryButton(CreateObjectEntry entry)
    {
        bool selected = _selected == entry;
        var style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
        if (GUILayout.Button($"{entry.Label}  ({entry.Kind})", style))
            Select(entry);
    }

    void DrawDetail()
    {
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("Select a ScriptableObject, prefab, or hierarchy factory.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Kind", _selected.Kind.ToString());
        EditorGUILayout.LabelField("Type / menu", _selected.MenuPath ?? "");
        if (_selected.AssetType != null)
            EditorGUILayout.LabelField("Type", _selected.AssetType.Name);

        if (_selected.Kind == CreateObjectKind.Prefab && _selected.Prefab != null)
        {
            EnsurePrefabEditor(_selected.Prefab);
            if (_prefabEditor != null)
            {
                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MaxHeight(160f));
                _prefabEditor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
        }
    }

    void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(_selected == null))
        {
            if (_selected != null && _selected.Kind == CreateObjectKind.Asset)
            {
                if (GUILayout.Button("Create asset", GUILayout.Height(26)))
                    CreateAsset(_selected);
            }
            else if (_selected != null && _selected.Kind == CreateObjectKind.Prefab)
            {
                if (GUILayout.Button("Place prefab", GUILayout.Height(26)))
                    PlacePrefab(_selected);
            }
            else if (_selected != null && _selected.Kind == CreateObjectKind.Hierarchy)
            {
                if (GUILayout.Button("Create in hierarchy", GUILayout.Height(26)))
                {
                    if (!EditorApplication.ExecuteMenuItem(_selected.MenuPath))
                        Debug.LogWarning("[Create Object] Menu not found: " + _selected.MenuPath);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void Select(CreateObjectEntry entry)
    {
        _selected = entry;
        _fileNameDirty = false;
        _fileName = entry != null ? entry.DefaultFileName ?? "" : "";
        if (entry == null || entry.Kind != CreateObjectKind.Prefab)
            DestroyPrefabEditor();
        else
            EnsurePrefabEditor(entry.Prefab);
    }

    string PreviewPath()
    {
        if (_selected == null)
            return "";
        if (_selected.Kind == CreateObjectKind.Hierarchy)
            return "(scene object, not saved)";
        if (_selected.Kind == CreateObjectKind.Prefab)
            return (_selected.PrefabPath ?? "") + "  (instance, not a new file)";

        string folder = SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest);
        return SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
            folder, _fileName, _selected.Extension ?? ".asset");
    }

    void CreateAsset(CreateObjectEntry entry)
    {
        if (entry?.AssetType == null)
            return;
        string folder = SystemDrawerCreateObjectCatalog.ResolveSaveFolder(_dest);
        string path = SystemDrawerCreateObjectCatalog.ResolveCreatedFilePath(
            folder, _fileName, entry.Extension ?? ".asset");
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            SystemDrawerCreateObjectCatalog.EnsureAssetFolder(dir.Replace('\\', '/'));

        var inst = ScriptableObject.CreateInstance(entry.AssetType);
        inst.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(inst, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = inst;
        EditorGUIUtility.PingObject(inst);
    }

    void PlacePrefab(CreateObjectEntry entry)
    {
        if (entry?.Prefab == null)
            return;
        var parent = Selection.activeTransform;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
        if (instance == null)
            return;
        Undo.RegisterCreatedObjectUndo(instance, "Place " + entry.Prefab.name);
        if (parent != null)
        {
            Undo.SetTransformParent(instance.transform, parent, "Place " + entry.Prefab.name);
            instance.transform.localPosition = Vector3.zero;
        }
        Selection.activeGameObject = instance;
    }

    void EnsurePrefabEditor(GameObject prefab)
    {
        if (_prefabEditor != null && _prefabEditor.target == prefab)
            return;
        DestroyPrefabEditor();
        if (prefab != null)
            _prefabEditor = Editor.CreateEditor(prefab);
    }

    void DestroyPrefabEditor()
    {
        if (_prefabEditor == null)
            return;
        DestroyImmediate(_prefabEditor);
        _prefabEditor = null;
    }
}
