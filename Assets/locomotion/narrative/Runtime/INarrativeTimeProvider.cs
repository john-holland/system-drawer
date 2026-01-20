namespace Locomotion.Narrative
{
    /// <summary>
    /// Pluggable time source for narrative scheduling.
    /// Implementations may be driven by Unity time, WeatherSystem updates, Timeline, etc.
    /// </summary>
    public interface INarrativeTimeProvider
    {
        NarrativeDateTime GetNow();
    }
}

