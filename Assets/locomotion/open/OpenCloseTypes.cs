namespace Locomotion.Open
{
    public enum OpenCloseJointKind
    {
        Hinge,
        Configurable,
        Slide,
        LatchOnly,
    }

    public enum AutoCloseBtMode
    {
        None,
        OnStopExit,
        AfterChildren,
        OnSequenceEnd,
        Manual,
    }

    public enum OpenCloseDriveMode
    {
        Physics,
        Animation,
        Hybrid,
    }

    public enum OpenCloseClosureMode
    {
        Auto,
        OpenBeatClosed,
        LatchFailed,
        CloseBeatClosed,
        Cancelled,
    }

    public enum OpenCloseQuestHintKind
    {
        None,
        Complete,
        Advance,
        Note,
        Change,
    }

    public enum OpenableJointState
    {
        Locked,
        Closed,
        Opening,
        Open,
        Closing,
    }
}
