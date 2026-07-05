#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Persistent notifications feed + console tab.</summary>
public sealed class ContinuuuumNotificationsWindow : EditorWindow
{
    static ContinuuuumNotificationFeedController _feed = new ContinuuuumNotificationFeedController();
    static int _badgeCount;

    int _tab;
    Vector2 _feedScroll;
    Vector2 _consoleScroll;
    string _filter = "";

    public static int UnreadBadgeCount => _badgeCount;

    [MenuItem("Window/Continuuuum/Notifications")]
    public static void Open()
    {
        var w = GetWindow<ContinuuuumNotificationsWindow>("Continuuuum Notifications");
        w.minSize = new Vector2(480, 360);
        w.RefreshFeed();
    }

    public static void NotifyBadgeUpdated(int count)
    {
        _badgeCount = count;
        var w = Resources.FindObjectsOfTypeAll<ContinuuuumNotificationsWindow>().FirstOrDefault();
        w?.Repaint();
    }

    void OnEnable()
    {
        _feed.Changed += Repaint;
        titleContent = new GUIContent("Notifications", EditorGUIUtility.IconContent("console.infoicon").image);
    }

    void OnDisable() => _feed.Changed -= Repaint;

    void OnGUI()
    {
        titleContent.text = _badgeCount > 0 ? $"Notifications ({_badgeCount})" : "Notifications";

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _tab = GUILayout.Toolbar(_tab, new[] { "Feed", "Console" }, EditorStyles.toolbarButton);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            RefreshFeed();
        if (GUILayout.Button("Hub", EditorStyles.toolbarButton, GUILayout.Width(40)))
            Application.OpenURL(ContinuuuumEditorSession.ApiBaseUrl + "/ui");
        EditorGUILayout.EndHorizontal();

        if (_tab == 0)
            DrawFeed();
        else
            DrawConsole();
    }

    void DrawFeed()
    {
        _feedScroll = EditorGUILayout.BeginScrollView(_feedScroll);
        foreach (var n in _feed.Items)
        {
            if (n == null) continue;
            bool unread = string.IsNullOrEmpty(n.readAt);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(ContinuuuumNotificationFeedController.TypeLabel(n.type), unread ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.LabelField(n.message ?? "", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (unread && GUILayout.Button("Mark read", GUILayout.Width(80)))
                MarkRead(n.id);
            if (!string.IsNullOrEmpty(n.draftId) && GUILayout.Button("Script Editor", GUILayout.Width(90)))
                ContinuuuumScriptEditorWindow.Open(n.draftId, n.reviewId);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawConsole()
    {
        _filter = EditorGUILayout.TextField("Filter type", _filter);
        _consoleScroll = EditorGUILayout.BeginScrollView(_consoleScroll);
        foreach (var e in ContinuuuumNotificationConsoleSink.Entries)
        {
            if (!string.IsNullOrEmpty(_filter) && (e.type?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(e.at.ToString("HH:mm:ss"), GUILayout.Width(64));
            EditorGUILayout.LabelField(e.type, GUILayout.Width(120));
            EditorGUILayout.LabelField(e.message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    async void RefreshFeed()
    {
        await _feed.RefreshAsync();
        _badgeCount = _feed.UnreadCount;
        NotifyBadgeUpdated(_badgeCount);
    }

    async void MarkRead(string id) => await _feed.MarkReadAsync(id);
}

#endif
