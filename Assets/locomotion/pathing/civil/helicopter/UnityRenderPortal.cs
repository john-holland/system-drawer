using System;
using Continuuuum.Telecom;
using UnityEngine;

/// <summary>
/// Composites a RenderTexture over a webtop panel using JS portalBounds2 anchors.
/// Does not stream frames into the webtop (no cctvFrame path).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Unity Render Portal")]
public sealed class UnityRenderPortal : MonoBehaviour
{
    public string portalId = "gps";
    public RenderTexture sourceTexture;
    public Transform overlayParent;
    public MeshRenderer overlayRenderer;
    public float webtopWidthPx = 1280f;
    public float webtopHeightPx = 720f;
    public Rect lastBoundsPx;
    public Rect lastBoundsNormalized = new Rect(0f, 0f, 1f, 0.56f);
    public bool hasBounds;

    TelecomUnityBridge _bridge;
    static Material _sharedMat;

    void OnEnable()
    {
        TelecomUnityBridge.PortalBounds2Received += OnPortalBounds2;
    }

    void OnDisable()
    {
        TelecomUnityBridge.PortalBounds2Received -= OnPortalBounds2;
    }

    public void BindTelecom(Component telecom)
    {
        _bridge = telecom as TelecomUnityBridge;
        if (_bridge == null && telecom != null)
            _bridge = telecom.GetComponent<TelecomUnityBridge>();
    }

    void OnPortalBounds2(string payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson)) return;
        var wrapper = JsonUtility.FromJson<PortalBounds2Payload>(payloadJson);
        if (wrapper?.portals == null) return;
        for (int i = 0; i < wrapper.portals.Length; i++)
        {
            var p = wrapper.portals[i];
            if (p == null || (!string.IsNullOrEmpty(portalId) && p.portalId != portalId))
                continue;
            ApplyBounds(p);
            break;
        }
    }

    public void ApplyBounds(PortalBounds2Entry entry)
    {
        if (entry == null) return;
        lastBoundsPx = new Rect(entry.x, entry.y, entry.width, entry.height);
        if (entry.nw > 1e-4f && entry.nh > 1e-4f)
            lastBoundsNormalized = new Rect(entry.nx, entry.ny, entry.nw, entry.nh);
        else if (webtopWidthPx > 1f && webtopHeightPx > 1f)
            lastBoundsNormalized = new Rect(
                entry.x / webtopWidthPx,
                entry.y / webtopHeightPx,
                entry.width / webtopWidthPx,
                entry.height / webtopHeightPx);
        hasBounds = true;
        EnsureOverlayQuad();
        UpdateOverlayTransform();
        SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.PortalBounds,
            SendMessageOptions.DontRequireReceiver);
    }

    public void EnsureOverlayQuad()
    {
        if (overlayRenderer != null) return;
        Transform parent = overlayParent != null ? overlayParent : transform;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "UnityRenderPortal_" + portalId;
        go.transform.SetParent(parent, false);
        var col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
        overlayRenderer = go.GetComponent<MeshRenderer>();
        if (_sharedMat == null)
            _sharedMat = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        if (overlayRenderer != null)
            overlayRenderer.sharedMaterial = new Material(_sharedMat);
        ApplyTexture();
    }

    public void ApplyTexture()
    {
        if (overlayRenderer == null || sourceTexture == null) return;
        var mat = overlayRenderer.material;
        if (mat != null && mat.HasProperty("_MainTex"))
            mat.mainTexture = sourceTexture;
        else if (mat != null)
            mat.mainTexture = sourceTexture;
    }

    void UpdateOverlayTransform()
    {
        if (overlayRenderer == null) return;
        ApplyTexture();
        var t = overlayRenderer.transform;
        // Local plane: normalized rect mapped onto a unit webtop panel in front of mount.
        float w = Mathf.Max(0.05f, lastBoundsNormalized.width);
        float h = Mathf.Max(0.05f, lastBoundsNormalized.height);
        float cx = lastBoundsNormalized.x + w * 0.5f - 0.5f;
        float cy = 0.5f - (lastBoundsNormalized.y + h * 0.5f);
        t.localPosition = new Vector3(cx, cy, -0.01f);
        t.localScale = new Vector3(w, h, 1f);
        t.localRotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        if (sourceTexture != null && overlayRenderer != null)
            ApplyTexture();
    }

    [Serializable]
    public class PortalBounds2Payload
    {
        public PortalBounds2Entry[] portals;
    }

    [Serializable]
    public class PortalBounds2Entry
    {
        public string portalId;
        public float x, y, width, height;
        public float nx, ny, nw, nh;
    }
}
