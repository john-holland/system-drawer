/// <summary>How additional formation rows are offset when cohort size exceeds slot count.</summary>
public enum TravelFormationWrapDirection
{
    /// <summary>Additional rows shift opposite travel forward (-forward * row * spacing).</summary>
    Back,
    /// <summary>Additional rows shift along +world right (relative to travel forward on XZ).</summary>
    Right,
    /// <summary>Additional rows shift along -world right.</summary>
    Left
}
