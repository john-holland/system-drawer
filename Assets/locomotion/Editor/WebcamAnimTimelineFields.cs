using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

/// <summary>Text In/Out/Duration/Limit for webcam and vehicle takes. Empty limit = auto from video then animation file.</summary>
public static class WebcamAnimTimelineFields
{
    public static bool TryGetVideoLengthMs(VideoPlayer player, out double ms)
    {
        ms = 0.0;
        if (player == null)
            return false;
        double sec = player.length;
        if (double.IsNaN(sec) || double.IsInfinity(sec) || sec <= 0.0)
            return false;
        ms = sec * 1000.0;
        return true;
    }

    public static void DrawInOutDuration(WebcamAnimRecordingAsset asset, VideoPlayer videoPlayer = null)
    {
        if (asset == null)
            return;

        EditorGUI.BeginChangeCheck();
        string inText = EditorGUILayout.DelayedTextField("In (ms)", AnimationSpanDuration.FormatMs(asset.startMs));
        if (EditorGUI.EndChangeCheck() && AnimationSpanDuration.TryParseMs(inText, out double inMs))
            asset.SetUserInMs(inMs);

        EditorGUI.BeginChangeCheck();
        string outText = EditorGUILayout.DelayedTextField("Out (ms)", AnimationSpanDuration.FormatMs(asset.endMs));
        if (EditorGUI.EndChangeCheck() && AnimationSpanDuration.TryParseMs(outText, out double outMs))
            asset.SetUserOutMs(outMs);

        EditorGUI.BeginChangeCheck();
        string durText = EditorGUILayout.DelayedTextField("Duration (ms)", AnimationSpanDuration.FormatMs(asset.DurationMs));
        if (EditorGUI.EndChangeCheck())
        {
            if (string.IsNullOrWhiteSpace(durText))
            {
                asset.userSetTimeline = false;
                ApplyAuto(asset, videoPlayer);
            }
            else if (AnimationSpanDuration.TryParseMs(durText, out double durMs) && durMs > 0.0)
                asset.SetUserDurationMs(durMs);
        }

        EditorGUI.BeginChangeCheck();
        string limitShown = asset.userDurationLimitMs > 0.0
            ? AnimationSpanDuration.FormatMs(asset.userDurationLimitMs)
            : "";
        string limitText = EditorGUILayout.DelayedTextField("Limit (ms)", limitShown);
        if (EditorGUI.EndChangeCheck())
        {
            if (string.IsNullOrWhiteSpace(limitText))
                asset.userDurationLimitMs = 0.0;
            else if (AnimationSpanDuration.TryParseMs(limitText, out double limitMs) && limitMs >= 0.0)
                asset.userDurationLimitMs = limitMs;
            if (!asset.userSetTimeline)
                ApplyAuto(asset, videoPlayer);
        }

        asset.startMs = WebcamAnimTimelineGranularityUtil.SnapMs(asset.startMs, asset.granularity);
        asset.endMs = WebcamAnimTimelineGranularityUtil.SnapMs(asset.endMs, asset.granularity);

        double videoMs = 0.0;
        if (TryGetVideoLengthMs(videoPlayer, out double fromPlayer))
            videoMs = fromPlayer;
        else if (asset.cachedVideoDurationMs > 0.0)
            videoMs = asset.cachedVideoDurationMs;
        double fileMs = asset.lastTrack != null ? asset.lastTrack.LatestTimeMs() : 0.0;
        if (fileMs <= 0.0 && asset.lastVehicleTrack?.frames != null && asset.lastVehicleTrack.frames.Length > 0)
            fileMs = asset.lastVehicleTrack.frames[asset.lastVehicleTrack.frames.Length - 1].tMs;

        if (!asset.userSetTimeline)
            asset.ApplyAutoDurationFromSources(videoMs, fileMs);

        string source = asset.userSetTimeline
            ? "user"
            : (asset.userDurationLimitMs > 0.0
                ? "user limit"
                : (videoMs > 0.0 ? "video" : (fileMs > 0.0 ? "animation file" : "recording")));
        EditorGUILayout.LabelField(
            $"Span {AnimationSpanDuration.FormatMs(asset.DurationMs)} ms ({source})",
            EditorStyles.miniLabel);
    }

    public static void BindVideoPrepare(VideoPlayer player, WebcamAnimRecordingAsset asset)
    {
        if (player == null || asset == null)
            return;
        player.prepareCompleted += vp =>
        {
            if (vp == null || asset == null)
                return;
            if (TryGetVideoLengthMs(vp, out double ms))
                asset.ApplyAutoDurationFromSources(ms, asset.lastTrack != null ? asset.lastTrack.LatestTimeMs() : 0.0);
        };
        if (player.isPrepared && TryGetVideoLengthMs(player, out double already))
            asset.ApplyAutoDurationFromSources(already, asset.lastTrack != null ? asset.lastTrack.LatestTimeMs() : 0.0);
        else
            player.Prepare();
    }

    public static float PlayheadMaxMs(WebcamAnimRecordingAsset recording, PoseTrack track)
    {
        double limit = recording != null ? recording.userDurationLimitMs : 0.0;
        double videoOrRec = 0.0;
        if (recording != null && (recording.userSetTimeline || !recording.TimelineLooksDefault))
            videoOrRec = recording.DurationMs;
        if (recording != null && recording.cachedVideoDurationMs > 0.0 && videoOrRec <= 0.0)
            videoOrRec = recording.cachedVideoDurationMs;
        double file = 0.0;
        if (track != null)
            file = track.LatestTimeMs();
        else if (recording?.lastTrack != null)
            file = recording.lastTrack.LatestTimeMs();
        return (float)AnimationSpanDuration.ResolveMs(limit, videoOrRec, file, 1000.0);
    }

    public static float DrawPlayheadMs(string label, float playheadMs, float maxMs)
    {
        maxMs = Mathf.Max(1f, maxMs);
        EditorGUI.BeginChangeCheck();
        string text = EditorGUILayout.DelayedTextField(label, AnimationSpanDuration.FormatMs(playheadMs));
        if (EditorGUI.EndChangeCheck() && AnimationSpanDuration.TryParseMs(text, out double parsed))
            playheadMs = (float)parsed;
        return Mathf.Clamp(playheadMs, 0f, maxMs);
    }

    static void ApplyAuto(WebcamAnimRecordingAsset asset, VideoPlayer videoPlayer)
    {
        double videoMs = 0.0;
        if (TryGetVideoLengthMs(videoPlayer, out double fromPlayer))
            videoMs = fromPlayer;
        else if (asset.cachedVideoDurationMs > 0.0)
            videoMs = asset.cachedVideoDurationMs;
        double fileMs = asset.lastTrack != null ? asset.lastTrack.LatestTimeMs() : 0.0;
        asset.ApplyAutoDurationFromSources(videoMs, fileMs);
    }
}
