#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.EditorTools;
using Locomotion.Rig;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

/// <summary>Locomotion → Vehicle Video Steering. Project YOLO26 tracks onto a road-center spline and bake a drive BT.</summary>
public sealed class VehicleVideoSteeringWindow : EditorWindow
{
    WebcamAnimRecordingAsset _asset;
    VehicleRoadCenterSpline _spline;
    TravelAgent _agent;
    VehicleActor _vehicle;
    RagdollIKAnimationManager _ik;
    Transform _occupant;
    string _status = "";
    VehicleProjectionResult _lastProjection;
    CabinInstrumentHints _cabinHints;
    Vector2 _scroll;
    readonly WebcamAnimTimeScrubber _scrubber = new WebcamAnimTimeScrubber();
    WebcamAnimPreviewRig _previewRig;
    Camera _overlayCam;
    RenderTexture _videoRt;
    RenderTexture _overlayRt;
    VideoPlayer _videoPlayer;
    int _lastShot = -1;
    double _lastTickAt;

    [MenuItem("Locomotion/Vehicle Video Steering")]
    [MenuItem("Window/System Drawer/Animation/Vehicle Video Steering", false, 104)]
    public static void Open()
    {
        var w = GetWindow<VehicleVideoSteeringWindow>("Vehicle Video Steering");
        w.minSize = new Vector2(420, 640);
    }

    public static void OpenWith(WebcamAnimRecordingAsset asset)
    {
        Open();
        var w = GetWindow<VehicleVideoSteeringWindow>();
        w._asset = asset;
    }

    void OnEnable()
    {
        EnsureRts();
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopVideoPlayer();
        ReleaseRts();
    }

    void EnsureRts()
    {
        if (_videoRt == null)
            _videoRt = new RenderTexture(512, 288, 16) { name = "VehicleVideoPreviewRT" };
        if (_overlayRt == null)
            _overlayRt = new RenderTexture(512, 288, 16) { name = "VehicleOverlayRT" };
    }

    void ReleaseRts()
    {
        if (_videoRt != null)
        {
            _videoRt.Release();
            DestroyImmediate(_videoRt);
            _videoRt = null;
        }
        if (_overlayRt != null)
        {
            _overlayRt.Release();
            DestroyImmediate(_overlayRt);
            _overlayRt = null;
        }
    }

    void OnEditorUpdate()
    {
        EnsureRts();
        if (_asset != null)
            _scrubber.Bind(_asset);
        TickClock();
        RenderOverlay();
        if (_asset != null && _asset.syncVehicleRagdoll)
            SyncRagdoll();
        ApplyCameras();
        if (_scrubber.playing)
            Repaint();
    }

    void TickClock()
    {
        double now = EditorApplication.timeSinceStartup;
        if (_lastTickAt <= 0) _lastTickAt = now;
        double dt = now - _lastTickAt;
        _lastTickAt = now;
        if (_videoPlayer != null && _videoPlayer.isPlaying)
        {
            _scrubber.playing = true;
            _scrubber.Seek(_videoPlayer.time * 1000.0);
            return;
        }
        if (_scrubber.playing)
        {
            _scrubber.Tick(dt);
            if (_videoPlayer != null)
            {
                _videoPlayer.time = _scrubber.playheadMs / 1000.0;
                if (!_videoPlayer.isPlaying)
                    _videoPlayer.Play();
            }
        }
        else if (_videoPlayer != null)
        {
            if (_videoPlayer.isPlaying)
                _videoPlayer.Pause();
            _videoPlayer.time = _scrubber.playheadMs / 1000.0;
        }
    }

    Camera ResolveOverlay()
    {
        if (_previewRig != null && _previewRig.overlayCamera != null)
            return _previewRig.overlayCamera;
        return _overlayCam;
    }

    void RenderOverlay()
    {
        var cam = ResolveOverlay();
        if (cam == null || _overlayRt == null) return;
        var prev = cam.targetTexture;
        cam.targetTexture = _overlayRt;
        cam.Render();
        cam.targetTexture = prev;
    }

    void SyncRagdoll()
    {
        var vehicle = _previewRig != null && _previewRig.vehicle != null ? _previewRig.vehicle : _vehicle;
        var occupant = _previewRig != null && _previewRig.occupant != null ? _previewRig.occupant : _occupant;
        var ik = _previewRig != null && _previewRig.ik != null ? _previewRig.ik : _ik;
        BoneMap map = null;
        if (ik != null)
        {
            var actor = ik.GetRagdollActorTransform();
            if (actor != null)
                map = actor.GetComponent<BoneMap>() ?? actor.GetComponentInChildren<BoneMap>();
        }
        WebcamAnimVehicleRagdollSync.Apply(
            _asset, (float)_scrubber.playheadMs, vehicle, occupant, map, _lastProjection);
    }

    void ApplyCameras()
    {
        if (_asset == null) return;
        WebcamAnimCameraDirector.Apply(
            _asset.cameraShots,
            _previewRig != null ? _previewRig.CameraArray() : System.Array.Empty<Camera>(),
            ResolveOverlay(),
            _previewRig != null ? _previewRig.pathingRig : null,
            _previewRig != null ? _previewRig.transition : null,
            _scrubber.playheadMs,
            1f / 60f,
            ref _lastShot);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawPreview();
        _asset = (WebcamAnimRecordingAsset)EditorGUILayout.ObjectField(
            "Recording", _asset, typeof(WebcamAnimRecordingAsset), false);
        _spline = (VehicleRoadCenterSpline)EditorGUILayout.ObjectField(
            "Road center spline", _spline, typeof(VehicleRoadCenterSpline), true);
        _agent = (TravelAgent)EditorGUILayout.ObjectField("Travel Agent", _agent, typeof(TravelAgent), true);
        _vehicle = (VehicleActor)EditorGUILayout.ObjectField("Vehicle Actor", _vehicle, typeof(VehicleActor), true);
        _occupant = (Transform)EditorGUILayout.ObjectField("Occupant", _occupant, typeof(Transform), true);
        _ik = (RagdollIKAnimationManager)EditorGUILayout.ObjectField(
            "IK Animation Manager", _ik, typeof(RagdollIKAnimationManager), true);

        if (_asset != null)
        {
            Undo.RecordObject(_asset, "Vehicle video steering");
            if (_asset.kind != WebcamAnimKind.Vehicle)
                _asset.kind = WebcamAnimKind.Vehicle;
            _asset.cabinCamera = EditorGUILayout.Toggle("Cabin camera", _asset.cabinCamera);
            if (_asset.cabinCamera)
            {
                _asset.modelSpec = VehicleVideoSteeringIds.CabinCompositeSpec;
                _asset.inferShoulderShifts = EditorGUILayout.Toggle("Infer shoulder shifts", _asset.inferShoulderShifts);
                int poseIdx = (_asset.species ?? "").Length > 0 ? 1 : 0;
                poseIdx = EditorGUILayout.Popup("Cabin pose", poseIdx, new[] { "mediapipe_holistic@v1", "mocapanything@v2" });
                if (poseIdx == 1 && string.IsNullOrEmpty(_asset.species))
                    _asset.species = "Human";
                if (poseIdx == 0)
                    _asset.species = "";
                _asset.polarVelocityPath = EditorGUILayout.TextField("Polar velocity path", _asset.polarVelocityPath);
            }
            else
                _asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
            EditorGUILayout.LabelField("Model spec", _asset.modelSpec);
            _asset.facingYawDegrees = EditorGUILayout.Slider(
                _asset.cabinCamera ? "Facing yaw (window-forward)" : "Facing yaw",
                _asset.facingYawDegrees, 0f, 360f);
            _asset.vehicleTrackPath = EditorGUILayout.TextField("Vehicle track path", _asset.vehicleTrackPath);
            WebcamAnimTimelineFields.DrawInOutDuration(_asset, _videoPlayer);
            _scrubber.Bind(_asset);
            WebcamAnimTimeScrubberDrawer.Draw(_scrubber, CollectTicks());
            WebcamAnimTimeScrubberDrawer.DrawOverlaySettings(_asset);
            WebcamAnimTimeScrubberDrawer.DrawCameraShots(_asset);
        }

        EditorGUILayout.Space();
        bool cabin = _asset != null && _asset.cabinCamera;
        EditorGUI.BeginDisabledGroup(!cabin && !FeatureBudget.IsFeatureActive(FeatureBudgetIds.VehicleDetect));
        if (GUILayout.Button(cabin ? "Detect (cabin pose + polar)" : "Detect (local stub / load JSON)"))
            Detect();
        EditorGUI.EndDisabledGroup();
        if (!cabin && !FeatureBudget.IsFeatureActive(FeatureBudgetIds.VehicleDetect))
            EditorGUILayout.HelpBox("Feature budget: Vehicle YOLO Detect is off.", MessageType.Warning);

        if (GUILayout.Button(cabin ? "Project polar (± spline)" : "Project onto spline"))
            Project();
        if (GUILayout.Button("Bake TravelAgent plan + steering BT"))
            Bake();

        DrawDiamond();
        DrawCabinHints();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        EditorGUILayout.EndScrollView();
        if (_asset != null)
            EditorUtility.SetDirty(_asset);
    }

    void DrawPreview()
    {
        EnsureRts();
        EditorGUILayout.LabelField("Recording preview", EditorStyles.boldLabel);
        var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(200f));
        WebcamAnimTimeScrubberDrawer.DrawComposite(r, _overlayRt, _videoRt, _asset);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pick video file"))
            PickVideo();
        EditorGUILayout.EndHorizontal();
        _previewRig = (WebcamAnimPreviewRig)EditorGUILayout.ObjectField(
            "Preview rig (test scene)", _previewRig, typeof(WebcamAnimPreviewRig), true);
        _overlayCam = (Camera)EditorGUILayout.ObjectField(
            "Overlay camera", _overlayCam, typeof(Camera), true);
    }

    double[] CollectTicks()
    {
        var ticks = new List<double>();
        if (_asset != null && _asset.lastVehicleTrack?.segments != null)
        {
            for (int i = 0; i < _asset.lastVehicleTrack.segments.Length; i++)
                ticks.Add(_asset.lastVehicleTrack.segments[i].startMs);
        }
        if (_asset != null && _asset.cameraShots != null)
        {
            for (int i = 0; i < _asset.cameraShots.Length; i++)
            {
                if (_asset.cameraShots[i] != null)
                    ticks.Add(_asset.cameraShots[i].startMs);
            }
        }
        return ticks.Count > 0 ? ticks.ToArray() : null;
    }

    void PickVideo()
    {
        string p = EditorUtility.OpenFilePanel("Video", "", "mp4,mov,webm,avi");
        if (string.IsNullOrEmpty(p) || _asset == null)
            return;
        StopVideoPlayer();
        _asset.localClipPath = p;
        EditorUtility.SetDirty(_asset);
        EnsureRts();
        var host = EditorUtility.CreateGameObjectWithHideFlags("VehicleVideoPreviewPlayer", HideFlags.HideAndDontSave);
        _videoPlayer = host.AddComponent<VideoPlayer>();
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _videoRt;
        _videoPlayer.url = p;
        _videoPlayer.isLooping = true;
        _videoPlayer.Pause();
        WebcamAnimTimelineFields.BindVideoPrepare(_videoPlayer, _asset);
        _status = "Video: " + p;
    }

    void StopVideoPlayer()
    {
        if (_videoPlayer == null) return;
        var host = _videoPlayer.gameObject;
        _videoPlayer.Stop();
        DestroyImmediate(host);
        _videoPlayer = null;
    }

    void DrawDiamond()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Speed / steer envelope", EditorStyles.boldLabel);
        Rect r = GUILayoutUtility.GetRect(280, 220);
        float speed01 = 0f, steer01 = 0f, brake01 = 0f, yaw01 = 0f;
        if (_lastProjection != null && _lastProjection.segments.Count > 0)
        {
            float maxSpeed = 0.01f;
            float maxSteer = 0.01f;
            for (int i = 0; i < _lastProjection.segments.Count; i++)
            {
                maxSpeed = Mathf.Max(maxSpeed, _lastProjection.segments[i].speed);
                for (int j = 0; j < _lastProjection.segments[i].waypoints.Count; j++)
                    maxSteer = Mathf.Max(maxSteer, Mathf.Abs(_lastProjection.segments[i].waypoints[j].steerHintSigned01));
            }
            var last = _lastProjection.segments[_lastProjection.segments.Count - 1];
            speed01 = Mathf.Clamp01(last.speed / Mathf.Max(8f, maxSpeed));
            steer01 = Mathf.Clamp01(maxSteer);
            brake01 = last.speed < 0.5f ? 0.7f : 0.1f;
            yaw01 = Mathf.Repeat((_asset != null ? _asset.facingYawDegrees : 0f) / 360f, 1f);
        }
        if (_asset != null && _asset.cabinCamera && _asset.lastPolarVelocity != null && _asset.lastPolarVelocity.FrameCount > 0)
        {
            var f = _asset.lastPolarVelocity.FrameAt(_scrubber.playheadMs);
            speed01 = f != null ? Mathf.Clamp01(f.speedHint / 12f) : speed01;
            brake01 = _cabinHints.brake01 > 0.01f ? _cabinHints.brake01 : brake01;
            steer01 = Mathf.Max(steer01, Mathf.Abs(_cabinHints.steerSigned01));
        }
        PowerDiamondDrawer.DrawOverlay(
            r,
            new[] { "Speed", "Steer", "Brake", "Facing" },
            new[] { speed01, steer01, brake01, yaw01 },
            new[] { 1f, 1f, 1f, 1f },
            new[] { 0.8f, 0.8f, 0.5f, 0.5f },
            0f);
    }

    void DrawCabinHints()
    {
        if (_asset == null || !_asset.cabinCamera)
            return;
        _cabinHints = CabinPoseInstrumentSolver.Evaluate(
            _asset.lastTrack, (float)_scrubber.playheadMs, _asset.lastPolarVelocity, _asset.inferShoulderShifts);
        EditorGUILayout.HelpBox(
            $"Cabin residual={_cabinHints.residualLean:0.00} pedal={_cabinHints.pedal} foot={_cabinHints.footOverride} agree={_cabinHints.shoulderAgreesWithPolar}",
            _cabinHints.shoulderAgreesWithPolar ? MessageType.Info : MessageType.Warning);
    }

    void Detect()
    {
        if (_asset == null)
        {
            _status = "Assign a recording.";
            return;
        }
        if (_asset.cabinCamera)
        {
            _asset.modelSpec = VehicleVideoSteeringIds.CabinCompositeSpec;
            if (_asset.lastTrack == null || _asset.lastTrack.Count == 0)
                _asset.lastTrack = new LocalStubPoseAnimationDetector().Detect(_asset.localClipPath, "mediapipe_holistic@v1");
            _asset.lastPolarVelocity = CabinPolarVelocity.TryLoad(_asset.polarVelocityPath)
                ?? CabinPolarVelocity.TryLoad(_asset.localClipPath)
                ?? CabinPolarVelocity.Stub();
            _asset.lastVehicleTrack = VehicleTrack.TryLoad(_asset.vehicleTrackPath);
            _status = "Cabin polar frames=" + _asset.lastPolarVelocity.FrameCount +
                      " traffic=" + (_asset.lastVehicleTrack != null ? _asset.lastVehicleTrack.FrameCount : 0);
            _asset.ApplyLoadedTrackDuration();
            EditorUtility.SetDirty(_asset);
            return;
        }
        _asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
        var loaded = VehicleTrack.TryLoad(_asset.vehicleTrackPath);
        if (loaded == null && !string.IsNullOrEmpty(_asset.localClipPath))
            loaded = VehicleTrack.TryLoad(_asset.localClipPath);
        _asset.lastVehicleTrack = loaded ?? LocalStubVehicleTrackDetector.Detect(_asset.localClipPath, _asset.modelSpec);
        _status = loaded != null
            ? "Loaded vehicle track frames=" + _asset.lastVehicleTrack.FrameCount
            : "Local stub vehicle track (Continuuuum yolo26_vehicle@intel hop required for real detect).";
        _asset.ApplyLoadedTrackDuration();
        EditorUtility.SetDirty(_asset);
    }

    void Project()
    {
        if (_asset == null)
        {
            _status = "Assign a recording.";
            return;
        }
        if (_asset.cabinCamera)
        {
            if (_asset.lastPolarVelocity == null || _asset.lastPolarVelocity.FrameCount == 0)
                Detect();
            Vector3 origin = _vehicle != null ? _vehicle.transform.position : (_spline != null ? _spline.Sample(0f) : Vector3.zero);
            Vector3 fwd = _vehicle != null ? _vehicle.transform.forward : Vector3.forward;
            float yaw = _asset.facingYawDegrees * Mathf.Deg2Rad;
            fwd = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            _lastProjection = VehicleTrackProjector.ProjectPolar(_asset.lastPolarVelocity, origin, fwd, _spline);
            if (_agent != null)
                VehicleTrackProjector.ApplyToTravelAgent(_lastProjection, _agent, _vehicle);
            if (_previewRig != null)
                _previewRig.SetProjection(_lastProjection);
            _status = "Cabin polar waypoints=" + _lastProjection.waypoints.Count + " (YOLO not used as ego)";
            return;
        }
        if (_spline == null)
        {
            _status = "Assign recording and spline.";
            return;
        }
        if (_asset.lastVehicleTrack == null || _asset.lastVehicleTrack.FrameCount == 0)
            Detect();
        _lastProjection = VehicleTrackProjector.Project(_asset.lastVehicleTrack, _spline, _asset.facingYawDegrees, cabinCamera: false);
        if (_agent != null)
            VehicleTrackProjector.ApplyToTravelAgent(_lastProjection, _agent, _vehicle);
        if (_previewRig != null)
            _previewRig.SetProjection(_lastProjection);
        _status = "Projected waypoints=" + _lastProjection.waypoints.Count +
                  " segments=" + _lastProjection.segments.Count +
                  " trackId=" + _lastProjection.subjectTrackId;
    }

    void Bake()
    {
        if (_lastProjection == null || _lastProjection.waypoints.Count == 0)
            Project();
        if (_lastProjection == null)
        {
            _status = "Projection empty.";
            return;
        }
        Transform parent = _vehicle != null ? _vehicle.transform : (_agent != null ? _agent.transform : null);
        if (parent == null && _spline != null)
            parent = _spline.transform;
        if (parent == null)
        {
            _status = "Assign vehicle or travel agent as BT parent.";
            return;
        }
        var baked = VehicleSteeringBtBaker.Bake(
            parent,
            _lastProjection,
            _vehicle,
            _agent,
            _asset != null && _asset.cabinCamera ? _asset.lastPolarVelocity : null,
            _occupant);
        if (_ik != null && _asset != null && _asset.lastTrack != null && _asset.lastTrack.Count > 0)
        {
            var map = parent.GetComponentInChildren<BoneMap>();
            PoseTrackClipBaker.BakeAndAddSet(_ik, _asset.lastTrack, map, parent, "Drive", syncSelection: false);
        }
        _status = "Baked steering BT seed=" + (baked.seed != null) + " driveWaypoints=" + baked.driveWaypointCount;
    }
}
#endif
