using System;
using UnityEngine;

/// <summary>Eyes follow mouse when moving mouse, index fingertip when typing, webtop window centroids when on webtop.</summary>
[AddComponentMenu("Locomotion/Periphery/Eyes Gaze Controller")]
public sealed class EyesGazeController : MonoBehaviour
{
    public enum GazeMode { Idle, Mouse, TypingIndex, WebtopWindow }

    public Transform headBone;
    public Transform leftEye;
    public Transform rightEye;
    public Transform mouseAnchor;
    public FingerPositionCache fingerCache;
    public float gazeSpeed = 8f;
    public GazeMode mode = GazeMode.Idle;
    public Vector3 webtopWindowCentroid;

    Vector3 _target;

    public void SetMouseTarget(Vector3 world) { mode = GazeMode.Mouse; _target = world; }
    public void SetTypingIndexTarget(Vector3 world) { mode = GazeMode.TypingIndex; _target = world; }
    public void SetWebtopCentroid(Vector3 world) { mode = GazeMode.WebtopWindow; webtopWindowCentroid = world; _target = world; }

    /// <summary>Map CSS centroid (normalized 0-1) onto monitor plane for gaze.</summary>
    public void SetWebtopCentroidFromNormalizedUv(Transform monitor, float u, float v, float depth = 0.02f)
    {
        if (monitor == null) return;
        Vector3 local = new Vector3((u - 0.5f) * 0.6f, (0.5f - v) * 0.35f, depth);
        SetWebtopCentroid(monitor.TransformPoint(local));
    }

    void OnEnable()
    {
        // Soft-subscribe if Continuuuum telecom bridge is present (same AppDomain).
        var bridgeType = System.Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Continuuuum.Runtime")
                         ?? System.Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Assembly-CSharp");
        if (bridgeType == null) return;
        var evt = bridgeType.GetEvent("WindowCentroidsReceived");
        if (evt == null) return;
        _windowHandler = new Action<string>(OnWindowCentroidsJson);
        evt.AddEventHandler(null, _windowHandler);
    }

    void OnDisable()
    {
        if (_windowHandler == null) return;
        var bridgeType = System.Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Continuuuum.Runtime")
                         ?? System.Type.GetType("Continuuuum.Telecom.TelecomUnityBridge, Assembly-CSharp");
        bridgeType?.GetEvent("WindowCentroidsReceived")?.RemoveEventHandler(null, _windowHandler);
        _windowHandler = null;
    }

    Action<string> _windowHandler;

    void OnWindowCentroidsJson(string payload)
    {
        // Prefer first window center as UV proxy until monitor calibration exists.
        if (string.IsNullOrEmpty(payload)) return;
        mode = GazeMode.WebtopWindow;
    }

    void LateUpdate()
    {
        if (mode == GazeMode.Idle)
            return;
        Transform pivot = headBone != null ? headBone : transform;
        Vector3 dir = (_target - pivot.position);
        if (dir.sqrMagnitude < 1e-6f)
            return;
        Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, want, Time.deltaTime * gazeSpeed);
        AimEye(leftEye);
        AimEye(rightEye);
    }

    void AimEye(Transform eye)
    {
        if (eye == null) return;
        Vector3 dir = (_target - eye.position);
        if (dir.sqrMagnitude < 1e-6f) return;
        eye.rotation = Quaternion.Slerp(eye.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * gazeSpeed * 1.2f);
    }
}
