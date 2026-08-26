using UnityEditor;
using UnityEngine;

public static class WebcamAnimTimeScrubberDrawer
{
    public static void Draw(WebcamAnimTimeScrubber scrubber, double[] tickMs)
    {
        if (scrubber == null) return;
        EditorGUILayout.LabelField("Time scrubber", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(scrubber.playing ? "Pause" : "Play", GUILayout.Width(64)))
        {
            if (scrubber.playing) scrubber.Pause();
            else scrubber.Play();
        }
        if (GUILayout.Button("Stop", GUILayout.Width(64)))
            scrubber.Stop();
        EditorGUILayout.LabelField($"{scrubber.playheadMs:0.#} ms  /  {scrubber.endMs:0.#} ms", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        float min = (float)scrubber.startMs;
        float max = (float)Mathf.Max((float)scrubber.endMs, min + 1f);
        float play = (float)scrubber.playheadMs;
        float next = WebcamAnimTimelineFields.DrawPlayheadMs("Playhead ms", play, max);
        if (next < min)
            next = min;
        if (Mathf.Abs(next - play) > 1e-3f)
        {
            scrubber.Pause();
            scrubber.Seek(next);
        }

        Rect bar = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(12f));
        EditorGUI.DrawRect(bar, new Color(0.15f, 0.15f, 0.18f, 1f));
        double span = scrubber.DurationMs;
        if (tickMs != null)
        {
            for (int i = 0; i < tickMs.Length; i++)
            {
                float x = bar.x + (float)((tickMs[i] - scrubber.startMs) / span) * bar.width;
                EditorGUI.DrawRect(new Rect(x, bar.y, 2f, bar.height), new Color(1f, 0.7f, 0.2f, 1f));
            }
        }
        float px = bar.x + scrubber.Normalized01 * bar.width;
        EditorGUI.DrawRect(new Rect(px - 1f, bar.y, 2f, bar.height), Color.white);
        Event e = Event.current;
        if (e != null && bar.Contains(e.mousePosition) &&
            (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            float t = Mathf.InverseLerp(bar.x, bar.xMax, e.mousePosition.x);
            scrubber.Pause();
            scrubber.Seek(scrubber.startMs + t * span);
            e.Use();
        }
    }

    public static void DrawOverlaySettings(WebcamAnimRecordingAsset asset)
    {
        if (asset == null) return;
        EditorGUILayout.LabelField("Video / scene overlay", EditorStyles.boldLabel);
        asset.previewVideoOpacity = EditorGUILayout.Slider(
            "Recording opacity", asset.previewVideoOpacity, 0f, 1f);
        asset.previewVideoScale = EditorGUILayout.Slider(
            "Recording scale (1 = 1:1 stretch)", asset.previewVideoScale, 0.25f, 2f);
        asset.previewVideoOffset01 = EditorGUILayout.Vector2Field("Recording offset (01)", asset.previewVideoOffset01);
        asset.syncVehicleRagdoll = EditorGUILayout.Toggle("Sync vehicle ragdoll", asset.syncVehicleRagdoll);
    }

    public static void DrawCameraShots(WebcamAnimRecordingAsset asset)
    {
        if (asset == null) return;
        EditorGUILayout.LabelField("Multi-mode cameras", EditorStyles.boldLabel);
        int n = asset.cameraShots != null ? asset.cameraShots.Length : 0;
        int next = EditorGUILayout.IntField("Shots", n);
        if (next < 0) next = 0;
        if (next != n)
        {
            var arr = new WebcamAnimCameraShot[next];
            for (int i = 0; i < next; i++)
                arr[i] = i < n && asset.cameraShots[i] != null
                    ? asset.cameraShots[i]
                    : new WebcamAnimCameraShot { startMs = asset.startMs, transitionSec = 0.75f };
            asset.cameraShots = arr;
        }
        if (asset.cameraShots == null) return;
        for (int i = 0; i < asset.cameraShots.Length; i++)
        {
            var s = asset.cameraShots[i] ?? (asset.cameraShots[i] = new WebcamAnimCameraShot());
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shot " + i, EditorStyles.miniBoldLabel);
            s.startMs = EditorGUILayout.DoubleField("Switch at ms", s.startMs);
            s.focusMode = (Locomotion.Camera.CameraFocusMode)EditorGUILayout.EnumPopup("Focus mode", s.focusMode);
            s.cameraIndex = EditorGUILayout.IntField("Camera list index", s.cameraIndex);
            s.transition = (WebcamAnimCameraTransition)EditorGUILayout.EnumPopup("Transition", s.transition);
            s.transitionSec = EditorGUILayout.FloatField("Transition sec", s.transitionSec);
            EditorGUILayout.EndVertical();
        }
    }

    public static void DrawComposite(Rect host, Texture scene, Texture video, WebcamAnimRecordingAsset asset)
    {
        if (Event.current.type != EventType.Repaint) return;
        EditorGUI.DrawRect(host, new Color(0.08f, 0.08f, 0.1f, 1f));
        if (scene != null)
            GUI.DrawTexture(host, scene, ScaleMode.StretchToFill, false);
        if (video != null && asset != null)
        {
            var old = GUI.color;
            GUI.color = WebcamAnimPreviewOverlay.VideoTint(asset.previewVideoOpacity);
            var vr = WebcamAnimPreviewOverlay.AlignedVideoRect(
                host, asset.previewVideoScale, asset.previewVideoOffset01);
            GUI.DrawTexture(vr, video, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }
    }
}
