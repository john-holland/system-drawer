using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Animation / take length: user-typed limit, else video length, else the loaded animation file.
/// </summary>
public static class AnimationSpanDuration
{
    public const double DefaultRecordingEndMs = 1000.0;
    public const float DefaultPreviewSeconds = 2f;

    public static bool LooksLikeDefaultRecordingSpan(double startMs, double endMs) =>
        startMs <= 0.0 && Math.Abs(endMs - DefaultRecordingEndMs) < 0.51;

    public static float ResolveSeconds(
        float userLimitSeconds,
        float videoSeconds,
        float animationFileSeconds,
        float fallbackSeconds = DefaultPreviewSeconds)
    {
        if (userLimitSeconds > 0f)
            return userLimitSeconds;
        if (videoSeconds > 0f)
            return videoSeconds;
        if (animationFileSeconds > 0f)
            return animationFileSeconds;
        return Mathf.Max(0.1f, fallbackSeconds);
    }

    public static double ResolveMs(
        double userLimitMs,
        double videoMs,
        double animationFileMs,
        double fallbackMs = DefaultRecordingEndMs)
    {
        if (userLimitMs > 0.0)
            return userLimitMs;
        if (videoMs > 0.0)
            return videoMs;
        if (animationFileMs > 0.0)
            return animationFileMs;
        return Math.Max(1.0, fallbackMs);
    }

    public static bool TryParseSeconds(string text, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
    }

    public static bool TryParseMs(string text, out double ms)
    {
        ms = 0.0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out ms);
    }

    public static string FormatMs(double ms) =>
        ms.ToString("0.###", CultureInfo.InvariantCulture);

    public static string FormatSeconds(float seconds) =>
        seconds.ToString("0.###", CultureInfo.InvariantCulture);

    public static float MsToSeconds(double ms) => (float)(Math.Max(0.0, ms) / 1000.0);
}
