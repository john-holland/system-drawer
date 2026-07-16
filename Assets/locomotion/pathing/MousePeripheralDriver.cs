using UnityEngine;

/// <summary>Simple mouse peripheral aim driver for desk mouseAnchor.</summary>
[AddComponentMenu("Locomotion/Periphery/Mouse Peripheral Driver")]
public sealed class MousePeripheralDriver : MonoBehaviour
{
    public Transform mouseAnchor;
    public EyesGazeController gaze;
    public Vector3 worldPoint;

    public void MoveTo(Vector3 world)
    {
        worldPoint = world;
        if (mouseAnchor != null)
            mouseAnchor.position = world;
        gaze?.SetMouseTarget(world);
    }
}
