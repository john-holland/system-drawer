using UnityEngine;

/// <summary>1:1 (or nudged) video-over-scene composite for recording preview.</summary>
public static class WebcamAnimPreviewOverlay
{
    public const float DefaultVideoOpacity = 0.5f;

    public static Rect AlignedVideoRect(Rect host, float scale, Vector2 offset01)
    {
        float s = Mathf.Max(0.05f, scale);
        float w = host.width * s;
        float h = host.height * s;
        float x = host.x + (host.width - w) * 0.5f + offset01.x * host.width;
        float y = host.y + (host.height - h) * 0.5f + offset01.y * host.height;
        return new Rect(x, y, w, h);
    }

    public static Color VideoTint(float opacity) =>
        new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
}
