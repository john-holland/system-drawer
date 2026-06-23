#if UNITY_EDITOR
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Camera.Editor
{
    public static class CameraTopologyExportUtility
    {
        public static CameraTopologySample CaptureSample(CameraPathingRig rig)
        {
            if (rig == null || rig.rigCamera == null) return null;

            var solver = rig.pathingSolver;
            solver?.RebuildIfNeeded(force: false);
            var leaves = solver?.Leaves;

            var ctx = new CameraPathingContext
            {
                camera = rig.rigCamera,
                objectTarget = rig.objectTarget,
                characterRoot = rig.characterRoot,
                headSocket = rig.headSocket,
                firstPersonPivot = rig.firstPersonPivot,
            };

            float salience = Strategies.ActorVisionTrainingFocusStrategy.ComputeMemorabilityMl(ctx);
            return new CameraTopologySample
            {
                episodeId = rig.episodeId,
                shotId = rig.shotId,
                focusMode = rig.activeMode.ToString(),
                topologyVector = FrustumAlignedOctreeBasis.BuildTopologyVector(rig.rigCamera, leaves),
                memorabilityMl = salience,
                userRatingMean = rig.hints.userRatingMean,
                actorVisionSalience = salience,
                rigPose = CameraRigPose.FromCamera(rig.rigCamera, rig.activeMode),
            };
        }

        public static void ExportRig(CameraPathingRig rig, string folder)
        {
            var sample = CaptureSample(rig);
            if (sample == null) return;
            Directory.CreateDirectory(folder);
            string name = string.IsNullOrEmpty(sample.shotId) ? $"shot_{System.Guid.NewGuid():N}" : sample.shotId;
            string path = Path.Combine(folder, name + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(sample, Formatting.Indented));
        }
    }

    public static class CameraTopologyExportMenu
    {
        [MenuItem("Locomotion/Camera/Export topology for LSTM training...")]
        static void ExportSelected()
        {
            var rig = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<CameraPathingRig>()
                : null;
            if (rig == null)
            {
                EditorUtility.DisplayDialog("Camera export", "Select a GameObject with CameraPathingRig.", "OK");
                return;
            }

            string folder = EditorUtility.OpenFolderPanel("Export camera topology", "CameraTopology_Training", "");
            if (string.IsNullOrEmpty(folder)) return;
            CameraTopologyExportUtility.ExportRig(rig, folder);
            EditorUtility.DisplayDialog("Camera export", "Exported sample JSON.", "OK");
        }
    }
}
#endif
