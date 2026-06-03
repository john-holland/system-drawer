using System;

/// <summary>Byte formatting for treemap labels.</summary>
public static class MemorySwizzleFormat
{
    public static string Bytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        double kb = bytes / 1024.0;
        if (kb < 1024)
            return kb.ToString("0.#") + " KB";
        double mb = kb / 1024.0;
        if (mb < 1024)
            return mb.ToString("0.##") + " MB";
        double gb = mb / 1024.0;
        return gb.ToString("0.##") + " GB";
    }

    public static string Percent(float p) => (p * 100f).ToString("0.#") + "%";
}
