#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Authoring window for <see cref="NarrativePromptAsset"/> with <c>{P:...}</c> span editing.
    /// </summary>
    public class PromptEditorWindow : EditorWindow
    {
        const string PrefsRegistryAssetPath = "PromptEditor.PromptRegistryAssetPath";
        const string PrefsLastFolder = "PromptEditor.LastAssetFolder";

        PromptRegistry _registry;
        Vector2 _listScroll;
        Vector2 _treeScroll;
        Vector2 _rightScroll;

        string _workingOriginal = "";
        string _savedSnapshot = "";
        NarrativePromptAsset _activeAsset;

        readonly List<NarrativePromptAsset> _promptList = new List<NarrativePromptAsset>();
        readonly List<PromptSegment> _segments = new List<PromptSegment>();

        int _selectedSegmentIndex = -1;

        string _editName = "";
        readonly List<string> _editKeys = new List<string>();
        readonly List<string> _editVals = new List<string>();

        [MenuItem("Window/System Drawer/Narrative/Prompt Editor", false, 204)]
        public static void Open()
        {
            var w = GetWindow<PromptEditorWindow>("Prompt Editor");
            w.minSize = new Vector2(720, 520);
            w.Show();
        }

        void OnEnable()
        {
            LoadRegistryFromPrefs();
            RefreshPromptList();
            if (_activeAsset == null && _promptList.Count > 0)
                LoadAssetInternal(_promptList[0]);
            RebuildSegments();
        }

        void LoadRegistryFromPrefs()
        {
            string path = EditorPrefs.GetString(PrefsRegistryAssetPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                var reg = AssetDatabase.LoadAssetAtPath<PromptRegistry>(path);
                if (reg != null)
                    _registry = reg;
            }

            if (_registry == null)
            {
                var fromResources = Resources.Load<PromptRegistry>("PromptRegistry");
                if (fromResources != null)
                    _registry = fromResources;
            }

            if (_registry == null)
            {
                // Fallback: any PromptRegistry asset in project (doesn't require Resources/ path).
                string[] guids = AssetDatabase.FindAssets("t:PromptRegistry");
                if (guids != null && guids.Length > 0)
                {
                    string anyPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var anyRegistry = AssetDatabase.LoadAssetAtPath<PromptRegistry>(anyPath);
                    if (anyRegistry != null)
                        _registry = anyRegistry;
                }
            }
        }

        void SaveRegistryToPrefs()
        {
            if (_registry == null)
            {
                EditorPrefs.DeleteKey(PrefsRegistryAssetPath);
                return;
            }

            string path = AssetDatabase.GetAssetPath(_registry);
            if (!string.IsNullOrEmpty(path))
                EditorPrefs.SetString(PrefsRegistryAssetPath, path);
        }

        void OnTreeSelection(int segmentIndex)
        {
            _selectedSegmentIndex = segmentIndex;
            LoadEditFieldsFromSelectedSegment();
        }

        void LoadEditFieldsFromSelectedSegment()
        {
            _editName = "";
            _editKeys.Clear();
            _editVals.Clear();
            if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _segments.Count)
                return;

            PromptSegment s = _segments[_selectedSegmentIndex];
            if (!s.isPlaceholder)
                return;

            _editName = s.placeholderName ?? "";
            foreach (KeyValuePair<string, string> kv in s.placeholderParams)
            {
                _editKeys.Add(kv.Key);
                _editVals.Add(kv.Value ?? "");
            }
        }

        bool IsDirty => _activeAsset != null && !string.Equals(_workingOriginal ?? "", _savedSnapshot ?? "");

        void RefreshPromptList()
        {
            _promptList.Clear();
            if (_registry != null && _registry.prompts != null)
            {
                foreach (NarrativePromptAsset p in _registry.prompts)
                {
                    if (p != null)
                        _promptList.Add(p);
                }
            }
        }

        void RebuildSegments()
        {
            _segments.Clear();
            _segments.AddRange(PromptSpanParser.Parse(_workingOriginal ?? ""));
            Repaint();
        }

        void LoadAssetInternal(NarrativePromptAsset asset)
        {
            _activeAsset = asset;
            _workingOriginal = asset != null ? (asset.originalText ?? "") : "";
            _savedSnapshot = _workingOriginal;
            _selectedSegmentIndex = -1;
            RebuildSegments();
        }

        bool ConfirmNavigateAway()
        {
            if (!IsDirty)
                return true;

            int r = EditorUtility.DisplayDialogComplex(
                "Save prompt?",
                "The current prompt has unsaved changes.",
                "Save",
                "Don't Save",
                "Cancel");

            if (r == 2)
                return false;
            if (r == 0)
                SaveCurrent();
            return true;
        }

        bool TrySelectAsset(NarrativePromptAsset asset)
        {
            if (asset == _activeAsset)
                return true;
            if (!ConfirmNavigateAway())
                return false;
            LoadAssetInternal(asset);
            return true;
        }

        void SaveCurrent()
        {
            if (_activeAsset == null)
                return;

            Undo.RecordObject(_activeAsset, "Save narrative prompt");
            _activeAsset.originalText = _workingOriginal ?? "";
            _savedSnapshot = _workingOriginal ?? "";
            EditorUtility.SetDirty(_activeAsset);
            AssetDatabase.SaveAssets();
        }

        void ReloadFromAsset()
        {
            if (_activeAsset == null)
                return;
            if (!ConfirmNavigateAway())
                return;
            string path = AssetDatabase.GetAssetPath(_activeAsset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            _workingOriginal = _activeAsset.originalText ?? "";
            _savedSnapshot = _workingOriginal;
            RebuildSegments();
        }

        void CreateNewPrompt()
        {
            if (!ConfirmNavigateAway())
                return;

            string folder = EditorPrefs.GetString(PrefsLastFolder, "Assets");
            if (!AssetDatabase.IsValidFolder(folder))
                folder = "Assets";

            string path = EditorUtility.SaveFilePanelInProject(
                "New Narrative Prompt",
                "NewPrompt.asset",
                "asset",
                "Choose save location",
                folder);

            if (string.IsNullOrEmpty(path))
                return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                EditorPrefs.SetString(PrefsLastFolder, dir.Replace('\\', '/'));

            string unique = AssetDatabase.GenerateUniqueAssetPath(path);
            var asset = CreateInstance<NarrativePromptAsset>();
            asset.key = "prompt_" + System.DateTime.UtcNow.Ticks;
            asset.originalText = "";

            Undo.RegisterCreatedObjectUndo(asset, "Create narrative prompt");
            AssetDatabase.CreateAsset(asset, unique);
            if (_registry != null)
            {
                Undo.RecordObject(_registry, "Register prompt");
                _registry.Register(asset);
                EditorUtility.SetDirty(_registry);
            }

            AssetDatabase.SaveAssets();
            RefreshPromptList();
            LoadAssetInternal(asset);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        void ApplyPlaceholderEdits()
        {
            if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _segments.Count)
                return;
            PromptSegment s = _segments[_selectedSegmentIndex];
            if (!s.isPlaceholder)
                return;

            var pars = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _editKeys.Count; i++)
            {
                string k = (_editKeys[i] ?? "").Trim();
                if (string.IsNullOrEmpty(k))
                    continue;
                if (!pars.ContainsKey(k))
                    pars[k] = _editVals[i] ?? "";
            }

            _segments[_selectedSegmentIndex] = PromptSegment.Placeholder(
                s.start,
                s.length,
                _editName?.Trim() ?? "",
                pars);

            _workingOriginal = PromptSpanParser.JoinSegments(_segments);
            _segments.Clear();
            _segments.AddRange(PromptSpanParser.Parse(_workingOriginal ?? ""));
            int keep = Mathf.Clamp(_selectedSegmentIndex, 0, Mathf.Max(0, _segments.Count - 1));
            _selectedSegmentIndex = _segments.Count > 0 ? keep : -1;
            LoadEditFieldsFromSelectedSegment();
        }

        void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(280f, position.width * 0.55f)));

            EditorGUILayout.LabelField("Prompt registry", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _registry = (PromptRegistry)EditorGUILayout.ObjectField("Registry", _registry, typeof(PromptRegistry), false);
            if (EditorGUI.EndChangeCheck())
            {
                SaveRegistryToPrefs();
                RefreshPromptList();
            }
            if (_registry == null)
            {
                EditorGUILayout.HelpBox(
                    "No PromptRegistry assigned. Pick one, or create a new registry asset.",
                    MessageType.Warning);
                if (GUILayout.Button("Create PromptRegistry asset", GUILayout.Width(220)))
                    CreateRegistryAsset();
            }

            EditorGUILayout.LabelField("Prompts", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(140));
            for (int i = 0; i < _promptList.Count; i++)
            {
                NarrativePromptAsset p = _promptList[i];
                if (p == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                GUIStyle st = _activeAsset == p ? EditorStyles.boldLabel : EditorStyles.label;
                string label = $"{p.key}  ({p.name})";
                if (GUILayout.Button(label, st, GUILayout.ExpandWidth(true)))
                {
                    if (TrySelectAsset(p))
                    {
                        Selection.activeObject = p;
                        EditorGUIUtility.PingObject(p);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Original text", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _workingOriginal = EditorGUILayout.TextArea(_workingOriginal ?? "", GUILayout.MinHeight(120), GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
                RebuildSegments();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Segment tree", EditorStyles.boldLabel);
            float treeH = Mathf.Clamp(position.height * 0.28f, 140f, 260f);
            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll, GUILayout.Height(treeH));
            for (int i = 0; i < _segments.Count; i++)
            {
                PromptSegment seg = _segments[i];
                string row = seg.isPlaceholder
                    ? $"{{P}} {(!string.IsNullOrEmpty(seg.placeholderName) ? seg.placeholderName : "(params)")}  [{seg.length}]"
                    : $"Text  ({(seg.textRun != null ? seg.textRun.Length : 0)} chars)";
                GUIStyle rowStyle = _selectedSegmentIndex == i ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(row, rowStyle, GUILayout.ExpandWidth(true)))
                    OnTreeSelection(i);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            DrawPropertiesPanel();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New prompt", EditorStyles.toolbarButton, GUILayout.Width(90)))
                CreateNewPrompt();
            using (new EditorGUI.DisabledScope(_activeAsset == null))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    SaveCurrent();
            }

            using (new EditorGUI.DisabledScope(_activeAsset == null))
            {
                if (GUILayout.Button("Reload from disk", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    ReloadFromAsset();
            }

            GUILayout.FlexibleSpace();
            if (_activeAsset != null && IsDirty)
                GUILayout.Label("* unsaved", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void DrawPropertiesPanel()
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            if (_activeAsset == null)
            {
                EditorGUILayout.HelpBox("Select a prompt from the list or assign a registry.", MessageType.Info);
                return;
            }

            if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _segments.Count)
            {
                EditorGUILayout.HelpBox("Select a segment in the tree.", MessageType.None);
                return;
            }

            PromptSegment seg = _segments[_selectedSegmentIndex];
            EditorGUILayout.LabelField("Range", $"{seg.start} .. {seg.start + seg.length} (len {seg.length})");

            if (!seg.isPlaceholder)
            {
                EditorGUILayout.LabelField("Type", "Text run");
                EditorGUILayout.TextArea(seg.textRun ?? "", GUILayout.MinHeight(60));
                return;
            }

            EditorGUILayout.LabelField("Type", "{P} placeholder");
            EditorGUI.BeginChangeCheck();
            _editName = EditorGUILayout.TextField("Name", _editName);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Parameters", EditorStyles.miniLabel);
            for (int i = 0; i < _editKeys.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _editKeys[i] = EditorGUILayout.TextField(_editKeys[i] ?? "");
                _editVals[i] = EditorGUILayout.TextField(_editVals[i] ?? "");
                if (GUILayout.Button("-", GUILayout.Width(22)))
                {
                    _editKeys.RemoveAt(i);
                    _editVals.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add parameter"))
            {
                _editKeys.Add("key");
                _editVals.Add("");
            }

            EditorGUI.EndChangeCheck();

            if (GUILayout.Button("Apply to prompt", GUILayout.Height(26)))
                ApplyPlaceholderEdits();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Serialized span preview", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                PromptSpanParser.FormatPlaceholder(_editName?.Trim() ?? "", BuildEditDict()),
                EditorStyles.wordWrappedMiniLabel,
                GUILayout.MinHeight(36));
        }

        Dictionary<string, string> BuildEditDict()
        {
            var d = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _editKeys.Count; i++)
            {
                string k = (_editKeys[i] ?? "").Trim();
                if (string.IsNullOrEmpty(k) || d.ContainsKey(k))
                    continue;
                d[k] = _editVals[i] ?? "";
            }

            return d;
        }

        void CreateRegistryAsset()
        {
            string folder = EditorPrefs.GetString(PrefsLastFolder, "Assets");
            if (!AssetDatabase.IsValidFolder(folder))
                folder = "Assets";

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Prompt Registry",
                "PromptRegistry.asset",
                "asset",
                "Choose save location for PromptRegistry",
                folder);
            if (string.IsNullOrEmpty(path))
                return;

            var reg = CreateInstance<PromptRegistry>();
            Undo.RegisterCreatedObjectUndo(reg, "Create PromptRegistry");
            AssetDatabase.CreateAsset(reg, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();

            _registry = reg;
            SaveRegistryToPrefs();
            RefreshPromptList();
            Selection.activeObject = reg;
            EditorGUIUtility.PingObject(reg);
        }
    }
}
#endif
