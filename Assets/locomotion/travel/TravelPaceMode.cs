/// <summary>
/// Longitudinal convoy / bin-packing bias when resolving multibody paths along a shared route.
/// </summary>
public enum TravelPaceMode
{
    /// <summary>Extra forward separation — lead slot.</summary>
    Lead,
    /// <summary>Balanced spacing.</summary>
    Keep,
    /// <summary>Extra rear separation — tail slot.</summary>
    Tail
}
