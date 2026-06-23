using System;
using UnityEngine;

namespace Locomotion.Camera
{
    [Serializable]
    public struct TransitionProfile
    {
        public float durationSec;
        public AnimationCurve ease;

        public static TransitionProfile Default(float seconds = 0.75f)
        {
            return new TransitionProfile
            {
                durationSec = seconds,
                ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
            };
        }
    }

    public enum CameraTransitionState
    {
        Idle,
        Blending,
        Hold,
    }

    /// <summary>Blends camera pose/FOV between focus modes.</summary>
    public sealed class CameraTransitionController : MonoBehaviour
    {
        public UnityEngine.Camera targetCamera;
        public CameraTransitionState state = CameraTransitionState.Idle;

        CameraRigPose _from;
        CameraRigPose _to;
        float _elapsed;
        float _duration = 0.75f;
        AnimationCurve _ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public event Action<CameraFocusMode> OnTransitionComplete;

        void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<UnityEngine.Camera>();
        }

        public void RequestTransition(CameraRigPose toPose, TransitionProfile profile)
        {
            if (targetCamera == null) return;

            _from = CameraRigPose.FromCamera(targetCamera, toPose.focusMode);
            if (state == CameraTransitionState.Blending)
            {
                float t = EvaluateEase(Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _duration)));
                _from.position = Vector3.Lerp(_from.position, _to.position, t);
                _from.rotation = Quaternion.Slerp(_from.rotation, _to.rotation, t);
                _from.fieldOfView = Mathf.Lerp(_from.fieldOfView, _to.fieldOfView, t);
            }

            _to = toPose;
            _duration = Mathf.Max(0.05f, profile.durationSec);
            _ease = profile.ease ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _elapsed = 0f;
            state = CameraTransitionState.Blending;
        }

        public void RequestTransition(CameraFocusMode to, CameraPathingContext ctx, HierarchicalCameraPathingSolver solver, TransitionProfile profile)
        {
            var pose = solver != null ? solver.ComputePose(to, ctx) : CameraRigPose.FromCamera(targetCamera, to);
            pose.focusMode = to;
            RequestTransition(pose, profile);
        }

        void Update()
        {
            if (state != CameraTransitionState.Blending || targetCamera == null)
                return;

            _elapsed += Time.deltaTime;
            float t = EvaluateEase(Mathf.Clamp01(_elapsed / _duration));
            var blended = new CameraRigPose
            {
                position = Vector3.Lerp(_from.position, _to.position, t),
                rotation = Quaternion.Slerp(_from.rotation, _to.rotation, t),
                fieldOfView = Mathf.Lerp(_from.fieldOfView, _to.fieldOfView, t),
                focusMode = _to.focusMode,
            };
            blended.ApplyTo(targetCamera);

            if (_elapsed >= _duration)
            {
                state = CameraTransitionState.Hold;
                _to.ApplyTo(targetCamera);
                OnTransitionComplete?.Invoke(_to.focusMode);
            }
        }

        float EvaluateEase(float linearT)
        {
            return _ease != null ? _ease.Evaluate(linearT) : linearT;
        }
    }
}
