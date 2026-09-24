/// <summary>
/// Lemma keys for sewing machine, serger, and lathe Frame/Shell PixelLight machines.
/// Catalog slot ids are underscore forms of these hyphenated lemmas.
/// </summary>
public static class SewingLemmaPropertyKeys
{
    public const string SewingMachine = "sewing-machine";
    public const string Serger = "serger";
    public const string Lathe = "lathe";
    public const string Stitch = "stitch";
    public const string StitchProgram = "stitch-program";
    public const string Lockstitch = "lockstitch";
    public const string Overlock = "overlock";
    public const string Hem = "hem";
    public const string Seam = "seam";
    public const string Needle = "needle";
    public const string Bobbin = "bobbin";
    public const string Looper = "looper";
    public const string Presser = "presser";
    public const string Hook = "hook";
    public const string NeedleThroat = "needle-throat";
    public const string BobbinRace = "bobbin-race";
    public const string ThreadPath = "thread-path";
    public const string LooperRace = "looper-race";
    public const string DoorBobbin = "door-bobbin";
    public const string DoorBed = "door-bed";
    public const string DoorLooper = "door-looper";
    public const string SewingShell = "sewing-shell";
    public const string SewingFrame = "sewing-frame";
    public const string SergerShell = "serger-shell";
    public const string LooperUpper = "looper-upper";
    public const string LooperLower = "looper-lower";
    public const string Differential = "differential";
    public const string SpindleBore = "spindle-bore";
    public const string TailstockQuill = "tailstock-quill";
    public const string ChipChute = "chip-chute";
    public const string DoorHeadstock = "door-headstock";
    public const string DoorGearbox = "door-gearbox";
    public const string DoorChipPan = "door-chip-pan";
    public const string LatheFrameBed = "lathe-frame-bed";
    public const string LatheShellCover = "lathe-shell-cover";
    public const string Headstock = "headstock";
    public const string Tailstock = "tailstock";
    public const string Sew = "sew";
    public const string Serge = "serge";

    public static readonly string[] LemmaPlaceholders =
    {
        SewingMachine, Serger, Lathe, Stitch, StitchProgram, Lockstitch, Overlock,
        Hem, Seam, Needle, Bobbin, Looper, Presser, Hook,
        NeedleThroat, BobbinRace, ThreadPath, LooperRace,
        DoorBobbin, DoorBed, DoorLooper,
        SewingShell, SewingFrame, SergerShell,
        LooperUpper, LooperLower, Differential,
        SpindleBore, TailstockQuill, ChipChute,
        DoorHeadstock, DoorGearbox, DoorChipPan,
        LatheFrameBed, LatheShellCover, Headstock, Tailstock,
        Sew, Serge
    };
}
