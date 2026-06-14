using System;

/// <summary>Display formatting helpers for PerfTrace.</summary>
public static class PerfTraceFormat
{
    public static double TicksToMs(long ticks) => ticks * 1000.0 / TimeSpan.TicksPerSecond;

    public static string Ms(long ticks)
    {
        double ms = TicksToMs(ticks);
        if (ms < 1d)
            return ms.ToString("0.###") + " ms";
        if (ms < 1000d)
            return ms.ToString("0.##") + " ms";
        return (ms / 1000d).ToString("0.##") + " s";
    }

    public static string Ms(double ms)
    {
        if (ms < 1d)
            return ms.ToString("0.###") + " ms";
        if (ms < 1000d)
            return ms.ToString("0.##") + " ms";
        return (ms / 1000d).ToString("0.##") + " s";
    }

    public static string Percent(float ratio) => (ratio * 100f).ToString("0.#") + "%";

    public static string Bytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        double kb = bytes / 1024.0;
        if (kb < 1024)
            return kb.ToString("0.##") + " KB";
        double mb = kb / 1024.0;
        if (mb < 1024)
            return mb.ToString("0.##") + " MB";
        return (mb / 1024.0).ToString("0.##") + " GB";
    }
}
