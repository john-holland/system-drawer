#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Window → System Drawer → Active Cards — live pool + history ring buffer.</summary>
public sealed class ActiveCardsEditorWindow : EditorWindow
{
    PhysicsCardSolver _solver;
    Vector2 _scrollActive;
    Vector2 _scrollHistory;
    string _filter = "";
    int _bufferSize = 5000;

    [MenuItem("Window/System Drawer/Active Cards", false, 230)]
    public static void ShowWindow() => GetWindow<ActiveCardsEditorWindow>("Active Cards");

    void OnEnable() => EditorApplication.update += Repaint;

    void OnDisable() => EditorApplication.update -= Repaint;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Active Cards", EditorStyles.boldLabel);
        _solver = (PhysicsCardSolver)EditorGUILayout.ObjectField("Solver", _solver, typeof(PhysicsCardSolver), true);
        if (_solver == null && Selection.activeGameObject != null)
            _solver = Selection.activeGameObject.GetComponent<PhysicsCardSolver>();

        var hist = CardHistoryManager.Instance;
        if (hist == null)
        {
            if (GUILayout.Button("Create CardHistoryManager in scene"))
            {
                var go = new GameObject("CardHistoryManager");
                go.AddComponent<CardHistoryManager>();
            }
        }
        else
        {
            _bufferSize = EditorGUILayout.IntField("History buffer", hist.historyBufferSize);
            if (_bufferSize != hist.historyBufferSize && GUILayout.Button("Apply buffer size"))
                hist.SetBufferSize(_bufferSize);
            if (GUILayout.Button("Clear history"))
                hist.ClearHistory();
        }

        _filter = EditorGUILayout.TextField("Filter type", _filter);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Active (live pool snapshots)", EditorStyles.boldLabel);
        _scrollActive = EditorGUILayout.BeginScrollView(_scrollActive, GUILayout.Height(position.height * 0.4f));
        if (_solver != null)
        {
            IReadOnlyList<CardHistorySnapshot> active = hist != null
                ? hist.CopyActiveFrom(_solver)
                : CopyLocal(_solver);
            DrawSnaps(active);
        }
        else
            EditorGUILayout.HelpBox("Assign a PhysicsCardSolver.", MessageType.Info);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("History (newest first)", EditorStyles.boldLabel);
        _scrollHistory = EditorGUILayout.BeginScrollView(_scrollHistory);
        if (hist != null)
            DrawSnaps(hist.GetHistoryNewestFirst(300));
        else
            EditorGUILayout.HelpBox("No CardHistoryManager in scene.", MessageType.Warning);
        EditorGUILayout.EndScrollView();
    }

    void DrawSnaps(IReadOnlyList<CardHistorySnapshot> snaps)
    {
        if (snaps == null) return;
        for (int i = 0; i < snaps.Count; i++)
        {
            var s = snaps[i];
            if (s == null) continue;
            if (!string.IsNullOrEmpty(_filter) &&
                (s.typeName == null || s.typeName.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0) &&
                (s.displayName == null || s.displayName.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{s.eventKind} | {s.typeName} | {s.displayName}");
            EditorGUILayout.LabelField($"tag={s.physicalPathingTag}  duty={s.dutyOrActivitySummary}");
            EditorGUILayout.LabelField($"solver={s.actorOrSolverId}  t={s.unixMs}");
            EditorGUILayout.EndVertical();
        }
    }

    static List<CardHistorySnapshot> CopyLocal(PhysicsCardSolver solver)
    {
        var list = new List<CardHistorySnapshot>();
        if (solver?.availableCards == null) return list;
        for (int i = 0; i < solver.availableCards.Count; i++)
            list.Add(CardHistorySnapshot.FromCard(solver.availableCards[i], solver.name, "active"));
        return list;
    }
}

public sealed class ChefCardEditorWindow : EditorWindow
{
    ChefCard _card = ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Sear, null);

    [MenuItem("Window/System Drawer/Cards/Chef", false, 242)]
    public static void ShowWindow() => GetWindow<ChefCardEditorWindow>("Chef Cards");

    void OnGUI()
    {
        _card.dutyMode = (ChefDutyMode)EditorGUILayout.EnumPopup("Duty", _card.dutyMode);
        _card.activity = (ChefActivity)EditorGUILayout.EnumPopup("Activity", _card.activity);
        _card.stationOrTarget = (GameObject)EditorGUILayout.ObjectField("Station", _card.stationOrTarget, typeof(GameObject), true);
        _card.pourRateLitersPerSec = EditorGUILayout.FloatField("Pour L/s", _card.pourRateLitersPerSec);
        _card.accuracy01 = EditorGUILayout.Slider("Accuracy", _card.accuracy01, 0f, 1f);
        if (GUILayout.Button("Reset Defaults"))
            _card = ChefCard.Generate(_card.dutyMode, _card.activity, _card.stationOrTarget);
    }
}

public sealed class ScribeCardEditorWindow : EditorWindow
{
    const string PageImageFolder = "Assets/Scribe/Pages";

    ScribeCard _card = ScribeCard.Generate(ScribeActivity.Copy, "scribe-set");
    ScribePageRuntime _page;
    PenInkDrawingTarget _drawing;
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Cards/Scribe", false, 243)]
    public static void ShowWindow() => GetWindow<ScribeCardEditorWindow>("Scribe");

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Scribe card (duty)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This window authors a ScribeCard (Copy / Illuminate / Bind / Deliver). Page body — string or uploaded image — is below, and applies to ScribePageRuntime / PenInkDrawingTarget. Pen and Ink Studio bakes nibs and compiles strokes for IK.",
            MessageType.None);
        _card.activity = (ScribeActivity)EditorGUILayout.EnumPopup("Activity", _card.activity);
        _card.configId = EditorGUILayout.TextField("Config Id", _card.configId ?? "");
        _card.pageIndex = EditorGUILayout.IntField("Page", _card.pageIndex);
        _card.anchorKey = EditorGUILayout.TextField("Anchor", _card.anchorKey ?? "");
        _card.peckingOrder = EditorGUILayout.IntField("Pecking Order", _card.peckingOrder);
        _card.accuracy01 = EditorGUILayout.Slider("Accuracy", _card.accuracy01, 0f, 1f);
        _card.dialogTreeSetId = EditorGUILayout.TextField("Dialog Tree Set Id", _card.dialogTreeSetId ?? "");
        _card.pageSurface = (GameObject)EditorGUILayout.ObjectField(
            "Page Surface", _card.pageSurface, typeof(GameObject), true);
        if (GUILayout.Button("Reset Card Defaults"))
            _card = ScribeCard.Generate(_card.activity, _card.configId, _card.pageIndex, _card.anchorKey);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Page content", EditorStyles.boldLabel);
        _page = (ScribePageRuntime)EditorGUILayout.ObjectField(
            "Page Runtime", _page, typeof(ScribePageRuntime), true);
        _drawing = (PenInkDrawingTarget)EditorGUILayout.ObjectField(
            "Drawing Target", _drawing, typeof(PenInkDrawingTarget), true);
        if (_page == null && _card.pageSurface != null)
            _page = _card.pageSurface.GetComponent<ScribePageRuntime>();
        if (_drawing == null && _page != null)
            _drawing = _page.drawingTarget;
        if (_drawing == null && _card.pageSurface != null)
            _drawing = _card.pageSurface.GetComponent<PenInkDrawingTarget>();

        var kind = _page != null ? _page.sourceKind : (_drawing != null ? _drawing.sourceKind : PenInkDrawingTarget.SourceKind.Text);
        kind = (PenInkDrawingTarget.SourceKind)EditorGUILayout.EnumPopup("Source", kind);
        if (_page != null)
            _page.sourceKind = kind;
        if (_drawing != null)
            _drawing.sourceKind = kind;

        if (kind == PenInkDrawingTarget.SourceKind.Text)
        {
            EditorGUILayout.LabelField("String content");
            string body = _page != null ? _page.bodyText : (_drawing != null ? _drawing.sourceText : "");
            string next = EditorGUILayout.TextArea(body ?? "", GUILayout.MinHeight(80f));
            if (_page != null)
                _page.bodyText = next;
            if (_drawing != null)
                _drawing.sourceText = next;
        }
        else
        {
            Texture2D image = _page != null ? _page.sourceImage : (_drawing != null ? _drawing.sourceImage : null);
            image = (Texture2D)EditorGUILayout.ObjectField("Image", image, typeof(Texture2D), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Upload Image…"))
            {
                Texture2D uploaded = UploadPageImage();
                if (uploaded != null)
                    image = uploaded;
            }
            EditorGUILayout.EndHorizontal();
            if (_page != null)
                _page.sourceImage = image;
            if (_drawing != null)
                _drawing.sourceImage = image;
            if (image != null)
            {
                float h = Mathf.Min(160f, image.height);
                var r = GUILayoutUtility.GetRect(h * image.width / Mathf.Max(1, image.height), h);
                EditorGUI.DrawPreviewTexture(r, image, null, ScaleMode.ScaleToFit);
            }
        }

        if (GUILayout.Button("Apply Content To Page"))
            ApplyContent();
        if (GUILayout.Button("Compile Drawing Target") && _drawing != null)
            _drawing.Compile();

        EditorGUILayout.EndScrollView();
        if (_page != null)
            EditorUtility.SetDirty(_page);
        if (_drawing != null)
            EditorUtility.SetDirty(_drawing);
    }

    void ApplyContent()
    {
        if (_page == null && _card.pageSurface != null)
        {
            _page = _card.pageSurface.GetComponent<ScribePageRuntime>()
                    ?? _card.pageSurface.AddComponent<ScribePageRuntime>();
        }
        if (_page != null)
        {
            if (_drawing != null)
                _page.drawingTarget = _drawing;
            _page.configId = _card.configId;
            _page.pageIndex = _card.pageIndex;
            _page.anchorKey = _card.anchorKey;
            if (_page.sourceKind == PenInkDrawingTarget.SourceKind.Image)
                _page.ApplyImage(_page.sourceImage, _card.anchorKey);
            else
                _page.ApplyPage(_page.bodyText, _page.format, _card.anchorKey);
            EditorUtility.SetDirty(_page);
        }
        else if (_drawing != null)
        {
            if (_drawing.sourceKind == PenInkDrawingTarget.SourceKind.Text)
                _drawing.sourceText = _drawing.sourceText ?? "";
            _drawing.Compile();
            EditorUtility.SetDirty(_drawing);
        }
    }

    static Texture2D UploadPageImage()
    {
        string src = EditorUtility.OpenFilePanel("Scribe page image", "", "png,jpg,jpeg,tga,tif,tiff");
        if (string.IsNullOrEmpty(src))
            return null;
        if (!AssetDatabase.IsValidFolder("Assets/Scribe"))
            AssetDatabase.CreateFolder("Assets", "Scribe");
        if (!AssetDatabase.IsValidFolder(PageImageFolder))
            AssetDatabase.CreateFolder("Assets/Scribe", "Pages");
        string dest = AssetDatabase.GenerateUniqueAssetPath(
            PageImageFolder + "/" + System.IO.Path.GetFileName(src));
        string absDest = System.IO.Path.GetFullPath(dest);
        System.IO.File.Copy(src, absDest, overwrite: false);
        AssetDatabase.ImportAsset(dest);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
    }
}
#endif
