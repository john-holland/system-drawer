#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Locomotion.Rig;
using NUnit.Framework;
using UnityEngine;

public sealed class WebcamAnimAndDanceTests
{
    [Test]
    public void Granularity_TickMs_AndSnap()
    {
        Assert.AreEqual(0.1, WebcamAnimTimelineGranularityUtil.TickMs(WebcamAnimTimelineGranularity.Decimillisecond), 1e-9);
        Assert.AreEqual(1.0, WebcamAnimTimelineGranularityUtil.TickMs(WebcamAnimTimelineGranularity.Millisecond), 1e-9);
        Assert.AreEqual(10.0, WebcamAnimTimelineGranularityUtil.TickMs(WebcamAnimTimelineGranularity.Centisecond), 1e-9);
        Assert.AreEqual(1000.0, WebcamAnimTimelineGranularityUtil.TickMs(WebcamAnimTimelineGranularity.Second), 1e-9);
        Assert.AreEqual(WebcamAnimTimelineGranularity.Millisecond, WebcamAnimTimelineGranularityUtil.FromSlider(1));
        Assert.AreEqual(1230.0, WebcamAnimTimelineGranularityUtil.SnapMs(1234.0, WebcamAnimTimelineGranularity.Centisecond), 1e-9);
        Assert.AreEqual("millisecond", WebcamAnimTimelineGranularityUtil.JsonName(WebcamAnimTimelineGranularity.Millisecond));
    }

    [Test]
    public void DanceIntersect_VetoBlocksDiagonalConflicts()
    {
        var a = new DancePairing { callSlot01 = 0.1f, responseSlot01 = 0.9f };
        var b = new DancePairing { callSlot01 = 0.9f, responseSlot01 = 0.1f };
        Assert.IsTrue(DanceMirrorMap.PairingsIntersect(a, b));
        Assert.IsTrue(DanceMirrorMap.IsBlockedByIntersect(new[] { a }, b, allowIntersect: false));
        Assert.IsFalse(DanceMirrorMap.IsBlockedByIntersect(new[] { a }, b, allowIntersect: true));

        var routine = ScriptableObject.CreateInstance<DanceRoutineBehaviorTreeAsset>();
        try
        {
            routine.allowIntersect = false;
            Assert.IsTrue(routine.TryAddPairing(a, out _));
            Assert.IsFalse(routine.TryAddPairing(b, out var err));
            Assert.IsNotNull(err);
            Assert.AreEqual(1, routine.pairings.Count);

            routine.allowIntersect = true;
            Assert.IsTrue(routine.TryAddPairing(b, out _));
            Assert.AreEqual(2, routine.pairings.Count);
        }
        finally
        {
            Object.DestroyImmediate(routine);
        }
    }

    [Test]
    public void RecordingAsset_ModelSpecSubsection_RoundTrip()
    {
        var asset = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
        try
        {
            asset.kind = WebcamAnimKind.Vehicle;
            asset.modelSpec = "mediapipe_holistic@v1";
            asset.subsectionId = "takeoff_roll_0";
            asset.animationListIndex = 3;
            asset.startMs = 1200;
            asset.endMs = 8400;
            asset.granularity = WebcamAnimTimelineGranularity.Millisecond;
            asset.targetHint = "magneto_bt";
            asset.vehicleTrackPath = "tracks/car.vehicletrack.json";
            asset.facingYawDegrees = 45f;

            string json = asset.ToTypeMetadata().ToJson();
            var meta = WebcamAnimTypeMetadata.FromJson(json);
            Assert.AreEqual(WebcamAnimTypeMetadata.KindValue, meta.kind);
            Assert.AreEqual("mediapipe_holistic@v1", meta.model_spec);
            Assert.AreEqual("takeoff_roll_0", meta.subsection);
            Assert.AreEqual("tracks/car.vehicletrack.json", meta.vehicleTrackPath);
            Assert.AreEqual(45f, meta.facingYawDegrees, 0.01f);

            var other = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
            try
            {
                other.ApplyTypeMetadata(meta);
                Assert.AreEqual("mediapipe_holistic@v1", other.modelSpec);
                Assert.AreEqual("takeoff_roll_0", other.subsectionId);
                Assert.AreEqual(WebcamAnimKind.Vehicle, other.kind);
                Assert.AreEqual(3, other.animationListIndex);
                Assert.AreEqual(WebcamAnimTimelineGranularity.Millisecond, other.granularity);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void LocalStubDetector_ReturnsSamples()
    {
        var det = new LocalStubPoseAnimationDetector();
        var track = det.Detect("", "stub-spec");
        Assert.AreEqual(LocalStubPoseAnimationDetector.DetectorId, det.Id);
        Assert.Greater(track.Count, 0);
        Assert.AreEqual("stub-spec", track.modelSpec);
    }

    [Test]
    public void WebPreview_ParsesQueryAndBuildsEditorUrl()
    {
        var go = new GameObject("WebcamPreviewTest");
        try
        {
            var preview = go.AddComponent<WebcamAnimWebPreview>();
            preview.ParseAbsoluteUrl("https://host/continuuuum_editor/index.html?docId=abc&subsection=takeoff&startMs=12&endMs=84");
            Assert.AreEqual("abc", preview.libraryDocId);
            Assert.AreEqual("takeoff", preview.subsectionId);
            Assert.AreEqual(12.0, preview.startMs, 1e-6);
            string url = preview.WebGlPreviewUrl("/continuuuum_editor", "http://localhost:5050");
            StringAssert.Contains("docId=abc", url);
            StringAssert.Contains("subsection=takeoff", url);
            StringAssert.Contains("apiBase=", url);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ContainsDialogFalse_ActiveDialogSpansEmpty()
    {
        var routine = ScriptableObject.CreateInstance<DanceRoutineBehaviorTreeAsset>();
        try
        {
            routine.containsDialog = false;
            routine.dialogSpans.Add(new DanceMediaSpan { startMs = 0, endMs = 500, label = "kept" });
            Assert.AreEqual(0, routine.ActiveDialogSpans.Count);
            Assert.AreEqual(1, routine.dialogSpans.Count);

            routine.containsDialog = true;
            Assert.AreEqual(1, routine.ActiveDialogSpans.Count);
        }
        finally
        {
            Object.DestroyImmediate(routine);
        }
    }

    [Test]
    public void MediaSpan_QuantizeSnapsToBeatGrid()
    {
        double snapped = DanceMediaSpan.SnapOne(
            130, bpm: 120f, subdivision: 4, quantize01: 1f,
            WebcamAnimTimelineGranularity.Millisecond);
        Assert.AreEqual(125.0, snapped, 1e-6);

        var span = new DanceMediaSpan { startMs = 130, endMs = 280 };
        span.Snap(120f, 4, 1f, WebcamAnimTimelineGranularity.Millisecond);
        Assert.AreEqual(125.0, span.startMs, 1e-6);
        Assert.AreEqual(250.0, span.endMs, 1e-6);
    }

    [Test]
    public void AudioSpanDetectors_ReturnOrderedSpans()
    {
        var stub = new LocalStubAudioSpanDetector();
        var spans = stub.Detect("clip.wav", "stub-spec");
        Assert.AreEqual(LocalStubAudioSpanDetector.DetectorId, stub.Id);
        Assert.GreaterOrEqual(spans.Length, 1);
        Assert.Less(spans[0].startMs, spans[0].endMs);

        var whisper = new WhisperDialogSpanDetector();
        var empty = whisper.Detect("", WhisperDialogSpanDetector.DefaultModelSpec);
        Assert.AreEqual(0, empty.Length);
        var dialog = whisper.Detect("take.wav", "whisper@base");
        Assert.GreaterOrEqual(dialog.Length, 1);
        Assert.Less(dialog[0].startMs, dialog[0].endMs);

        var music = new MusicAnalysisSpanDetector();
        var song = music.Detect("mix.wav", MusicAnalysisSpanDetector.DefaultModelSpec);
        Assert.GreaterOrEqual(song.Length, 1);
        Assert.Less(song[0].startMs, song[0].endMs);
    }

    [Test]
    public void DanceRunner_QuantizeDelayZeroWhenNoSong()
    {
        var go = new GameObject("DanceRunnerTest");
        try
        {
            var runner = go.AddComponent<DanceRoutineRunner>();
            var routine = ScriptableObject.CreateInstance<DanceRoutineBehaviorTreeAsset>();
            try
            {
                routine.containsSong = false;
                runner.routine = routine;
                Assert.AreEqual(0f, runner.QuantizeDelaySec());
            }
            finally
            {
                Object.DestroyImmediate(routine);
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    const string TinyBvh = @"HIERARCHY
ROOT Hips
{
  OFFSET 0 0 0
  CHANNELS 6 Xposition Yposition Zposition Zrotation Yrotation Xrotation
  JOINT Spine
  {
    OFFSET 0 10 0
    CHANNELS 3 Zrotation Yrotation Xrotation
    End Site
    {
      OFFSET 0 5 0
    }
  }
}
MOTION
Frames: 2
Frame Time: 0.033333
0 0 0 0 0 0 0 0 0
0 1 0 10 0 0 5 0 0
";

    [Test]
    public void BvhImporter_ParsesJointsAndSamples()
    {
        var track = BvhPoseTrackImporter.FromText(TinyBvh, "mocapanything@v2");
        Assert.AreEqual("mocapanything@v2", track.modelSpec);
        Assert.Greater(track.Count, 0);
        var joints = new List<BvhPoseTrackImporter.Joint>();
        BvhPoseTrackImporter.CollectJoints(TinyBvh, joints);
        Assert.AreEqual(2, joints.Count);
        Assert.AreEqual("Hips", joints[0].name);
        Assert.AreEqual("Spine", joints[1].name);
        Assert.AreEqual(0, joints[1].parent);
        Assert.IsTrue(track.TrySample("Hips", 33.3f, out var pos, out _));
        Assert.AreEqual(1f, pos.y, 0.01f);
    }

    [Test]
    public void PoseTrack_RemapAndPlayer_WritesBone()
    {
        var go = new GameObject("PosePlayer");
        var bone = new GameObject("Hips");
        bone.transform.SetParent(go.transform, false);
        try
        {
            var map = go.AddComponent<BoneMap>();
            map.Set("Human:Hips", bone.transform);
            var track = new PoseTrack { modelSpec = "mediapipe_holistic@v1" };
            track.samples.Add(new PoseBoneSample
            {
                traitId = "Hips",
                timeMs = 0,
                localPosition = new Vector3(0, 2, 0),
                localRotation = Quaternion.identity
            });
            var remapped = track.RemapTraitIds(new Dictionary<string, string> { { "Hips", "Human:Hips" } });
            int n = PoseTrackPlayer.Apply(remapped, map, 0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(2f, bone.transform.localPosition.y, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PoseTrackLiveDto_ParsesSamplesArray()
    {
        const string json = "{\"modelSpec\":\"mediapipe_holistic@v1\",\"tMs\":12,\"samples\":[{\"traitId\":\"Human:Hips\",\"timeMs\":12,\"localPosition\":{\"x\":0,\"y\":1,\"z\":0},\"localRotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1}}]}";
        var track = PoseTrackLiveDto.ToTrack(json);
        Assert.AreEqual(1, track.Count);
        Assert.AreEqual("Human:Hips", track.samples[0].traitId);
        Assert.AreEqual(12f, track.LatestTimeMs(), 1e-4f);
    }

    [Test]
    public void PoseTrack_Apply_UsesLatestSampleTime()
    {
        var go = new GameObject("PoseLatest");
        var bone = new GameObject("Hips");
        bone.transform.SetParent(go.transform, false);
        try
        {
            var map = go.AddComponent<BoneMap>();
            map.Set("Human:Hips", bone.transform);
            var track = new PoseTrack { modelSpec = "mediapipe_holistic@v1" };
            track.samples.Add(new PoseBoneSample
            {
                traitId = "Human:Hips",
                timeMs = 0,
                localPosition = new Vector3(0, 1, 0),
                localRotation = Quaternion.identity
            });
            track.samples.Add(new PoseBoneSample
            {
                traitId = "Human:Hips",
                timeMs = 120,
                localPosition = new Vector3(0, 5, 0),
                localRotation = Quaternion.identity
            });
            Assert.AreEqual(120f, track.LatestTimeMs(), 1e-4f);
            int n = PoseTrackPlayer.Apply(track, map, track.LatestTimeMs());
            Assert.AreEqual(1, n);
            Assert.AreEqual(5f, bone.transform.localPosition.y, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void RemoteDetector_LoadsPoseTrackJson()
    {
        string path = Path.Combine(Path.GetTempPath(), "wa_posetrack_test.json");
        var track = new PoseTrack { modelSpec = "mediapipe_holistic@v1" };
        track.samples.Add(new PoseBoneSample { traitId = "Human:Head", timeMs = 0 });
        File.WriteAllText(path, track.ToJson());
        try
        {
            var det = new ContinuuuumRemotePoseAnimationDetector();
            var loaded = det.Detect(path, "mediapipe_holistic@v1");
            Assert.Greater(loaded.Count, 0);
            Assert.AreEqual("Human:Head", loaded.samples[0].traitId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public void TimeScrubber_PlayPauseStop_AndTick()
    {
        var s = new WebcamAnimTimeScrubber { startMs = 100, endMs = 1100, granularity = WebcamAnimTimelineGranularity.Millisecond };
        s.Seek(100);
        s.Play();
        s.Tick(0.2);
        Assert.IsTrue(s.playing);
        Assert.AreEqual(300, s.playheadMs, 0.5);
        s.Pause();
        Assert.IsFalse(s.playing);
        s.Stop();
        Assert.AreEqual(100, s.playheadMs, 0.5);
        s.Play();
        s.Tick(2.0);
        Assert.IsFalse(s.playing);
        Assert.AreEqual(100, s.playheadMs, 0.5);
    }

    [Test]
    public void CameraDirector_PicksShotAndTransitionT()
    {
        var shots = new[]
        {
            new WebcamAnimCameraShot { startMs = 0, transition = WebcamAnimCameraTransition.Cut },
            new WebcamAnimCameraShot { startMs = 1000, transition = WebcamAnimCameraTransition.Blend, transitionSec = 0.5f, cameraIndex = 1, focusMode = Locomotion.Camera.CameraFocusMode.FirstPerson }
        };
        Assert.AreEqual(0, WebcamAnimCameraDirector.ShotIndexAt(shots, 0));
        Assert.AreEqual(0, WebcamAnimCameraDirector.ShotIndexAt(shots, 999));
        Assert.AreEqual(1, WebcamAnimCameraDirector.ShotIndexAt(shots, 1000));
        Assert.AreEqual(1, WebcamAnimCameraDirector.ShotIndexAt(shots, 4000));
        Assert.AreEqual(0f, WebcamAnimCameraDirector.TransitionT(shots[1], 1000), 0.01f);
        Assert.AreEqual(1f, WebcamAnimCameraDirector.TransitionT(shots[1], 1500), 0.01f);
        Assert.AreEqual(1f, WebcamAnimCameraDirector.TransitionT(shots[0], 10), 0.01f);
        var cross = new WebcamAnimCameraShot { startMs = 0, transition = WebcamAnimCameraTransition.Crossfade, transitionSec = 1f };
        Assert.AreEqual(0.5f, WebcamAnimCameraDirector.TransitionT(cross, 500), 0.02f);
    }

    [Test]
    public void PreviewOverlay_AlignedRect_1to1ThenScaleOffset()
    {
        var host = new Rect(10, 20, 200, 100);
        var one = WebcamAnimPreviewOverlay.AlignedVideoRect(host, 1f, Vector2.zero);
        Assert.AreEqual(host.x, one.x, 0.01f);
        Assert.AreEqual(host.width, one.width, 0.01f);
        var scaled = WebcamAnimPreviewOverlay.AlignedVideoRect(host, 0.5f, new Vector2(0.1f, 0f));
        Assert.AreEqual(100f, scaled.width, 0.01f);
        Assert.AreEqual(10f + 50f + 20f, scaled.x, 0.01f);
        Assert.AreEqual(0.5f, WebcamAnimPreviewOverlay.VideoTint(0.5f).a, 0.001f);
    }
}
#endif
