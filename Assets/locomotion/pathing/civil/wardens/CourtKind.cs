/// <summary>
/// Court procedure flavor. American is adversarial with jury; English is inquisitorial;
/// Kangaroo collapses Rights/Constitution allow toward 0.
/// <see cref="Regular"/> aliases <see cref="American"/> for existing serialized data.
/// </summary>
public enum CourtKind
{
    American = 0,
    Regular = 0,
    Kangaroo = 1,
    English = 2
}

/// <summary>Coefficients derived from <see cref="CourtKind"/>.</summary>
public static class CourtKindCoeffs
{
    public static bool JuryRequired(CourtKind kind) =>
        kind != CourtKind.English && kind != CourtKind.Kangaroo;

    public static bool Adversarial(CourtKind kind) =>
        kind != CourtKind.English;

    public static float Kangaroo01(CourtKind kind) =>
        kind == CourtKind.Kangaroo ? 1f : 0f;
}
