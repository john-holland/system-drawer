using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Locomotion.Narrative;

/// <summary>
/// In-game UI for the Spatial 4D editor: markers, start/stop, datetime, timeline scrubber,
/// reticle, tool slots, and save to flat file. Shown when orchestrator.showInGameSpatial4DEditor is true in Play mode.
/// Optional: assign FirstPersonController to disable movement when "spatial editor mode" is on.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class Spatial4DInGameUI : MonoBehaviour
{
    [Header("References")]
    public SpatialGenerator4DOrchestrator orchestrator;
    [Tooltip("Optional: for current date/time and timeline t.")]
    public NarrativeClock narrativeClock;
    [Tooltip("Optional: disabled when entering spatial editor mode.")]
    public MonoBehaviour firstPersonController;
    [Tooltip("Optional: for reticle raycast. Defaults to Main Camera.")]
    public Camera raycastCamera;
    [Tooltip("Optional: position used for causality (auto-start when entering narrative volume). Defaults to camera transform.")]
    public Transform causalityPosition;

    [Header("Slots (assign in inspector or set at runtime via buttons)")]
    [Tooltip("Active game object for Mark game object and Use tool on.")]
    public GameObject activeGameObjectSlot;
    public Transform activeTransformSlot;
    [Tooltip("Tool to use (game object or assign toolTransformSlot).")]
    public GameObject toolSlot;
    public Transform toolTransformSlot;
    [Tooltip("Use tool on this object, or set Use tool on spacetime location.")]
    public GameObject useToolOnSlot;
    public Transform useToolOnTransformSlot;

    [Header("Save shortcut")]
    public KeyCode saveShortcut = KeyCode.S;
    public bool saveShortcutCtrl = true;

    [Header("Timeline bar")]
    [Tooltip("Bar represents this many seconds and refills modulo. Default 60 = one minute.")]
    [SerializeField] private float barModuloSeconds = 60f;
    [Tooltip("When true, bar fills by real (game) time so it refills every 60 real seconds. When false, bar uses narrative time.")]
    [SerializeField] private bool barUsesGameTime = true;

    private Canvas canvas;
    private RectTransform panelRoot;
    private bool spatialEditorModeActive;
    private List<Spatial4DExpressionEntryDto> entries = new List<Spatial4DExpressionEntryDto>();

    // Current state
    private float currentT;
    private bool recordingStarted;
    private bool barFrozen;      // true after Mark stop until Mark start
    private float barFrozenValue;
    private bool wasInsideNarrativeVolume;
    private Vector3 currentMarkedLocation;
    private bool hasMarkedLocation;
    private bool useToolOnSpacetimeLocation;
    private float timelineEndT = float.NaN;
    private GameObject reticleInstance;
    private const float ReticleDistance = 10f;

    // UI refs (built at runtime)
    private ScrollRect panelScrollRect;
    private InputField dateTimeInput;
    private Slider timelineSlider;
    private Text activeGoLabel;
    private Text locationLabel;
    private Text spacetimeLinkLabel;
    private Button saveButton;

    [Header("Roll-up (Alt release)")]
    [Tooltip("Roll-down/roll-up animation duration in seconds.")]
    [SerializeField] private float rollDuration = 0.25f;
    private float rollTween;
    private float savedScrollPosition = 1f;
    private bool wasCurtainDown;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (raycastCamera == null)
            raycastCamera = Camera.main;
        EnsureCanvas();
    }

    private void Start()
    {
        ResolveOrchestrator();
        SyncCurrentTFromClock();
        canvas.enabled = false;
    }

    private void ResolveOrchestrator()
    {
        if (orchestrator != null) return;
        orchestrator = GetComponentInParent<SpatialGenerator4DOrchestrator>();
        if (orchestrator == null)
            orchestrator = FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
    }

    private void EnsureCanvas()
    {
        if (panelRoot != null) return;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        if (canvas.gameObject.GetComponent<CanvasScaler>() == null)
        {
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        if (canvas.gameObject.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        var go = new GameObject("Spatial4DEditorPanel", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        panelRoot = (RectTransform)go.transform;
        panelRoot.anchorMin = new Vector2(0.02f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.35f, 0.98f);
        panelRoot.pivot = new Vector2(0f, 1f);
        panelRoot.offsetMin = Vector2.zero;
        panelRoot.offsetMax = Vector2.zero;

        var panelImage = go.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.92f);
        panelImage.raycastTarget = true;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.25f, 0.35f, 1f);
        outline.effectDistance = new Vector2(3, 3);

        panelScrollRect = go.AddComponent<ScrollRect>();
        panelScrollRect.horizontal = false;
        panelScrollRect.vertical = true;
        panelScrollRect.scrollSensitivity = 20f;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(go.transform, false);
        var viewportRect = (RectTransform)viewportGo.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = (RectTransform)contentGo.transform;
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0, -800f);
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 800f);
        var contentSizeFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var vertical = contentGo.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 4;
        vertical.padding = new RectOffset(8, 8, 8, 8);
        vertical.childControlHeight = false;
        vertical.childForceExpandHeight = false;

        panelScrollRect.viewport = viewportRect;
        panelScrollRect.content = contentRect;

        BuildUIChildren(contentGo.transform);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        panelScrollRect.verticalNormalizedPosition = 1f;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = FindAnyObjectByType<EventSystem>();
        if (es != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Debug.Log("[Spatial4D UI] EventSystem created - required for button clicks and scroll.");
    }

    private void BuildUIChildren(Transform parent)
    {
        AddLabel(parent, "Spatial 4D Editor");
        AddButton(parent, "Enter spatial editor mode", () => { Debug.Log("[Spatial4D UI] Button clicked: Enter spatial editor mode"); OnEnterSpatialEditorMode(); });
        AddButton(parent, "Exit spatial editor mode", () => { Debug.Log("[Spatial4D UI] Button clicked: Exit spatial editor mode"); OnExitSpatialEditorMode(); });

        dateTimeInput = AddInput(parent, "Date/Time (narrative)", "2025-01-01 00:00:00");
        if (dateTimeInput != null)
            dateTimeInput.onEndEdit.AddListener(OnDateTimeEdited);

        float modulo = Mathf.Max(0.1f, barModuloSeconds);
        AddLabel(parent, string.Format("Timeline (0–{0:F0}s)", modulo));
        timelineSlider = AddSlider(parent, 0f, modulo, 0f);
        if (timelineSlider != null)
            timelineSlider.onValueChanged.AddListener(OnMinuteBarChanged);

        activeGoLabel = AddLabel(parent, "Active GO: (none)");
        AddButton(parent, "Mark game object", () => { Debug.Log("[Spatial4D UI] Button clicked: Mark game object"); OnMarkGameObject(); });
        AddButton(parent, "Set game object to reticle", () => { Debug.Log("[Spatial4D UI] Button clicked: Set game object to reticle"); OnSetGameObjectToReticle(); });

        locationLabel = AddLabel(parent, "Location: —");
        AddButton(parent, "Set location", () => { Debug.Log("[Spatial4D UI] Button clicked: Set location"); OnSetLocation(); });
        AddButton(parent, "Mark start", () => { Debug.Log("[Spatial4D UI] Button clicked: Mark start"); OnMarkStart(); });
        AddButton(parent, "Mark stop", () => { Debug.Log("[Spatial4D UI] Button clicked: Mark stop"); OnMarkStop(); });

        spacetimeLinkLabel = AddLabel(parent, "Location @ t: (set location first)");
        AddButton(parent, "Use location at time (toggle)", () => { Debug.Log("[Spatial4D UI] Button clicked: Use location at time (toggle)"); useToolOnSpacetimeLocation = !useToolOnSpacetimeLocation; });
        AddLabel(parent, "Select tool (assign in inspector)");
        AddLabel(parent, "Use tool on (object or spacetime link)");
        AddButton(parent, "Record tool use", () => { Debug.Log("[Spatial4D UI] Button clicked: Record tool use"); OnRecordToolUse(); });
        saveButton = AddButton(parent, "Save to file", () => { Debug.Log("[Spatial4D UI] Button clicked: Save to file"); OnSave(); });
        AddLabel(parent, "Shortcut: Ctrl+S");
    }

    private Text AddLabel(Transform parent, string text)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 12;
        label.color = Color.white;
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 18;
        return label;
    }

    private InputField AddInput(Transform parent, string labelText, string placeholder)
    {
        var go = new GameObject("Input", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 22;
        var input = go.AddComponent<InputField>();
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.black;
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 2);
        textRect.offsetMax = new Vector2(-4, -2);
        input.textComponent = text;
        var image = go.AddComponent<Image>();
        image.color = Color.white;
        input.targetGraphic = image;
        input.text = placeholder;
        return input;
    }

    private Slider AddSlider(Transform parent, float min, float max, float value)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 20;
        var slider = go.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        var bgRect = (RectTransform)bg.transform;
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(bg.transform, false);
        var fillRect = (RectTransform)fill.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        slider.fillRect = fillRect;
        return slider;
    }

    private Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 24;
        var button = go.AddComponent<Button>();
        var image = go.AddComponent<Image>();
        image.color = new Color(0.56f, 1f, 0f, 1f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        if (onClick != null)
            button.onClick.AddListener(onClick);
        return button;
    }

    private static readonly Vector2 ExpandedAnchorMin = new Vector2(0.02f, 0.5f);
    private static readonly Vector2 ExpandedAnchorMax = new Vector2(0.35f, 0.98f);
    private static readonly Vector2 RolledAnchorMin = new Vector2(0.02f, 1f);
    private static readonly Vector2 RolledAnchorMax = new Vector2(0.35f, 1f);

    private void Update()
    {
        ResolveOrchestrator();
        if (orchestrator == null) return;
        bool shouldShow = orchestrator.showInGameSpatial4DEditor && Application.isPlaying;
        if (canvas != null)
            canvas.enabled = shouldShow;
        if (!shouldShow) return;

        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool curtainDown = altHeld || spatialEditorModeActive;
        float targetRoll = curtainDown ? 1f : 0f;
        float step = rollDuration > 0f ? Time.deltaTime / rollDuration : 1f;
        rollTween = Mathf.MoveTowards(rollTween, targetRoll, step);

        if (panelRoot != null)
        {
            panelRoot.anchorMin = Vector2.Lerp(RolledAnchorMin, ExpandedAnchorMin, rollTween);
            panelRoot.anchorMax = Vector2.Lerp(RolledAnchorMax, ExpandedAnchorMax, rollTween);
            panelRoot.offsetMin = Vector2.zero;
            panelRoot.offsetMax = Vector2.zero;
        }

        if (curtainDown)
            EnsureEventSystem();
        if (curtainDown && !wasCurtainDown && panelScrollRect != null)
            panelScrollRect.verticalNormalizedPosition = savedScrollPosition;
        if (curtainDown && panelScrollRect != null)
        {
            savedScrollPosition = panelScrollRect.verticalNormalizedPosition;
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float scrollStep = scroll * 0.05f;
                panelScrollRect.verticalNormalizedPosition = Mathf.Clamp01(panelScrollRect.verticalNormalizedPosition - scrollStep);
                Debug.Log($"[Spatial4D UI] Mouse wheel: delta={scroll:F2}, scrollPos={panelScrollRect.verticalNormalizedPosition:F3}");
            }
        }
        if (!curtainDown && wasCurtainDown && panelScrollRect != null)
            savedScrollPosition = panelScrollRect.verticalNormalizedPosition;
        wasCurtainDown = curtainDown;

        if (saveShortcutCtrl && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(saveShortcut))
            OnSave();

        float causalityT = narrativeClock != null ? NarrativeCalendarMath.DateTimeToSeconds(narrativeClock.Now) : currentT;
        Vector3 causalityPos = causalityPosition != null ? causalityPosition.position : (raycastCamera != null ? raycastCamera.transform.position : Vector3.zero);
        bool inside = NarrativeVolumeQuery.IsInsideNarrativeVolume(causalityPos, causalityT);

        if (orchestrator != null && orchestrator.autoStartWithCausality && !recordingStarted && inside && !wasInsideNarrativeVolume)
            recordingStarted = true;

        if (orchestrator != null && orchestrator.collectCausalityEvents && inside && !wasInsideNarrativeVolume)
            RecordCausalityTriggers(causalityPos, causalityT);

        wasInsideNarrativeVolume = inside;

        SyncCurrentTFromClock();
        UpdateReticle();
        if (timelineSlider != null)
        {
            float modulo = Mathf.Max(0.1f, barModuloSeconds);
            float barValue = barFrozen
                ? barFrozenValue
                : (barUsesGameTime ? (Time.time % modulo) : (currentT % modulo));
            timelineSlider.SetValueWithoutNotify(barValue);
        }
        GameObject activeGo = activeGameObjectSlot != null ? activeGameObjectSlot : activeTransformSlot != null ? activeTransformSlot.gameObject : null;
        if (activeGo == null)
            activeGo = GetReticleHitGameObject();
        if (activeGoLabel != null)
            activeGoLabel.text = "Active GO: " + (activeGo != null ? activeGo.name : "(none)");
        if (locationLabel != null)
        {
            Vector3 loc = GetReticleHitPosition();
            locationLabel.text = string.Format("Location: {0:F1}, {1:F1}, {2:F1}", loc.x, loc.y, loc.z);
        }
        if (spacetimeLinkLabel != null)
            spacetimeLinkLabel.text = hasMarkedLocation
                ? string.Format("Location @ t={0:F0}s: ({1:F1},{2:F1},{3:F1})", currentT, currentMarkedLocation.x, currentMarkedLocation.y, currentMarkedLocation.z)
                : "Location @ t: (set location first)";
    }

    private void SyncCurrentTFromClock()
    {
        if (narrativeClock == null) return;
        if (recordingStarted)
            currentT = NarrativeCalendarMath.DateTimeToSeconds(narrativeClock.Now);
        UpdateDateTimeDisplay();
    }

    private void UpdateDateTimeDisplay()
    {
        if (dateTimeInput != null)
            dateTimeInput.text = NarrativeCalendarMath.SecondsToNarrativeDateTime(currentT).ToString();
    }

    private void OnMinuteBarChanged(float barValue)
    {
        if (barUsesGameTime) return; // Bar is driven by game time; drag has no effect on narrative time.
        float modulo = Mathf.Max(0.1f, barModuloSeconds);
        currentT = Mathf.Floor(currentT / modulo) * modulo + Mathf.Clamp(barValue, 0f, modulo);
        UpdateDateTimeDisplay();
    }

    private void OnDateTimeEdited(string text)
    {
        if (narrativeClock == null) return;
        if (TryParseNarrativeDateTime(text, out Locomotion.Narrative.NarrativeDateTime dt))
        {
            currentT = NarrativeCalendarMath.DateTimeToSeconds(dt);
            if (timelineSlider != null && !barUsesGameTime)
                timelineSlider.SetValueWithoutNotify(currentT % Mathf.Max(0.1f, barModuloSeconds));
        }
    }

    private static bool TryParseNarrativeDateTime(string s, out Locomotion.Narrative.NarrativeDateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var parts = s.Trim().Replace("Z", "").Split(new[] { ' ', '-', ':', 'T' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return false;
        int y = int.TryParse(parts[0], out int vy) ? vy : 2025;
        int mo = int.TryParse(parts[1], out int vmo) ? vmo : 1;
        int d = int.TryParse(parts[2], out int vd) ? vd : 1;
        int h = int.TryParse(parts[3], out int vh) ? vh : 0;
        int min = int.TryParse(parts[4], out int vmin) ? vmin : 0;
        int sec = parts.Length > 5 && int.TryParse(parts[5], out int vsec) ? vsec : 0;
        result = new Locomotion.Narrative.NarrativeDateTime(y, mo, d, h, min, sec);
        return true;
    }

    private float GetTimelineMax()
    {
        if (!float.IsNaN(timelineEndT)) return timelineEndT;
        SpatialGenerator4D sg4 = GetFirst4DGenerator();
        return sg4 != null ? sg4.tMax : 3600f;
    }

    private SpatialGenerator4D GetFirst4DGenerator()
    {
        if (orchestrator == null || orchestrator.spatialGenerators == null) return null;
        foreach (var g in orchestrator.spatialGenerators)
        {
            if (g is SpatialGenerator4D sg4) return sg4;
        }
        return null;
    }

    private void RecordCausalityTriggers(Vector3 position, float t)
    {
        if (orchestrator == null || orchestrator.causalityTriggersTripped == null) return;
        var sg4 = GetFirst4DGenerator();
        if (sg4 == null) return;
        var bounds = new Bounds(position, Vector3.one * 1f);
        var markers = sg4.Search(bounds, t);
        if (markers == null) return;
        foreach (var marker in markers)
        {
            if (marker == null) continue;
            sg4.TryGetEntry(marker, out Bounds4 volume, out object payload);
            var dto = new CausalityTriggerTrippedDto
            {
                gameTime = t,
                px = position.x, py = position.y, pz = position.z,
                spatialNodeId = marker.name + "(" + marker.GetInstanceID() + ")",
                payloadLabel = payload != null ? payload.ToString() : null
            };
            orchestrator.causalityTriggersTripped.Add(dto);
        }
    }

    private void UpdateReticle()
    {
        if (raycastCamera == null) return;
        Vector3 hit = GetReticleHitPosition();
        if (reticleInstance == null)
        {
            reticleInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticleInstance.name = "Spatial4DReticle";
            reticleInstance.transform.localScale = Vector3.one * 0.3f;
            var renderer = reticleInstance.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
                renderer.material.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Object.Destroy(reticleInstance.GetComponent<Collider>());
        }
        reticleInstance.transform.position = hit;
        reticleInstance.transform.forward = raycastCamera.transform.forward;
    }

    private Vector3 GetReticleHitPosition()
    {
        if (raycastCamera == null) return Vector3.zero;
        Ray ray = raycastCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return hit.point;
        return ray.origin + ray.direction * ReticleDistance;
    }

    private GameObject GetReticleHitGameObject()
    {
        if (raycastCamera == null) return null;
        Ray ray = raycastCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f) && hit.collider != null)
            return hit.collider.gameObject;
        return null;
    }

    private void OnEnterSpatialEditorMode()
    {
        spatialEditorModeActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (firstPersonController != null)
            firstPersonController.enabled = false;
    }

    private void OnExitSpatialEditorMode()
    {
        spatialEditorModeActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (firstPersonController != null)
            firstPersonController.enabled = true;
    }

    private void OnSetLocation()
    {
        currentMarkedLocation = GetReticleHitPosition();
        hasMarkedLocation = true;
    }

    private void OnSetGameObjectToReticle()
    {
        if (raycastCamera == null) return;
        Ray ray = raycastCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            activeGameObjectSlot = hit.collider.gameObject;
            activeTransformSlot = hit.transform;
        }
    }

    private void OnRecordToolUse()
    {
        var toolGo = toolSlot != null ? toolSlot : toolTransformSlot != null ? toolTransformSlot.gameObject : null;
        var entry = new Spatial4DExpressionEntryDto
        {
            type = "ToolUse",
            id = System.Guid.NewGuid().ToString("N"),
            label = "Tool use",
            toolScenePath = toolGo != null ? GetScenePath(toolGo.transform) : "",
            toolInstanceId = toolGo != null ? toolGo.GetInstanceID() : 0
        };
        if (useToolOnSpacetimeLocation && hasMarkedLocation)
        {
            entry.targetIsSpacetimeLocation = true;
            entry.targetPx = currentMarkedLocation.x;
            entry.targetPy = currentMarkedLocation.y;
            entry.targetPz = currentMarkedLocation.z;
            entry.targetT = currentT;
        }
        else
        {
            var targetGo = useToolOnSlot != null ? useToolOnSlot : useToolOnTransformSlot != null ? useToolOnTransformSlot.gameObject : null;
            if (targetGo != null)
            {
                entry.targetScenePath = GetScenePath(targetGo.transform);
                entry.targetInstanceId = targetGo.GetInstanceID();
            }
        }
        entries.Add(entry);
    }

    private void OnMarkGameObject()
    {
        var go = activeGameObjectSlot != null ? activeGameObjectSlot : activeTransformSlot != null ? activeTransformSlot.gameObject : null;
        if (go == null)
        {
            if (raycastCamera != null)
            {
                Ray ray = raycastCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    activeGameObjectSlot = hit.collider.gameObject;
                    activeTransformSlot = hit.transform;
                    go = activeGameObjectSlot;
                }
            }
            if (go == null) return;
        }
        var entry = new Spatial4DExpressionEntryDto
        {
            type = "MarkedGameObject",
            id = System.Guid.NewGuid().ToString("N"),
            label = go.name,
            px = go.transform.position.x,
            py = go.transform.position.y,
            pz = go.transform.position.z,
            t = currentT,
            dateTimeString = NarrativeCalendarMath.SecondsToNarrativeDateTime(currentT).ToString(),
            scenePath = GetScenePath(go.transform),
            instanceId = go.GetInstanceID()
        };
        entries.Add(entry);
    }

    private void OnMarkStart()
    {
        recordingStarted = true;
        barFrozen = false;
        Vector3 pos = hasMarkedLocation ? currentMarkedLocation : GetReticleHitPosition();
        var entry = new Spatial4DExpressionEntryDto
        {
            type = "Start",
            id = System.Guid.NewGuid().ToString("N"),
            label = "Start",
            px = pos.x, py = pos.y, pz = pos.z,
            t = currentT,
            dateTimeString = NarrativeCalendarMath.SecondsToNarrativeDateTime(currentT).ToString()
        };
        entries.Add(entry);
        TryInsertInto4D(pos, currentT, 1f, "Start");
    }

    private void OnMarkStop()
    {
        Vector3 pos = hasMarkedLocation ? currentMarkedLocation : GetReticleHitPosition();
        var entry = new Spatial4DExpressionEntryDto
        {
            type = "Stop",
            id = System.Guid.NewGuid().ToString("N"),
            label = "Stop",
            px = pos.x, py = pos.y, pz = pos.z,
            t = currentT,
            dateTimeString = NarrativeCalendarMath.SecondsToNarrativeDateTime(currentT).ToString()
        };
        entries.Add(entry);
        TryInsertInto4D(pos, currentT, 1f, "Stop");
    }

    private void TryInsertInto4D(Vector3 center, float t, float durationT, object payload)
    {
        var sg4 = GetFirst4DGenerator();
        if (sg4 == null) return;
        var b4 = new Bounds4(center, Vector3.one * 0.5f, t, t + durationT);
        sg4.Insert(b4, payload);
    }

    private static string GetScenePath(Transform t)
    {
        if (t == null) return "";
        var path = new List<string>();
        while (t != null)
        {
            path.Add(t.name);
            t = t.parent;
        }
        path.Reverse();
        return string.Join("/", path);
    }

    private void OnSave()
    {
        if (orchestrator == null || entries == null) return;
        string path = Spatial4DExportUtility.ResolvePath(orchestrator.inGameUIOutputFilePath);
        var format = Spatial4DExportUtility.FromSpatial4DOutputFormat(orchestrator.inGameUIOutputFormat);
        if (orchestrator.inGameUIAppendToFile)
            Spatial4DExportUtility.AppendToFile(path, entries, format);
        else
        {
            var dto = new Spatial4DExpressionsDto { schemaVersion = 1 };
            dto.entries.AddRange(entries);
            if (!float.IsNaN(timelineEndT))
                dto.timelineEndT = timelineEndT;
            Spatial4DExportUtility.WriteToFile(path, dto, format);
        }
        entries.Clear();
    }

    private void OnDestroy()
    {
        if (reticleInstance != null)
            Destroy(reticleInstance);
    }
}
