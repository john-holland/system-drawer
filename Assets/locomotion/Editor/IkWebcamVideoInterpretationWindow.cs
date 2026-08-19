#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

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
    double _playheadMs;
    bool _webcamOn;

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
        StopWebcam();
        StopVideoPlayer();
        ReleaseRts();
    }

    void OnEditorUpdate()
    {
        EnsureRts();
        if (_webcamOn && _webcam != null && _webcam.isPlaying)
        {
            Graphics.Blit(_webcam, _videoRt);
            Repaint();
        }
        if (_videoPlayer != null && _videoPlayer.isPlaying)
            Repaint();
    }

    void EnsureRts()
    {
        if (_actorRt == null)
            _actorRt = new RenderTexture(512, 288, 16) { name = "IkWebcamActorRT" };
        if (_videoRt == null)
            _videoRt = new RenderTexture(512, 288, 16) { name = "IkWebcamVideoRT" };
    }

    void ReleaseRts()
    {
        if (_actorRt != null)
        {
            _actorRt.Release();
            DestroyImmediate(_actorRt);
            _actorRt = null;
        }
        if (_videoRt != null)
        {
            _videoRt.Release();
            DestroyImmediate(_videoRt);
            _videoRt = null;
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
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(_webcamOn ? "Stop webcam" : "Start webcam"))
            ToggleWebcam();
        if (GUILayout.Button("Pick video file"))
            PickVideo();
        EditorGUILayout.EndHorizontal();
    }

    void DrawTimeline()
    {
        if (_asset == null)
            return;
        EditorGUILayout.Space();
        _asset.kind = (WebcamAnimKind)EditorGUILayout.EnumPopup("Kind", _asset.kind);
        int g = (int)_asset.granularity;
        g = EditorGUILayout.IntSlider("Granularity", g, 0, 6);
        _asset.granularity = WebcamAnimTimelineGranularityUtil.FromSlider(g);
        EditorGUILayout.LabelField("Tick", WebcamAnimTimelineGranularityUtil.TickMs(_asset.granularity) + " ms (" + _asset.granularity + ")");
        float min = (float)_asset.startMs;
        float max = (float)Mathf.Max((float)_asset.endMs, min + 1f);
        float play = (float)_playheadMs;
        play = EditorGUILayout.Slider("Playhead ms", play, min, max);
        _playheadMs = WebcamAnimTimelineGranularityUtil.SnapMs(play, _asset.granularity);
        EditorGUILayout.BeginHorizontal();
        _asset.startMs = EditorGUILayout.DoubleField("In (ms)", _asset.startMs);
        _asset.endMs = EditorGUILayout.DoubleField("Out (ms)", _asset.endMs);
        EditorGUILayout.EndHorizontal();
        _asset.startMs = WebcamAnimTimelineGranularityUtil.SnapMs(_asset.startMs, _asset.granularity);
        _asset.endMs = WebcamAnimTimelineGranularityUtil.SnapMs(_asset.endMs, _asset.granularity);
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
        _ikManager = (RagdollIKAnimationManager)EditorGUILayout.ObjectField(
            "IK Animation Manager", _ikManager, typeof(RagdollIKAnimationManager), true);
        DrawAnimationIndexPopup();
        _asset.modelSpec = EditorGUILayout.TextField("Model spec", _asset.modelSpec);
        _asset.subsectionId = EditorGUILayout.TextField("Subsection (reedit)", _asset.subsectionId);
        _asset.targetHint = EditorGUILayout.TextField("Target hint", _asset.targetHint);
        _asset.localClipPath = EditorGUILayout.TextField("Local clip path", _asset.localClipPath);
        _asset.libraryDocId = EditorGUILayout.TextField("USC library doc id", _asset.libraryDocId);
        _useLocalDetect = EditorGUILayout.Toggle("Local detect (fast)", _useLocalDetect);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Detect"))
            RunDetect();
        if (GUILayout.Button("Upload to Continuuuum"))
            UploadToContinuuuum();
        if (GUILayout.Button("Spawn bone grabber"))
            SpawnGrabber();
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
        _videoPlayer.Play();
        _status = "Video: " + p;
    }

    void RunDetect()
    {
        if (_asset == null)
            return;
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
