using System;

/// <summary>Timeline tick size from decimillisecond up. Slider maps to this enum.</summary>
public enum WebcamAnimTimelineGranularity
{
    Decimillisecond = 0,
    Millisecond = 1,
    Centisecond = 2,
    Decisecond = 3,
    Second = 4,
    Decasecond = 5,
    Minute = 6
}

public static class WebcamAnimTimelineGranularityUtil
{
    /// <summary>Tick duration in milliseconds for one unit at this granularity.</summary>
    public static double TickMs(WebcamAnimTimelineGranularity g)
    {
        switch (g)
        {
            case WebcamAnimTimelineGranularity.Decimillisecond: return 0.1;
            case WebcamAnimTimelineGranularity.Millisecond: return 1.0;
            case WebcamAnimTimelineGranularity.Centisecond: return 10.0;
            case WebcamAnimTimelineGranularity.Decisecond: return 100.0;
            case WebcamAnimTimelineGranularity.Second: return 1000.0;
            case WebcamAnimTimelineGranularity.Decasecond: return 10_000.0;
            case WebcamAnimTimelineGranularity.Minute: return 60_000.0;
            default: return 1.0;
        }
    }

    public static string JsonName(WebcamAnimTimelineGranularity g) =>
        g.ToString().ToLowerInvariant();

    public static WebcamAnimTimelineGranularity FromSlider(int sliderIndex)
    {
        int n = Enum.GetValues(typeof(WebcamAnimTimelineGranularity)).Length;
        if (sliderIndex < 0) sliderIndex = 0;
        if (sliderIndex >= n) sliderIndex = n - 1;
        return (WebcamAnimTimelineGranularity)sliderIndex;
    }

    public static double SnapMs(double ms, WebcamAnimTimelineGranularity g)
    {
        double tick = TickMs(g);
        if (tick <= 0) return ms;
        return Math.Round(ms / tick) * tick;
    }
}
