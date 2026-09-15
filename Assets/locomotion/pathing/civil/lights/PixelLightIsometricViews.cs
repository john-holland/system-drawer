using UnityEngine;

/// <summary>Fixed isometric rotations for the six cube faces (Frame/Shell PixelLight editor).</summary>
public static class PixelLightIsometricViews
{
    public const float TiltDeg = 35.264f;
    public const float YawDeg = 45f;

    public static Quaternion RotationFor(PixelLightDesignerView view)
    {
        switch (view)
        {
            case PixelLightDesignerView.Top:
                return Quaternion.Euler(90f - TiltDeg, YawDeg, 0f);
            case PixelLightDesignerView.Bottom:
                return Quaternion.Euler(-(90f - TiltDeg), YawDeg, 0f);
            case PixelLightDesignerView.Back:
                return Quaternion.Euler(TiltDeg, YawDeg + 180f, 0f);
            case PixelLightDesignerView.Left:
                return Quaternion.Euler(TiltDeg, YawDeg + 90f, 0f);
            case PixelLightDesignerView.Right:
                return Quaternion.Euler(TiltDeg, YawDeg - 90f, 0f);
            default:
                return Quaternion.Euler(TiltDeg, YawDeg, 0f);
        }
    }

    public static Vector3 ForwardFor(PixelLightDesignerView view) =>
        RotationFor(view) * Vector3.forward;
}
