/// <summary>Canonical parkour animation / IK training group tags.</summary>
public static class ParkourAnimationGroup
{
    public const string LopingStrides = "parkour.loping_strides";
    public const string FallRolls = "parkour.fall_rolls";
    public const string Mantling = "parkour.mantling";
    public const string OneLegLanding = "parkour.one_leg_landing";
    public const string OneHandLanding = "parkour.one_hand_landing";
    public const string Swinging = "parkour.swinging";
    public const string FootSwinging = "parkour.foot_swinging";
    public const string ToeFingerHoldSwings = "parkour.toe_finger_hold_swings";
    public const string SpringLanding = "parkour.spring_landing";
    public const string SpringRollJump = "parkour.spring_roll_jump";
    public const string SlideDownLedgePropJump = "parkour.slide_down_ledge_prop_jump";
    public const string WallRun = "parkour.wall_run";
    public const string UnevenBarsSwing = "parkour.uneven_bars_swing";
    public const string JungleGymBars = "parkour.jungle_gym_bars";
    public const string CrashThrough = "parkour.crash_through";
    public const string AngularPassThrough = "parkour.angular_pass_through";

    public static readonly string[] All =
    {
        LopingStrides, FallRolls, Mantling, OneLegLanding, OneHandLanding,
        Swinging, FootSwinging, ToeFingerHoldSwings, SpringLanding, SpringRollJump,
        SlideDownLedgePropJump, WallRun, UnevenBarsSwing, JungleGymBars,
        CrashThrough, AngularPassThrough
    };
}

/// <summary>Rope inchworm IK / card tags.</summary>
public static class RopeInchwormAnimationGroup
{
    public const string MantleLeft = "rope.mantle_left";
    public const string MantleRight = "rope.mantle_right";
    public const string Lowering = "rope.lowering";
    public const string ClimbingUp = "rope.climbing_up";
    public const string ClimbOntoLedge = "rope.climb_onto_ledge";
    public const string Idling = "rope.idling";
    public const string TagSherpaCarry = "rope.attach_sherpa_carry";

    public static readonly string[] All =
    {
        MantleLeft, MantleRight, Lowering, ClimbingUp, ClimbOntoLedge, Idling, TagSherpaCarry
    };
}

/// <summary>Picks damage-minimizing parkour tags given markers + aperture mode.</summary>
public static class ParkourDamageMinAnimSelect
{
    public static string SelectForAperture(PathingAperture aperture, float damage01, bool hasStrongLead)
    {
        if (aperture == null)
            return ParkourAnimationGroup.LopingStrides;
        switch (aperture.passMode)
        {
            case PathingAperturePassMode.CrashThrough:
                if (damage01 > 0.45f)
                    return hasStrongLead ? ParkourAnimationGroup.CrashThrough : ParkourAnimationGroup.FallRolls;
                return ParkourAnimationGroup.SpringRollJump;
            case PathingAperturePassMode.AngularPassThrough:
                return damage01 > 0.35f
                    ? OneShoulderLead(hasStrongLead)
                    : ParkourAnimationGroup.AngularPassThrough;
            default:
                return ParkourAnimationGroup.LopingStrides;
        }
    }

    public static string OneShoulderLead(bool strong) =>
        strong ? ParkourAnimationGroup.OneHandLanding : ParkourAnimationGroup.FallRolls;

    public static string SelectLanding(float entrySpeed01, float damage01)
    {
        if (entrySpeed01 > 0.7f && damage01 < 0.3f)
            return ParkourAnimationGroup.SpringLanding;
        if (damage01 > 0.4f)
            return ParkourAnimationGroup.FallRolls;
        if (entrySpeed01 > 0.45f)
            return ParkourAnimationGroup.OneLegLanding;
        return ParkourAnimationGroup.LopingStrides;
    }
}
