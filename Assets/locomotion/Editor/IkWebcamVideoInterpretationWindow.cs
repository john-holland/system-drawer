#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using Locomotion.EditorTools;
using Locomotion.Rig;

/// <summary>
/// Window → System Drawer → Animation → IK Webcam Video Interpretation
/// Dual viewport (actor | webcam/video), bone grabbers, granularity timeline, local vs Continuuuum upload.
/// </summary>
public sealed class IkWebcamVideoInterpretationWindow : EditorWindow
{
    const string AssetFolder = "Assets/WebcamAnim";

    Vector2 _leftScroll;
    Vector2 _inspScroll;
    WebcamAnimRecordingAsset _asset;
    readonly List<WebcamAnimRecordingAsset> _assets = new List<WebcamAnimRecordingAsset>();
    RagdollIKAnimationManager _ikManager;
    GameObject _previewRoot;
    Camera _actorCam;
    Camera _videoCam;
    RenderTexture _actorRt;
    RenderTexture _videoRt;
    WebCamTexture _webcam;
    VideoPlayer _videoPlayer;
    bool _useLocalDetect = true;
    string _status = "";
    readonly WebcamAnimTimeScrubber _scrubber = new WebcamAnimTimeScrubber();
    bool _webcamOn;
    Rect _videoBox;
    bool _liveMirror = true;
    bool _editModeContact = true;
    bool _activateTrainingObjects = true;
    PhysicsIKTrainingRunAsset _ikTrainRun;
    SceneAsset _measurementScene;
    bool _triedBoneMap;
    BoneMap _liveMap;
    UnityWebRequest _livePoseReq;
    double _lastLivePoseAt;
    double _lastContactAt;
    Texture2D _livePoseReadTex;
    readonly InteractedObjectCheckpoint _checkpoint = new InteractedObjectCheckpoint();
    WebcamAnimPreviewRig _previewRig;
    Camera _overlayCam;
    RenderTexture _overlayRt;
    VehicleActor _syncVehicle;
    Transform _syncOccupant;
    int _lastShot = -1;
    VehicleProjectionResult _syncProjection;
    double _lastTickAt;

    [MenuItem("Window/System Drawer/Animation/IK Webcam Video Interpretation", false, 103)]
    public static void ShowWindow()
    {
        var w = GetWindow<IkWebcamVideoInterpretationWindow>("IK Webcam Video");
        w.minSize = new Vector2(960, 640);
    }

    public static void OpenForKind(WebcamAnimKind kind)
    {
        ShowWindow();
        var w = GetWindow<IkWebcamVideoInterpretationWindow>();
        if (w._asset != null)
            w._asset.kind = kind;
    }

    void OnEnable()
    {
        EnsureRts();
        RefreshList();
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        AbortLivePose();
        StopWebcam();
        StopVideoPlayer();
        ReleaseRts();
        if (_livePoseReadTex != null)
        {
            DestroyImmediate(_livePoseReadTex);
            _livePoseReadTex = null;
        }
    }

    void OnEditorUpdate()
    {
        EnsureRts();
        if (_asset != null)
            _scrubber.Bind(_asset);
        TickPlaybackClock();
        bool mediaPlaying = (_webcamOn && _webcam != null && _webcam.isPlaying)
            || (_videoPlayer != null && _videoPlayer.isPlaying)
            || _scrubber.playing;
        if (_webcamOn && _webcam != null && _webcam.isPlaying)
        {
            Graphics.Blit(_webcam, _videoRt);
            Repaint();
        }
        if (_videoPlayer != null && (_videoPlayer.isPlaying || _scrubber.playing))
            Repaint();
        RenderOverlayCamera();
        if (mediaPlaying && !_useLocalDetect && _webcamOn && _webcam != null && _webcam.isPlaying)
            HopLivePose();
        if (_liveMirror && mediaPlaying)
            ApplyLiveMirror();
        if (_asset != null && _asset.syncVehicleRagdoll)
            ApplyVehicleRagdollSync();
        ApplyCameraDirector();
        if (_editModeContact && (_liveMirror || _activateTrainingObjects))
            TickEditModeContact();
        if (_scrubber.playing)
            Repaint();
    }

    void TickPlaybackClock()
    {
        double now = EditorApplication.timeSinceStartup;
        if (_lastTickAt <= 0)
            _lastTickAt = now;
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

    void RenderOverlayCamera()
    {
        var cam = ResolveOverlayCamera();
        if (cam == null || _overlayRt == null) return;
        var prev = cam.targetTexture;
        cam.targetTexture = _overlayRt;
        cam.Render();
        cam.targetTexture = prev;
    }

    Camera ResolveOverlayCamera()
    {
        if (_previewRig != null && _previewRig.overlayCamera != null)
            return _previewRig.overlayCamera;
        return _overlayCam;
    }

    void ApplyVehicleRagdollSync()
    {
        var vehicle = _previewRig != null && _previewRig.vehicle != null ? _previewRig.vehicle : _syncVehicle;
        var occupant = _previewRig != null && _previewRig.occupant != null ? _previewRig.occupant : _syncOccupant;
        var ik = _previewRig != null && _previewRig.ik != null ? _previewRig.ik : _ikManager;
        BoneMap map = _previewRig != null ? _previewRig.ResolveMap() : ResolveLiveBoneMap();
        WebcamAnimVehicleRagdollSync.Apply(
            _asset, (float)_scrubber.playheadMs, vehicle, occupant, map, _syncProjection);
    }

    void ApplyCameraDirector()
    {
        if (_asset == null) return;
        var rig = _previewRig;
        Camera[] cams = rig != null ? rig.CameraArray() : System.Array.Empty<Camera>();
        WebcamAnimCameraDirector.Apply(
            _asset.cameraShots,
            cams,
            ResolveOverlayCamera(),
            rig != null ? rig.pathingRig : null,
            rig != null ? rig.transition : null,
            _scrubber.playheadMs,
            1f / 60f,
            ref _lastShot);
    }

    bool ApplyLiveMirror()
    {
        if (_ikManager == null)
            return false;
        var map = ResolveLiveBoneMap();
        if (map == null || _asset == null)
            return false;
        var track = _asset.lastTrack;
        if (track == null || track.Count == 0)
            return false;
        float timeMs = (float)_scrubber.playheadMs;
        if (_videoPlayer != null && _videoPlayer.isPlaying && track.Count > 8)
            timeMs = (float)(_videoPlayer.time * 1000.0);
        PoseTrackPlayer.Apply(track, map, timeMs);
        var ragdoll = _ikManager.ragdollSystem
            ?? _ikManager.GetComponent<RagdollSystem>()
            ?? _ikManager.GetComponentInChildren<RagdollSystem>();
        RagdollPoseUtility.ZeroRagdollVelocities(ragdoll);
        SceneView.RepaintAll();
        return true;
    }

    BoneMap ResolveLiveBoneMap()
    {
        if (_liveMap != null)
            return _liveMap;
        if (_ikManager == null)
            return null;
        Transform actor = _ikManager.GetRagdollActorTransform();
        if (actor == null)
            return null;
        _liveMap = actor.GetComponent<BoneMap>();
        if (_liveMap == null)
            _liveMap = actor.GetComponentInChildren<BoneMap>();
        if (_liveMap == null && !_triedBoneMap)
        {
            _triedBoneMap = true;
            _liveMap = RagdollAutoWire.EnsureBoneMap(actor.gameObject);
            _status = "BoneMap missing on ragdoll; EnsureBoneMap ran once.";
        }
        return _liveMap;
    }

    void HopLivePose()
    {
        if (_livePoseReq != null)
            return;
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastLivePoseAt < 0.1)
            return;
        _lastLivePoseAt = now;
        if (_videoRt == null)
            return;
        var prev = RenderTexture.active;
        RenderTexture.active = _videoRt;
        int w = Mathf.Max(2, _videoRt.width);
        int h = Mathf.Max(2, _videoRt.height);
        if (_livePoseReadTex == null || _livePoseReadTex.width != w || _livePoseReadTex.height != h)
        {
            if (_livePoseReadTex != null)
                DestroyImmediate(_livePoseReadTex);
            _livePoseReadTex = new Texture2D(w, h, TextureFormat.RGB24, false);
        }
        _livePoseReadTex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        _livePoseReadTex.Apply(false, false);
        RenderTexture.active = prev;
        byte[] jpg = _livePoseReadTex.EncodeToJPG(70);
        if (jpg == null || jpg.Length == 0)
            return;
        string url = ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/') + "/api/webcam-animations/live-pose";
        var form = new WWWForm();
        form.AddBinaryData("file", jpg, "frame.jpg", "image/jpeg");
        form.AddField("model_spec", string.IsNullOrEmpty(_asset?.modelSpec) ? "mediapipe_holistic@v1" : _asset.modelSpec);
        form.AddField("tMs", ((int)(now * 1000.0)).ToString());
        _livePoseReq = UnityWebRequest.Post(url, form);
        var op = _livePoseReq.SendWebRequest();
        op.completed += _ => OnLivePoseDone();
    }

    void OnLivePoseDone()
    {
        var req = _livePoseReq;
        _livePoseReq = null;
        if (req == null)
            return;
        try
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (req.responseCode == 503)
                    _status = "Live MediaPipe needs Continuuuum (uncheck Local detect). " + req.downloadHandler?.text;
                return;
            }
            string json = req.downloadHandler != null ? req.downloadHandler.text : "";
            var hop = PoseTrackLiveDto.ToTrack(json);
            if (hop == null || hop.Count == 0 || _asset == null)
                return;
            if (_asset.lastTrack == null)
                _asset.lastTrack = new PoseTrack { modelSpec = hop.modelSpec };
            _asset.lastTrack.AppendSamples(hop.samples, 2000f);
        }
        finally
        {
            req.Dispose();
        }
    }

    void AbortLivePose()
    {
        if (_livePoseReq == null)
            return;
        _livePoseReq.Abort();
        _livePoseReq.Dispose();
        _livePoseReq = null;
    }

    void TickEditModeContact()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastContactAt < 0.1)
            return;
        _lastContactAt = now;
        if (_ikManager == null)
            return;
        var ragdoll = _ikManager.ragdollSystem
            ?? _ikManager.GetComponent<RagdollSystem>()
            ?? _ikManager.GetComponentInChildren<RagdollSystem>();
        if (ragdoll == null)
            return;
        var objects = _ikTrainRun != null
            ? _ikTrainRun.ResolveMeasurementObjects()
            : new List<GameObject>();
        var result = GoodSectionContactActivation.Tick(ragdoll, objects, _checkpoint);
        if (result.contacts != null)
        {
            for (int i = 0; i < result.contacts.Count; i++)
                GoodSectionContactActivation.CollectCascadeFromMoved(result.contacts[i], objects, _checkpoint);
        }
        var drawer = ragdoll.GetComponent<SystemDrawerAnimator>()
                     ?? ragdoll.GetComponentInParent<SystemDrawerAnimator>()
                     ?? ragdoll.GetComponentInChildren<SystemDrawerAnimator>();
        if (drawer != null)
            drawer.TickLayersFromEditor();
    }

    void EnsureRts()
    {
        if (_actorRt == null)
            _actorRt = new RenderTexture(512, 288, 16) { name = "IkWebcamActorRT" };
        if (_videoRt == null)
            _videoRt = new RenderTexture(512, 288, 16) { name = "IkWebcamVideoRT" };
        if (_overlayRt == null)
            _overlayRt = new RenderTexture(512, 288, 16) { name = "IkWebcamOverlayRT" };
    }

    void ReleaseRts()
    {
        if (_actorRt != null)
        {
            _actorRt.Release();
            DestroyImmediate(_actorRt);
            _actorRt = null;
        }
        if (_overlayRt != null)
        {
            _overlayRt.Release();
            DestroyImmediate(_overlayRt);
            _overlayRt = null;
        }
    }

    void RefreshList()
    {
        _assets.Clear();
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets", "WebcamAnim");
        foreach (var guid in AssetDatabase.FindAssets("t:WebcamAnimRecordingAsset", new[] { AssetFolder, "Assets" }))
        {
            var a = AssetDatabase.LoadAssetAtPath<WebcamAnimRecordingAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null)
                _assets.Add(a);
        }
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLeft();
        EditorGUILayout.BeginVertical();
        DrawViewports();
        DrawTimeline();
        DrawInspector();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    void DrawLeft()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(220));
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        if (GUILayout.Button("Refresh"))
            RefreshList();
        if (GUILayout.Button("+ New recording"))
        {
            _asset = CreateRecording();
            RefreshList();
        }
        foreach (var a in _assets)
        {
            if (a == null)
                continue;
            bool on = _asset == a;
            if (GUILayout.Toggle(on, a.displayName, "Button") && !on)
                _asset = a;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawViewports()
    {
        EnsureRts();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Actor", EditorStyles.miniLabel);
        GUILayout.Box(_actorRt, GUILayout.Width(position.width * 0.38f), GUILayout.Height(220));
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Webcam / video", EditorStyles.miniLabel);
        GUILayout.Box(_videoRt, GUILayout.Width(position.width * 0.38f), GUILayout.Height(220));
        _videoBox = GUILayoutUtility.GetLastRect();
        DrawVehicleOverlay(_videoBox);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Recording preview (scene + video overlay)", EditorStyles.miniLabel);
        var composite = GUILayoutUtility.GetRect(position.width * 0.78f, 220f);
        WebcamAnimTimeScrubberDrawer.DrawComposite(composite, _overlayRt, _videoRt, _asset);
        DrawVehicleOverlay(WebcamAnimPreviewOverlay.AlignedVideoRect(
            composite,
            _asset != null ? _asset.previewVideoScale : 1f,
            _asset != null ? _asset.previewVideoOffset01 : Vector2.zero));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(_webcamOn ? "Stop webcam" : "Start webcam"))
            ToggleWebcam();
        if (GUILayout.Button("Pick video file"))
            PickVideo();
        EditorGUILayout.EndHorizontal();
        _previewRig = (WebcamAnimPreviewRig)EditorGUILayout.ObjectField(
            "Preview rig (test scene)", _previewRig, typeof(WebcamAnimPreviewRig), true);
        _overlayCam = (Camera)EditorGUILayout.ObjectField(
            "Overlay camera", _overlayCam, typeof(Camera), true);
        _syncVehicle = (VehicleActor)EditorGUILayout.ObjectField(
            "Sync vehicle", _syncVehicle, typeof(VehicleActor), true);
        _syncOccupant = (Transform)EditorGUILayout.ObjectField(
            "Sync occupant", _syncOccupant, typeof(Transform), true);
    }

    void DrawTimeline()
    {
        if (_asset == null)
            return;
        EditorGUILayout.Space();
        _asset.kind = (WebcamAnimKind)EditorGUILayout.EnumPopup("Kind", _asset.kind);
        if (_asset.kind == WebcamAnimKind.Vehicle)
        {
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
            }
            else
                _asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
            _asset.facingYawDegrees = EditorGUILayout.Slider(
                _asset.cabinCamera ? "Facing yaw (window-forward)" : "Facing yaw",
                _asset.facingYawDegrees, 0f, 360f);
            DrawCabinShoulderTick();
        }
        int g = (int)_asset.granularity;
        g = EditorGUILayout.IntSlider("Granularity", g, 0, 6);
        _asset.granularity = WebcamAnimTimelineGranularityUtil.FromSlider(g);
        EditorGUILayout.LabelField("Tick", WebcamAnimTimelineGranularityUtil.TickMs(_asset.granularity) + " ms (" + _asset.granularity + ")");
        EditorGUILayout.BeginHorizontal();
        _asset.startMs = EditorGUILayout.DoubleField("In (ms)", _asset.startMs);
        _asset.endMs = EditorGUILayout.DoubleField("Out (ms)", _asset.endMs);
        EditorGUILayout.EndHorizontal();
        _asset.startMs = WebcamAnimTimelineGranularityUtil.SnapMs(_asset.startMs, _asset.granularity);
        _asset.endMs = WebcamAnimTimelineGranularityUtil.SnapMs(_asset.endMs, _asset.granularity);
        _scrubber.Bind(_asset);
        WebcamAnimTimeScrubberDrawer.Draw(_scrubber, CollectTicks());
        WebcamAnimTimeScrubberDrawer.DrawOverlaySettings(_asset);
        WebcamAnimTimeScrubberDrawer.DrawCameraShots(_asset);
    }

    double[] CollectTicks()
    {
        var ticks = new List<double>();
        if (_asset.lastVehicleTrack?.segments != null)
        {
            for (int i = 0; i < _asset.lastVehicleTrack.segments.Length; i++)
                ticks.Add(_asset.lastVehicleTrack.segments[i].startMs);
        }
        if (_asset.cameraShots != null)
        {
            for (int i = 0; i < _asset.cameraShots.Length; i++)
            {
                if (_asset.cameraShots[i] != null)
                    ticks.Add(_asset.cameraShots[i].startMs);
            }
        }
        return ticks.Count > 0 ? ticks.ToArray() : null;
    }

    void DrawCabinShoulderTick()
    {
        if (_asset == null || !_asset.cabinCamera)
            return;
        var pose = _asset.lastTrack;
        var polar = _asset.lastPolarVelocity;
        if (pose == null && polar == null)
            return;
        var hints = CabinPoseInstrumentSolver.Evaluate(
            pose, (float)_scrubber.playheadMs, polar, _asset.inferShoulderShifts);
        string agree = hints.footOverride
            ? "Foot override (pedal)"
            : (hints.shoulderAgreesWithPolar ? "Shoulder agrees with cabin speed" : "Shoulder disagrees with polar accel");
        EditorGUILayout.HelpBox(
            $"polar v={hints.polarAccel:0.00} lean={hints.shoulderLeanAp:0.00} residual={hints.residualLean:0.00} steer={hints.steerSigned01:0.00} — {agree}",
            hints.shoulderAgreesWithPolar ? MessageType.Info : MessageType.Warning);
    }

    void DrawInspector()
    {
        _inspScroll = EditorGUILayout.BeginScrollView(_inspScroll, GUILayout.MaxHeight(280));
        _asset = (WebcamAnimRecordingAsset)EditorGUILayout.ObjectField(
            "Recording", _asset, typeof(WebcamAnimRecordingAsset), false);
        if (_asset == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }
        Undo.RecordObject(_asset, "Webcam recording");
        _asset.displayName = EditorGUILayout.TextField("Name", _asset.displayName);
        _asset.actorPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Actor prefab", _asset.actorPrefab, typeof(GameObject), false);
        EditorGUI.BeginChangeCheck();
        _ikManager = (RagdollIKAnimationManager)EditorGUILayout.ObjectField(
            "IK Animation Manager", _ikManager, typeof(RagdollIKAnimationManager), true);
        if (EditorGUI.EndChangeCheck())
        {
            _liveMap = null;
            _triedBoneMap = false;
        }
        if (_ikManager == null)
            EditorGUILayout.HelpBox("Assign an IK Animation Manager to live-mirror PoseTrack onto the scene ragdoll.", MessageType.Warning);
        else if (ResolveLiveBoneMap() == null)
            EditorGUILayout.HelpBox("BoneMap missing on the ragdoll actor.", MessageType.Warning);
        _liveMirror = EditorGUILayout.Toggle("Live mirror", _liveMirror);
        _editModeContact = EditorGUILayout.Toggle("Edit-mode contact activation", _editModeContact);
        DrawIkTrainingFoldout();
        DrawAnimationIndexPopup();
        if (_asset.kind == WebcamAnimKind.Vehicle)
        {
            if (_asset.cabinCamera)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Model spec", VehicleVideoSteeringIds.CabinCompositeSpec);
                EditorGUI.EndDisabledGroup();
                _asset.modelSpec = VehicleVideoSteeringIds.CabinCompositeSpec;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Model spec", VehicleVideoSteeringIds.Yolo26IntelSpec);
                EditorGUI.EndDisabledGroup();
                _asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
            }
        }
        else
            _asset.modelSpec = EditorGUILayout.TextField("Model spec", _asset.modelSpec);
        _asset.subsectionId = EditorGUILayout.TextField("Subsection (reedit)", _asset.subsectionId);
        _asset.targetHint = EditorGUILayout.TextField("Target hint", _asset.targetHint);
        _asset.localClipPath = EditorGUILayout.TextField("Local clip path", _asset.localClipPath);
        if (_asset.kind == WebcamAnimKind.Vehicle && _asset.cabinCamera)
        {
            _asset.poseTrackPath = EditorGUILayout.TextField("Pose track path", _asset.poseTrackPath);
            _asset.polarVelocityPath = EditorGUILayout.TextField("Polar velocity path", _asset.polarVelocityPath);
        }
        _asset.libraryDocId = EditorGUILayout.TextField("USC library doc id", _asset.libraryDocId);
        _useLocalDetect = EditorGUILayout.Toggle("Local detect (fast)", _useLocalDetect);
        if (!_useLocalDetect)
            EditorGUILayout.HelpBox("Live MediaPipe hops need Continuuuum. Uncheck Local detect while the webcam plays.", MessageType.Info);
        else
            EditorGUILayout.HelpBox("Local detect skips live-pose POST; Apply still uses lastTrack if Detect already filled it.", MessageType.None);
        EditorGUI.BeginDisabledGroup(!_checkpoint.CanReset);
        if (GUILayout.Button("Reset to state"))
            _checkpoint.Reset();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Detect"))
            RunDetect();
        if (GUILayout.Button("Upload to Continuuuum"))
            UploadToContinuuuum();
        if (GUILayout.Button("Spawn bone grabber"))
            SpawnGrabber();
        if (_asset.kind == WebcamAnimKind.Vehicle && GUILayout.Button("Vehicle Video Steering"))
            VehicleVideoSteeringWindow.OpenWith(_asset);
        EditorGUILayout.EndHorizontal();
        EditorUtility.SetDirty(_asset);
        EditorGUILayout.EndScrollView();
    }

    void DrawAnimationIndexPopup()
    {
        var labels = new List<string> { "(none)" };
        var values = new List<int> { -1 };
        if (_ikManager != null && _ikManager.availableAnimations != null)
        {
            for (int i = 0; i < _ikManager.availableAnimations.Count; i++)
            {
                var set = _ikManager.availableAnimations[i];
                string n = set != null && !string.IsNullOrEmpty(set.displayName) ? set.displayName : "set " + i;
                labels.Add(i + ": " + n);
                values.Add(i);
            }
        }
        int cur = values.IndexOf(_asset.animationListIndex);
        if (cur < 0) cur = 0;
        int next = EditorGUILayout.Popup("Animation list index", cur, labels.ToArray());
        _asset.animationListIndex = values[next];
    }

    void DrawIkTrainingFoldout()
    {
        EditorGUILayout.LabelField("Optional IK training", EditorStyles.boldLabel);
        _ikTrainRun = (PhysicsIKTrainingRunAsset)EditorGUILayout.ObjectField(
            "Run asset", _ikTrainRun, typeof(PhysicsIKTrainingRunAsset), false);
        EditorGUI.BeginChangeCheck();
        _measurementScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Measurement scene", _measurementScene, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck() && _ikTrainRun != null)
        {
            _ikTrainRun.measurementScenePath = _measurementScene != null
                ? AssetDatabase.GetAssetPath(_measurementScene)
                : "";
            EditorUtility.SetDirty(_ikTrainRun);
        }
        if (_ikTrainRun != null && _measurementScene == null && !string.IsNullOrEmpty(_ikTrainRun.measurementScenePath))
            _measurementScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_ikTrainRun.measurementScenePath);
        bool hasMeasure = _ikTrainRun != null && (
            !string.IsNullOrEmpty(_ikTrainRun.measurementScenePath)
            || (_ikTrainRun.measurementObjectWeights != null && _ikTrainRun.measurementObjectWeights.Count > 0));
        if (hasMeasure)
            _activateTrainingObjects = true;
        _activateTrainingObjects = EditorGUILayout.Toggle("Activate training objects in editor", _activateTrainingObjects);
        if (_ikTrainRun != null)
        {
            _ikTrainRun.activateTrainingObjectsInEditor = _activateTrainingObjects;
            if (_measurementScene != null)
                _ikTrainRun.measurementScenePath = AssetDatabase.GetAssetPath(_measurementScene);
        }
        EditorGUI.BeginDisabledGroup(_ikTrainRun == null || _ikManager == null);
        if (GUILayout.Button("Start IK training from current pose"))
        {
            if (_ikTrainRun != null)
            {
                _ikTrainRun.initialPoseMode = IKTrainingInitialPoseMode.Current;
                EditorUtility.SetDirty(_ikTrainRun);
            }
            var solver = _ikManager != null ? _ikManager.GetComponent<PhysicsCardSolver>() : null;
            if (solver == null && _ikManager != null)
                solver = _ikManager.GetComponentInChildren<PhysicsCardSolver>();
            PhysicsIKTrainingWindow.OpenAndTrainFromCurrentPose(_ikTrainRun, solver);
        }
        EditorGUI.EndDisabledGroup();
    }

    WebcamAnimRecordingAsset CreateRecording()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets", "WebcamAnim");
        var a = CreateInstance<WebcamAnimRecordingAsset>();
        a.displayName = "Recording";
        a.recordingId = "wrec_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        string path = AssetDatabase.GenerateUniqueAssetPath(AssetFolder + "/WebcamAnimRecording.asset");
        AssetDatabase.CreateAsset(a, path);
        AssetDatabase.SaveAssets();
        return a;
    }

    void ToggleWebcam()
    {
        if (_webcamOn)
        {
            StopWebcam();
            return;
        }
        EnsureRts();
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            _status = "No webcam devices.";
            return;
        }
        _webcam = new WebCamTexture(devices[0].name, 640, 360);
        _webcam.Play();
        _webcamOn = true;
        _status = "Webcam: " + devices[0].name;
    }

    void StopWebcam()
    {
        if (_webcam != null)
        {
            _webcam.Stop();
            DestroyImmediate(_webcam);
            _webcam = null;
        }
        _webcamOn = false;
    }

    void StopVideoPlayer()
    {
        if (_videoPlayer == null)
            return;
        var host = _videoPlayer.gameObject;
        _videoPlayer.Stop();
        DestroyImmediate(host);
        _videoPlayer = null;
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
        var host = EditorUtility.CreateGameObjectWithHideFlags("IkWebcamVideoPlayer", HideFlags.HideAndDontSave);
        _videoPlayer = host.AddComponent<VideoPlayer>();
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _videoRt;
        _videoPlayer.url = p;
        _videoPlayer.isLooping = true;
        _videoPlayer.Pause();
        _scrubber.Bind(_asset);
        _scrubber.Stop();
        _status = "Video: " + p;
    }

    void DrawVehicleOverlay(Rect box)
    {
        if (_asset == null || _asset.kind != WebcamAnimKind.Vehicle || _asset.lastVehicleTrack == null)
            return;
        var frames = _asset.lastVehicleTrack.frames;
        if (frames == null || frames.Length == 0 || Event.current.type != EventType.Repaint)
            return;
        VehicleTrackFrame nearest = null;
        double best = double.MaxValue;
        for (int i = 0; i < frames.Length; i++)
        {
            double d = System.Math.Abs(frames[i].tMs - _scrubber.playheadMs);
            if (d < best)
            {
                best = d;
                nearest = frames[i];
            }
        }
        if (nearest?.bbox == null)
            return;
        float x = box.x + nearest.bbox.x1 * box.width;
        float y = box.y + nearest.bbox.y1 * box.height;
        float w = nearest.bbox.Width * box.width;
        float h = nearest.bbox.Height * box.height;
        Handles.BeginGUI();
        Handles.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Handles.DrawSolidRectangleWithOutline(new Rect(x, y, w, h), new Color(0.2f, 1f, 0.4f, 0.12f), Color.green);
        Handles.EndGUI();
    }

    void RunDetect()
    {
        if (_asset == null)
            return;
        if (_asset.kind == WebcamAnimKind.Vehicle)
        {
            if (_asset.cabinCamera)
            {
                _asset.modelSpec = VehicleVideoSteeringIds.CabinCompositeSpec;
                if (_useLocalDetect)
                {
                    _asset.lastTrack = new LocalStubPoseAnimationDetector().Detect(_asset.localClipPath, "mediapipe_holistic@v1");
                    _asset.lastPolarVelocity = CabinPolarVelocity.TryLoad(_asset.polarVelocityPath) ?? CabinPolarVelocity.Stub();
                    _asset.lastVehicleTrack = VehicleTrack.TryLoad(_asset.vehicleTrackPath);
                }
                else
                {
                    _asset.lastTrack = new ContinuuuumRemotePoseAnimationDetector { ApiBaseUrl = ContinuuuumApiConfig.GetApiBaseUrl() }
                        .Detect(_asset.poseTrackPath, "mediapipe_holistic@v1");
                    if (_asset.lastTrack == null || _asset.lastTrack.Count == 0)
                        _asset.lastTrack = ContinuuuumRemotePoseAnimationDetector.TryLoadJson(_asset.localClipPath);
                    _asset.lastPolarVelocity = CabinPolarVelocity.TryLoad(_asset.polarVelocityPath)
                        ?? CabinPolarVelocity.TryLoad(_asset.localClipPath);
                    _asset.lastVehicleTrack = VehicleTrack.TryLoad(_asset.vehicleTrackPath);
                }
                EditorUtility.SetDirty(_asset);
                _status = "Cabin pose samples=" + (_asset.lastTrack != null ? _asset.lastTrack.Count : 0) +
                          " polar=" + (_asset.lastPolarVelocity != null ? _asset.lastPolarVelocity.FrameCount : 0) +
                          " traffic=" + (_asset.lastVehicleTrack != null ? _asset.lastVehicleTrack.FrameCount : 0);
                return;
            }
            _asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
            if (!FeatureBudget.IsFeatureActive(FeatureBudgetIds.VehicleDetect))
            {
                _status = "Vehicle detect is feature-gated off.";
                return;
            }
            var loaded = VehicleTrack.TryLoad(_asset.vehicleTrackPath);
            if (loaded == null)
                loaded = VehicleTrack.TryLoad(_asset.localClipPath);
            if (_useLocalDetect)
                _asset.lastVehicleTrack = loaded ?? LocalStubVehicleTrackDetector.Detect(_asset.localClipPath, _asset.modelSpec);
            else
                _asset.lastVehicleTrack = loaded;
            if (_asset.lastVehicleTrack == null)
            {
                _status = "Vehicle detect requires yolo26_vehicle@intel hop output (vehicleTrackPath). No MediaPipe fallback.";
                return;
            }
            EditorUtility.SetDirty(_asset);
            _status = "Vehicle track frames=" + _asset.lastVehicleTrack.FrameCount +
                      " segments=" + (_asset.lastVehicleTrack.segments != null ? _asset.lastVehicleTrack.segments.Length : 0);
            return;
        }
        IPoseAnimationDetector det = _useLocalDetect
            ? (IPoseAnimationDetector)new LocalStubPoseAnimationDetector()
            : new ContinuuuumRemotePoseAnimationDetector { ApiBaseUrl = ContinuuuumApiConfig.GetApiBaseUrl() };
        _asset.lastTrack = det.Detect(_asset.localClipPath, _asset.modelSpec);
        EditorUtility.SetDirty(_asset);
        _status = det.Id + " samples=" + (_asset.lastTrack != null ? _asset.lastTrack.Count : 0);
    }

    void SpawnGrabber()
    {
        if (_asset == null || _asset.actorPrefab == null)
        {
            _status = "Assign actor prefab first.";
            return;
        }
        var go = PrefabUtility.InstantiatePrefab(_asset.actorPrefab) as GameObject;
        if (go == null)
            go = Instantiate(_asset.actorPrefab);
        if (go.GetComponent<BoneDimensionGrabber>() == null)
            go.AddComponent<BoneDimensionGrabber>();
        Selection.activeGameObject = go;
        _status = "Spawned grabber on " + go.name;
    }

    void UploadToContinuuuum()
    {
        if (_asset == null)
            return;
        string meta = _asset.ToTypeMetadata().ToJson();
        string baseUrl = ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/');
        if (string.IsNullOrEmpty(_asset.localClipPath) || !File.Exists(_asset.localClipPath))
        {
            PostMetadataOnly(baseUrl, meta);
            return;
        }
        try
        {
            byte[] bytes = File.ReadAllBytes(_asset.localClipPath);
            var form = new WWWForm();
            form.AddField("document_type", "video");
            form.AddField("type_metadata", meta);
            form.AddBinaryData("file", bytes, Path.GetFileName(_asset.localClipPath), "video/mp4");
            var req = UnityWebRequest.Post(baseUrl + "/api/table-read/usc-upload", form);
            var op = req.SendWebRequest();
            while (!op.isDone)
            { }
            _status = req.result == UnityWebRequest.Result.Success
                ? "Uploaded: " + req.downloadHandler.text
                : "Upload failed: " + req.error + " " + req.downloadHandler?.text;
            req.Dispose();
        }
        catch (System.Exception ex)
        {
            PostMetadataOnly(baseUrl, meta);
            _status = "File upload skipped (" + ex.Message + "); posted metadata.";
        }
    }

    void PostMetadataOnly(string baseUrl, string metaJson)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(metaJson);
            var req = new UnityWebRequest(baseUrl + "/api/webcam-animations", "POST");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            var op = req.SendWebRequest();
            while (!op.isDone)
            { }
            if (req.result == UnityWebRequest.Result.Success)
            {
                var parsed = JsonUtility.FromJson<IdWrap>(req.downloadHandler.text);
                if (parsed != null && !string.IsNullOrEmpty(parsed.id))
                    _asset.libraryDocId = parsed.id;
                _status = "Metadata saved " + req.downloadHandler.text;
            }
            else
                _status = "Metadata POST failed: " + req.error;
            req.Dispose();
        }
        catch (System.Exception ex)
        {
            _status = "Metadata POST error: " + ex.Message;
        }
    }

    [System.Serializable]
    sealed class IdWrap
    {
        public string id;
    }
}
#endif
