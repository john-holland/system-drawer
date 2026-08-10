/// <summary>DispatchRequest.kind vocabulary for train station / dispatch / engineer ops.</summary>
public static class TrainDispatchKinds
{
    public const string TrainStation = "train_station";
    public const string TrainDispatch = "train_dispatch";
    public const string EngineerStart = "train_engineer_start";
    public const string EngineerStop = "train_engineer_stop";
    public const string DispatchStart = "train_dispatch_start";
    public const string DispatchStop = "train_dispatch_stop";
    public const string SpeedAdjust = "train_speed_adjust";
    public const string EngineerSpeedAdjust = "train_engineer_speed_adjust";
    public const string TrafficStop = "train_engineer_traffic_stop";
    public const string Plow = "train_engineer_plow";
    public const string Justice = "train_engineer_justice";
    public const string TurnstileRequest = "train_dispatch_turnstile";
    public const string TurnstileEngineer = "train_engineer_turnstile";
    public const string FollowTrainRequest = "train_dispatch_follow";
    public const string FollowTrainEngineer = "train_engineer_follow";
    public const string Attendant = "tsa_train_attendant";
    public const string YardBackupForward = "train_yard_backup_forward";
    public const string YardTowPush = "train_yard_tow_push";
    public const string EngineerCompose = "train_engineer";
}
