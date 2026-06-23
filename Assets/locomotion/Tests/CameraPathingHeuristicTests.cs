using Locomotion.Camera;
using NUnit.Framework;

public class CameraPathingHeuristicTests
{
    [Test]
    public void PreferredMode_ReducesCost()
    {
        var hints = CameraPlannerHints.Default();
        hints.preferredMode = CameraFocusMode.Character;
        hints.memorabilityScore = 0.5f;
        float preferred = CameraPathingHeuristic.ModeCostDelta(CameraFocusMode.Character, in hints);
        float other = CameraPathingHeuristic.ModeCostDelta(CameraFocusMode.ObjectFocus, in hints);
        Assert.Less(preferred, other);
    }
}
