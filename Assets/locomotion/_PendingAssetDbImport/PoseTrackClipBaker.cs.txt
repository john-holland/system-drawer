#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Locomotion.Rig;
using UnityEditor;
using UnityEngine;

namespace Locomotion.EditorTools
{
    /// <summary>Bake a remapped PoseTrack to an AnimationClip and append a RagdollAnimationSet.</summary>
    public static class PoseTrackClipBaker
    {
        public static AnimationClip BakeClip(PoseTrack track, BoneMap map, Transform root, string clipName)
        {
            var clip = new AnimationClip { name = string.IsNullOrEmpty(clipName) ? "PoseTrack" : clipName };
            clip.legacy = false;
            if (track == null || map == null || root == null)
                return clip;

            var ids = new List<string>();
            track.CollectTraitIds(ids);
            var times = CollectTimes(track);
            if (times.Count == 0)
                times.Add(0f);

            for (int i = 0; i < ids.Count; i++)
            {
                if (!map.TryGet(ids[i], out var t) || t == null)
                    continue;
                string path = AnimationUtility.CalculateTransformPath(t, root);
                var px = new AnimationCurve();
                var py = new AnimationCurve();
                var pz = new AnimationCurve();
                var rx = new AnimationCurve();
                var ry = new AnimationCurve();
                var rz = new AnimationCurve();
                var rw = new AnimationCurve();
                for (int k = 0; k < times.Count; k++)
                {
                    if (!track.TrySample(ids[i], times[k], out var pos, out var rot))
                        continue;
                    float sec = times[k] / 1000f;
                    px.AddKey(sec, pos.x);
                    py.AddKey(sec, pos.y);
                    pz.AddKey(sec, pos.z);
                    rx.AddKey(sec, rot.x);
                    ry.AddKey(sec, rot.y);
                    rz.AddKey(sec, rot.z);
                    rw.AddKey(sec, rot.w);
                }
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", px);
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", py);
                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", pz);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", rx);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", ry);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", rz);
                clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", rw);
            }

            return clip;
        }

        public static int BakeAndAddSet(
            RagdollIKAnimationManager ik,
            PoseTrack track,
            BoneMap map,
            Transform root,
            string displayName,
            bool syncSelection = true)
        {
            if (ik == null || track == null)
                return -1;
            string folder = "Assets/WebcamAnim/Baked";
            if (!AssetDatabase.IsValidFolder("Assets/WebcamAnim"))
                AssetDatabase.CreateFolder("Assets", "WebcamAnim");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/WebcamAnim", "Baked");

            var clip = BakeClip(track, map, root, displayName);
            string clipPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + Sanitize(displayName) + ".anim");
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            int idx = RagdollIKAnimationManagerEditor.AddAnimationSetFromClip(ik, clip, folder);
            if (idx >= 0 && syncSelection)
            {
                if (ik.selectedIndicesForTraining == null)
                    ik.selectedIndicesForTraining = new List<int>();
                if (!ik.selectedIndicesForTraining.Contains(idx))
                    ik.selectedIndicesForTraining.Add(idx);
                ik.SyncSelectionToSetManagerAndHierarchy();
            }
            return idx;
        }

        static List<float> CollectTimes(PoseTrack track)
        {
            var set = new SortedSet<float>();
            if (track?.samples != null)
            {
                for (int i = 0; i < track.samples.Count; i++)
                {
                    if (track.samples[i] != null)
                        set.Add(track.samples[i].timeMs);
                }
            }
            return new List<float>(set);
        }

        static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "PoseTrack";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
#endif
