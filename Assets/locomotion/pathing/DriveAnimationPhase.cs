using System;

/// <summary>
/// Phased timing for driving-related animation service actions and GoodSection metadata.
/// </summary>
[Flags]
public enum DriveAnimationPhase
{
    None = 0,
    Enter = 1,
    Exit = 2,
    Drive = 4,
    Steer = 8,
    Throttle = 16,
    Brake = 32,
    Shift = 64,
    Aux = 128
}
