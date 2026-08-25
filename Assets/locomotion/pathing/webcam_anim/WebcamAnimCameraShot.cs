using System;
using Locomotion.Camera;
using UnityEngine;

public enum WebcamAnimCameraTransition
{
    Cut = 0,
    Blend = 1,
    Crossfade = 2
}

/// <summary>Timed camera cue on a recording. <see cref="cameraIndex"/> indexes an optional scene camera list.</summary>
[Serializable]
public sealed class WebcamAnimCameraShot
{
    public double startMs;
    public CameraFocusMode focusMode = CameraFocusMode.Character;
    public WebcamAnimCameraTransition transition = WebcamAnimCameraTransition.Blend;
    public float transitionSec = 0.75f;
    public int cameraIndex;
}

/// <summary>Picks the active shot at a playhead and the blend/crossfade weight from that shot's start.</summary>
public static class WebcamAnimCameraDirector
{
    public static int ShotIndexAt(WebcamAnimCameraShot[] shots, double playheadMs)
    {
        if (shots == null || shots.Length == 0) return -1;
        int best = -1;
        double bestStart = double.NegativeInfinity;
        for (int i = 0; i < shots.Length; i++)
        {
            var s = shots[i];
            if (s == null) continue;
            if (s.startMs <= playheadMs && s.startMs >= bestStart)
            {
                best = i;
                bestStart = s.startMs;
            }
        }
        return best;
    }

    public static float TransitionT(WebcamAnimCameraShot shot, double playheadMs)
    {
        if (shot == null) return 1f;
        if (shot.transition == WebcamAnimCameraTransition.Cut || shot.transitionSec <= 1e-4f)
            return 1f;
        return Mathf.Clamp01((float)((playheadMs - shot.startMs) / (shot.transitionSec * 1000.0)));
    }

    public static TransitionProfile Profile(WebcamAnimCameraShot shot)
    {
        if (shot == null || shot.transition == WebcamAnimCameraTransition.Cut)
            return TransitionProfile.Default(0.05f);
        return TransitionProfile.Default(Mathf.Max(0.05f, shot.transitionSec));
    }

    public static UnityEngine.Camera ResolveCamera(WebcamAnimCameraShot shot, UnityEngine.Camera overlay, UnityEngine.Camera[] cameras)
    {
        if (shot == null) return overlay;
        if (cameras != null && shot.cameraIndex >= 0 && shot.cameraIndex < cameras.Length && cameras[shot.cameraIndex] != null)
            return cameras[shot.cameraIndex];
        return overlay;
    }

    public static void Apply(
        WebcamAnimCameraShot[] shots,
        UnityEngine.Camera[] cameras,
        UnityEngine.Camera overlay,
        CameraPathingRig rig,
        CameraTransitionController transition,
        double playheadMs,
        float deltaTime,
        ref int lastShotIndex)
    {
        int idx = ShotIndexAt(shots, playheadMs);
        if (idx < 0) return;
        var shot = shots[idx];
        var cam = ResolveCamera(shot, overlay, cameras);
        if (cam == null) return;

        if (idx != lastShotIndex)
        {
            lastShotIndex = idx;
            if (rig != null)
            {
                rig.rigCamera = cam;
                rig.SetFocusMode(shot.focusMode, shot.transition != WebcamAnimCameraTransition.Cut);
            }
            else if (transition != null)
            {
                transition.targetCamera = cam;
                var pose = CameraRigPose.FromCamera(cam, shot.focusMode);
                if (shot.transition == WebcamAnimCameraTransition.Cut)
                    pose.ApplyTo(cam);
                else
                    transition.RequestTransition(pose, Profile(shot));
            }
            else if (overlay != null && cam != overlay)
            {
                overlay.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
                overlay.fieldOfView = cam.fieldOfView;
            }
        }

        if (transition != null)
            transition.Tick(deltaTime);
    }
}
