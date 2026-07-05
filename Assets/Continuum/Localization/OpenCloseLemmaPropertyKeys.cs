/// <summary>Property keys for open/close lemma verbs.</summary>
public static class OpenCloseLemmaPropertyKeys
{
    public const string OpenAngleDeg = "open-angle-deg";
    public const string DriveMode = "drive-mode";
    public const string RequireToolLemma = "require-tool-lemma";
    public const string UnlockBeforeOpen = "unlock-before-open";
    public const string CameraStopId = "camera-stop-id";
    public const string LinearOnly = "linear-only";
    public const string ActorIkProfileRef = "actor-ik-profile-ref";
    public const string ObjectIkProfileRef = "object-ik-profile-ref";
    public const string OpenAnimationRef = "open-animation-ref";
    public const string CloseAnimationRef = "close-animation-ref";
    public const string SoundOpenRef = "sound-open-ref";
    public const string SoundCloseRef = "sound-close-ref";
    public const string DialogueSpanRef = "dialogue-span-ref";
    public const string QuestHintKind = "quest-hint-kind";
    public const string QuestObjectiveId = "quest-objective-id";
    public const string AutoCloseBt = "auto-close-bt";
    public const string AutoCloseOnExit = "auto-close-on-exit";
    public const string CompileCloseAmbulation = "compile-close-ambulation";
    public const string ClosureMode = "closure-mode";
    public const string ArrivalBlendCoefficient = "arrival-blend-coefficient";
    public const string ReachRadiusMeters = "reach-radius-meters";
    public const string RequireFacingTarget = "require-facing-target";

    public static readonly string[] AllKeys =
    {
        OpenAngleDeg, DriveMode, RequireToolLemma, UnlockBeforeOpen, CameraStopId, LinearOnly,
        ActorIkProfileRef, ObjectIkProfileRef, OpenAnimationRef, CloseAnimationRef,
        SoundOpenRef, SoundCloseRef, DialogueSpanRef, QuestHintKind, QuestObjectiveId,
        AutoCloseBt, AutoCloseOnExit, CompileCloseAmbulation, ClosureMode,
        ArrivalBlendCoefficient, ReachRadiusMeters, RequireFacingTarget,
    };
}

public enum OpenCloseLemmaDriveMode
{
    Physics,
    Animation,
    Hybrid,
}

public enum OpenCloseLemmaClosureMode
{
    Auto,
    OpenBeatClosed,
    LatchFailed,
    CloseBeatClosed,
    Cancelled,
}

public enum OpenCloseLemmaQuestHintKind
{
    None,
    Complete,
    Advance,
    Note,
    Change,
}

public enum OpenCloseLemmaAutoCloseBtMode
{
    None,
    OnStopExit,
    AfterChildren,
    OnSequenceEnd,
    Manual,
}

[System.Serializable]
public struct OpenCloseLemmaProperties
{
    public float openAngleDeg;
    public OpenCloseLemmaDriveMode driveMode;
    public string requireToolLemma;
    public bool unlockBeforeOpen;
    public string cameraStopId;
    public bool linearOnly;
    public string actorIkProfileRef;
    public string objectIkProfileRef;
    public string openAnimationRef;
    public string closeAnimationRef;
    public string soundOpenRef;
    public string soundCloseRef;
    public string dialogueSpanRef;
    public OpenCloseLemmaQuestHintKind questHintKind;
    public string questObjectiveId;
    public OpenCloseLemmaAutoCloseBtMode autoCloseBt;
    public bool autoCloseOnExit;
    public bool compileCloseAmbulation;
    public OpenCloseLemmaClosureMode closureMode;
    public float arrivalBlendCoefficient;
    public float reachRadiusMeters;
    public bool requireFacingTarget;

    public static OpenCloseLemmaProperties Defaults => new OpenCloseLemmaProperties
    {
        openAngleDeg = 90f,
        driveMode = OpenCloseLemmaDriveMode.Hybrid,
        autoCloseBt = OpenCloseLemmaAutoCloseBtMode.OnStopExit,
        arrivalBlendCoefficient = 0f,
        reachRadiusMeters = 0.6f,
        requireFacingTarget = true,
        closureMode = OpenCloseLemmaClosureMode.Auto,
    };
}
