using UnityEngine;
using UnityEngine.UI;

/// <summary>In-game planet geometry tools (SDF / voxels / stamps).</summary>
[RequireComponent(typeof(Canvas))]
public class PlanetGeometryToolsUI : MonoBehaviour
{
    public enum ToolMode { Sculpt, PaintHeight, Biome, LiquidPermeability, StampPlanarFeature, ImportScan }
    public enum Representation { Sdf, Voxels, DualVoxels }

    public ToolMode mode = ToolMode.Sculpt;
    public Representation representation = Representation.Sdf;
    public Planetary.PlanetBody planetBody;

    Canvas _canvas;
    Dropdown _modeDropdown;
    Dropdown _repDropdown;

    void Start()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        BuildUi();
    }

    void BuildUi()
    {
        var panel = new GameObject("PlanetToolsPanel");
        panel.transform.SetParent(transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(10f, -10f);
        rect.sizeDelta = new Vector2(280f, 120f);

        _modeDropdown = CreateDropdown(panel.transform, "Mode", System.Enum.GetNames(typeof(ToolMode)), 0, OnModeChanged);
        _repDropdown = CreateDropdown(panel.transform, "Representation", System.Enum.GetNames(typeof(Representation)), 0, OnRepChanged);
    }

    static Dropdown CreateDropdown(Transform parent, string label, string[] options, int yRow, UnityEngine.Events.UnityAction<int> onChanged)
    {
        var go = new GameObject(label + "Dropdown");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -30f * yRow);
        rt.sizeDelta = new Vector2(0f, 28f);
        var dd = go.AddComponent<Dropdown>();
        dd.options.Clear();
        for (int i = 0; i < options.Length; i++)
            dd.options.Add(new Dropdown.OptionData(options[i]));
        dd.value = 0;
        dd.onValueChanged.AddListener(onChanged);
        return dd;
    }

    void OnModeChanged(int idx)
    {
        mode = (ToolMode)idx;
        if (mode == ToolMode.StampPlanarFeature && planetBody != null)
            planetBody.RebuildAll();
    }

    void OnRepChanged(int idx) => representation = (Representation)idx;
}
