using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window: all <see cref="SystemDrawerAnimator"/> instances in open scenes and each animator's
/// active playback snapshots (play mode) or configured layers (edit mode).
/// </summary>
public class SystemDrawerAnimationHierarchyWindow : EditorWindow
{
    private Vector2 _scroll;
    private bool _liveRefresh = true;
    private string _search = "";
    private readonly Dictionary<int, bool> _foldoutOpen = new Dictionary<int, bool>();

    [MenuItem("Window/System Drawer/Animation/Animation Hierarchy", false, 100)]
    public static void Open()
    {
        var win = GetWindow<SystemDrawerAnimationHierarchyWindow>("Animation Hierarchy");
        win.minSize = new Vector2(420f, 280f);
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (_liveRefresh && Application.isPlaying)
            Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            _liveRefresh = EditorGUILayout.ToggleLeft("Live refresh (play mode)", _liveRefresh, GUILayout.Width(200f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                Repaint();
        }

        _search = EditorGUILayout.TextField("Search filter", _search);

        SystemDrawerAnimator[] animators = FindAllAnimators();
        System.Array.Sort(animators, CompareByHierarchyPath);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"System Drawer Animators: {animators.Length}", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        string lastScene = null;
        foreach (SystemDrawerAnimator anim in animators)
        {
            if (anim == null)
                continue;

            string path = GetHierarchyPath(anim.transform);
            if (!string.IsNullOrEmpty(_search) &&
                path.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                anim.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            string sceneName = anim.gameObject.scene.IsValid() ? anim.gameObject.scene.name : "(no scene)";
            if (sceneName != lastScene)
            {
                lastScene = sceneName;
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"Scene: {sceneName}", EditorStyles.boldLabel);
            }

            DrawAnimatorBlock(anim, path);
        }

        if (animators.Length == 0)
            EditorGUILayout.HelpBox("No SystemDrawerAnimator components found in loaded scenes.", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private static SystemDrawerAnimator[] FindAllAnimators()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<SystemDrawerAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<SystemDrawerAnimator>(true);
#endif
    }

    private static int CompareByHierarchyPath(SystemDrawerAnimator a, SystemDrawerAnimator b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int scene = string.CompareOrdinal(a.gameObject.scene.name, b.gameObject.scene.name);
        if (scene != 0) return scene;
        return string.CompareOrdinal(GetHierarchyPath(a.transform), GetHierarchyPath(b.transform));
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return "";
        var parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private void DrawAnimatorBlock(SystemDrawerAnimator anim, string hierarchyPath)
    {
        int id = anim.GetInstanceID();
        if (!_foldoutOpen.TryGetValue(id, out bool open))
            open = true;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUILayout.HorizontalScope())
        {
            open = EditorGUILayout.Foldout(open, hierarchyPath, true);
            _foldoutOpen[id] = open;

            if (GUILayout.Button("Select", GUILayout.Width(56f)))
            {
                Selection.activeGameObject = anim.gameObject;
                EditorGUIUtility.PingObject(anim.gameObject);
            }

            if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                EditorGUIUtility.PingObject(anim.gameObject);
        }

        if (!open)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("GameObject", anim.gameObject.name, EditorStyles.miniLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                $"Assert: {(anim.LastAssertPassed ? "OK" : "FAIL")} — {anim.LastAssertMessage}",
                anim.LastAssertPassed ? MessageType.None : MessageType.Warning);

            IReadOnlyList<AnimationPlaybackSnapshot> snaps = anim.ActiveSnapshots;
            const float eps = 0.0001f;
            if (snaps == null || snaps.Count == 0)
            {
                EditorGUILayout.LabelField("Running animations", "(no snapshots yet)");
            }
            else
            {
                EditorGUILayout.LabelField("Running animations", EditorStyles.boldLabel);
                bool any = false;
                foreach (AnimationPlaybackSnapshot s in snaps)
                {
                    if (s.weight <= eps)
                        continue;
                    any = true;
                    string line = $" L{s.layerIndex}  [{s.treeName}]  →  {s.activeNodeName}  w={s.weight:F2}";
                    if (s.normalizedTime > 0f)
                        line += $"  t={s.normalizedTime:F2}";
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
                }
                if (!any)
                    EditorGUILayout.LabelField("(no active layers above weight threshold)");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see live snapshots. Configured layers:", MessageType.Info);
            if (anim.layers == null || anim.layers.Count == 0)
            {
                EditorGUILayout.LabelField("(no layers configured)");
            }
            else
            {
                foreach (AnimationLayerSlot slot in anim.layers)
                {
                    if (slot == null)
                        continue;
                    string treeName = slot.animationBehaviorTree != null ? slot.animationBehaviorTree.gameObject.name : "(none)";
                    EditorGUILayout.LabelField($" L{slot.layerIndex}  {treeName}  w={slot.weight:F2}  {slot.displayName}");
                }
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }
}
