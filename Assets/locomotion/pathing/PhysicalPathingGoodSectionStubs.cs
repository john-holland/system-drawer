using UnityEngine;

/// <summary>
/// Factory for stub good sections tagged by physical medium (minimal impulse stacks for planner filtering).
/// </summary>
public static class PhysicalPathingGoodSectionStubs
{
    public static GoodSection CreateAirGlideStub()
    {
        return new GoodSection
        {
            sectionName = "stub_air_glide",
            description = "Stub air locomotion section",
            physicalPathingMedium = PhysicalPathingMedium.Air,
            impulseStack = new System.Collections.Generic.List<ImpulseAction>()
        };
    }

    public static GoodSection CreateWaterSwimStub()
    {
        return new GoodSection
        {
            sectionName = "stub_water_swim",
            description = "Stub water locomotion section",
            physicalPathingMedium = PhysicalPathingMedium.Water,
            impulseStack = new System.Collections.Generic.List<ImpulseAction>()
        };
    }

    public static GoodSection CreateSpaceEvStub()
    {
        return new GoodSection
        {
            sectionName = "stub_space_ev",
            description = "Stub EVA / space locomotion section",
            physicalPathingMedium = PhysicalPathingMedium.Space,
            impulseStack = new System.Collections.Generic.List<ImpulseAction>()
        };
    }

    public static GoodSection CreateDriveSteerStub(string instrumentChannelKey)
    {
        var stack = new System.Collections.Generic.List<ImpulseAction>();
        if (!string.IsNullOrEmpty(instrumentChannelKey))
        {
            stack.Add(new ImpulseAction
            {
                muscleGroup = instrumentChannelKey,
                activation = 0.5f,
                duration = 0.2f
            });
        }

        return new GoodSection
        {
            sectionName = "stub_drive_steer",
            description = "Stub steer section (requires instrument map)",
            physicalPathingMedium = PhysicalPathingMedium.Ground,
            driveAnimationPhase = DriveAnimationPhase.Steer,
            driveInstrumentId = "steer",
            impulseStack = stack
        };
    }

    public static GoodSection CreateDriveThrottleStub(string instrumentChannelKey)
    {
        var stack = new System.Collections.Generic.List<ImpulseAction>();
        if (!string.IsNullOrEmpty(instrumentChannelKey))
        {
            stack.Add(new ImpulseAction
            {
                muscleGroup = instrumentChannelKey,
                activation = 0.6f,
                duration = 0.25f
            });
        }

        return new GoodSection
        {
            sectionName = "stub_drive_throttle",
            description = "Stub throttle section (requires instrument map)",
            physicalPathingMedium = PhysicalPathingMedium.Ground,
            driveAnimationPhase = DriveAnimationPhase.Throttle,
            driveInstrumentId = "throttle",
            impulseStack = stack
        };
    }
}
