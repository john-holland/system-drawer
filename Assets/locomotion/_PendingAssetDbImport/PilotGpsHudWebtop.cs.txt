using System.Collections.Generic;
using UnityEngine;

/// <summary>GPS content source for UnityRenderPortal — baked TravelAgent route or realtime nadir ortho.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Pilot GPS HUD Webtop")]
public sealed class PilotGpsHudWebtop : MonoBehaviour
{
    public HelicopterVehicleRagdoll helicopter;
    public AirplaneVehicleRagdoll airplane;
    public TravelAgent travelAgent;
    public Transform mount;
    public PilotGpsHudMode mode = PilotGpsHudMode.BakedRoute;
    public RenderTexture displayTexture;
    public float nadirOrthoSize = 80f;
    public float refreshHz = 8f;
    public int rtWidth = 512;
    public int rtHeight = 512;
    public PilotGpsRouteBakeCache bakeCache;
    public Color routeColor = new Color(0.2f, 0.9f, 1f, 1f);
    public Color backgroundColor = new Color(0.05f, 0.08f, 0.1f, 1f);

    Camera _nadirCam;
    float _nextRefresh;
    Texture2D _cpuBake;

    public RenderTexture EnsureRenderTexture(int w = 0, int h = 0)
    {
        if (w > 0) rtWidth = w;
        if (h > 0) rtHeight = h;
        if (displayTexture != null && displayTexture.width == rtWidth && displayTexture.height == rtHeight)
            return displayTexture;
        if (displayTexture != null)
            displayTexture.Release();
        displayTexture = new RenderTexture(rtWidth, rtHeight, 16, RenderTextureFormat.ARGB32)
        {
            name = "PilotGpsHud_" + gameObject.name
        };
        displayTexture.Create();
        if (helicopter?.renderPortal != null)
        {
            helicopter.renderPortal.sourceTexture = displayTexture;
            helicopter.renderPortal.ApplyTexture();
        }
        return displayTexture;
    }

    void Awake()
    {
        if (helicopter == null)
            helicopter = GetComponent<HelicopterVehicleRagdoll>();
        if (airplane == null)
            airplane = GetComponent<AirplaneVehicleRagdoll>();
        EnsureRenderTexture();
    }

    void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + (refreshHz > 0.1f ? 1f / refreshHz : 0.125f);
        EnsureRenderTexture();
        if (mode == PilotGpsHudMode.RealtimeIsometric)
            RenderRealtimeNadir();
        else
            RenderBakedRoute();
    }

    public void Open()
    {
        EnsureRenderTexture();
        SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.GpsOpen,
            SendMessageOptions.DontRequireReceiver);
    }

    public void Close()
    {
        SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.GpsClose,
            SendMessageOptions.DontRequireReceiver);
    }

    void EnsureNadirCamera()
    {
        if (_nadirCam != null) return;
        var go = new GameObject("PilotGpsNadirCam");
        go.transform.SetParent(transform, false);
        _nadirCam = go.AddComponent<Camera>();
        _nadirCam.orthographic = true;
        _nadirCam.enabled = false;
        _nadirCam.clearFlags = CameraClearFlags.SolidColor;
        _nadirCam.backgroundColor = backgroundColor;
    }

    void RenderRealtimeNadir()
    {
        EnsureNadirCamera();
        Transform body = helicopter != null ? helicopter.transform
            : (airplane != null ? airplane.transform : transform);
        _nadirCam.orthographicSize = nadirOrthoSize;
        _nadirCam.transform.position = body.position + Vector3.up * 200f;
        _nadirCam.transform.rotation = Quaternion.Euler(90f, body.eulerAngles.y, 0f);
        _nadirCam.targetTexture = displayTexture;
        _nadirCam.Render();
        _nadirCam.targetTexture = null;
    }

    void RenderBakedRoute()
    {
        if (_cpuBake == null || _cpuBake.width != rtWidth || _cpuBake.height != rtHeight)
            _cpuBake = new Texture2D(rtWidth, rtHeight, TextureFormat.RGBA32, false);
        var pixels = new Color[rtWidth * rtHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;

        List<Vector3> points = null;
        if (bakeCache != null && bakeCache.waypoints != null && bakeCache.waypoints.Count > 0)
            points = bakeCache.waypoints;
        else if (travelAgent != null && travelAgent.CachedPlan != null)
            points = FlattenPlan(travelAgent);

        if (points != null && points.Count > 1)
        {
            Bounds b = bakeCache != null && bakeCache.hasBounds
                ? bakeCache.worldBounds
                : BoundsFromPoints(points);
            for (int i = 1; i < points.Count; i++)
                DrawLine(pixels, WorldToUv(points[i - 1], b), WorldToUv(points[i], b), routeColor);
            Transform body = helicopter != null ? helicopter.transform
                : (airplane != null ? airplane.transform : transform);
            Vector2Int craft = WorldToUv(body.position, b);
            FillCircle(pixels, craft, 4, Color.yellow);
        }

        _cpuBake.SetPixels(pixels);
        _cpuBake.Apply(false);
        Graphics.Blit(_cpuBake, displayTexture);
    }

    public static List<Vector3> FlattenPlan(TravelAgent agent)
    {
        var list = new List<Vector3>();
        var plan = agent?.CachedPlan;
        if (plan?.segments == null) return list;
        for (int s = 0; s < plan.segments.Count; s++)
        {
            var seg = plan.segments[s];
            if (seg?.waypoints == null) continue;
            for (int i = 0; i < seg.waypoints.Count; i++)
                list.Add(seg.waypoints[i]);
        }
        return list;
    }

    public static PilotGpsRouteBakeCache BakeFromTravelAgent(TravelAgent agent, int width, int height)
    {
        var cache = ScriptableObject.CreateInstance<PilotGpsRouteBakeCache>();
        cache.waypoints = FlattenPlan(agent);
        if (cache.waypoints.Count > 0)
        {
            cache.worldBounds = BoundsFromPoints(cache.waypoints);
            cache.hasBounds = true;
        }
        cache.rtWidth = width;
        cache.rtHeight = height;
        return cache;
    }

    static Bounds BoundsFromPoints(List<Vector3> pts)
    {
        var b = new Bounds(pts[0], Vector3.one);
        for (int i = 1; i < pts.Count; i++)
            b.Encapsulate(pts[i]);
        b.Expand(Mathf.Max(10f, b.size.magnitude * 0.1f));
        return b;
    }

    Vector2Int WorldToUv(Vector3 world, Bounds b)
    {
        float u = Mathf.InverseLerp(b.min.x, b.max.x, world.x);
        float v = Mathf.InverseLerp(b.min.z, b.max.z, world.z);
        return new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(u * (rtWidth - 1)), 0, rtWidth - 1),
            Mathf.Clamp(Mathf.RoundToInt(v * (rtHeight - 1)), 0, rtHeight - 1));
    }

    void DrawLine(Color[] pixels, Vector2Int a, Vector2Int b, Color c)
    {
        int x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            int idx = y0 * rtWidth + x0;
            if (idx >= 0 && idx < pixels.Length) pixels[idx] = c;
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    void FillCircle(Color[] pixels, Vector2Int c, int r, Color col)
    {
        for (int y = -r; y <= r; y++)
        for (int x = -r; x <= r; x++)
        {
            if (x * x + y * y > r * r) continue;
            int px = c.x + x, py = c.y + y;
            if (px < 0 || py < 0 || px >= rtWidth || py >= rtHeight) continue;
            pixels[py * rtWidth + px] = col;
        }
    }
}

/// <summary>Baked CachedPlan waypoints for classical GPS HUD.</summary>
[CreateAssetMenu(fileName = "PilotGpsRouteBake", menuName = "Locomotion/Travel/Pilot GPS Route Bake")]
public sealed class PilotGpsRouteBakeCache : ScriptableObject
{
    public List<Vector3> waypoints = new List<Vector3>();
    public Bounds worldBounds;
    public bool hasBounds;
    public int rtWidth = 512;
    public int rtHeight = 512;
}
