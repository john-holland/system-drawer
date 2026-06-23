namespace Locomotion.Camera
{
    /// <summary>Steady-state camera focus modes for hierarchical pathing.</summary>
    public enum CameraFocusMode
    {
        ObjectFocus = 0,
        Character = 1,
        FirstPerson = 2,
        SceneFocus = 3,
        CentroidFocus = 4,
        MlActorVisionTrainingFocus = 5,
        Transition = 6,
    }
}
