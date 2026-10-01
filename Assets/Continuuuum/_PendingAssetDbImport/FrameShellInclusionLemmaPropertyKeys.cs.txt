/// <summary>
/// Canonical lemmas for Bounds4 Frame/Shell inclusion and PixelLight hollow/door slot identity.
/// Slot catalog ids use underscores; <see cref="ToSlotId"/> / <see cref="FromSlotId"/> convert.
/// </summary>
public static class FrameShellInclusionLemmaPropertyKeys
{
    public const string Frame = "frame";
    public const string Shell = "shell";
    public const string Inclusion = "inclusion";
    public const string FrameInclusion = "frame-inclusion";
    public const string ShellInclusion = "shell-inclusion";
    public const string Hollow = "hollow";
    public const string HollowSubtract = "hollow-subtract";
    public const string FrameId = "frame-id";
    public const string DoorId = "door-id";
    public const string HingeLabel = "hinge-label";
    public const string SlotKind = "slot-kind";
    public const string ZIndex = "z-index";
    public const string HollowRadius = "hollow-radius";

    public const string HingeLeft = "left";
    public const string HingeRight = "right";
    public const string HingeFront = "front";
    public const string HingeRear = "rear";
    public const string HingeBottom = "bottom";

    public static readonly string[] LemmaPlaceholders =
    {
        Frame, Shell, Inclusion, FrameInclusion, ShellInclusion, Hollow, HollowSubtract,
        FrameId, DoorId, HingeLabel, SlotKind, ZIndex, HollowRadius
    };

    public static string ToSlotId(string lemma)
    {
        if (string.IsNullOrEmpty(lemma)) return lemma;
        return lemma.Replace('-', '_');
    }

    public static string FromSlotId(string slotId)
    {
        if (string.IsNullOrEmpty(slotId)) return slotId;
        return slotId.Replace('_', '-');
    }

    public static bool IsFrameInclusion(string lemma)
    {
        string t = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return t == Frame || t == FrameInclusion;
    }

    public static bool IsShellInclusion(string lemma)
    {
        string t = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return t == Shell || t == ShellInclusion || t == Inclusion;
    }
}
