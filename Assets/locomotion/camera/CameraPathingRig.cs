using UnityEngine;

namespace Locomotion.Camera
{
    /// <summary>Main camera rig wiring pathing solver, transitions, and optional LSTM hints.</summary>
    public sealed class CameraPathingRig : MonoBehaviour
    {
        public UnityEngine.Camera rigCamera;
        public HierarchicalCameraPathingSolver pathingSolver;
        public CameraTransitionController transitionController;
        public Inference.CameraTopologyLSTM topologyLstm;

        [Header("Targets")]
        public Transform objectTarget;
        public Transform characterRoot;
        public Transform headSocket;
        public Transform firstPersonPivot;

        [Header("Runtime")]
        public CameraFocusMode activeMode = CameraFocusMode.Character;
        public CameraPlannerHints hints = CameraPlannerHints.Default();
        public string episodeId;
        public string shotId;

        CameraPathingContext _ctx;

        void Awake()
        {
            if (rigCamera == null)
                rigCamera = GetComponent<UnityEngine.Camera>();
            if (pathingSolver == null)
                pathingSolver = GetComponent<HierarchicalCameraPathingSolver>();
            if (transitionController == null)
                transitionController = GetComponent<CameraTransitionController>();
            if (topologyLstm == null)
                topologyLstm = GetComponent<Inference.CameraTopologyLSTM>();

            _ctx = BuildContext();
        }

        CameraPathingContext BuildContext()
        {
            return new CameraPathingContext
            {
                camera = rigCamera,
                objectTarget = objectTarget,
                characterRoot = characterRoot,
                headSocket = headSocket,
                firstPersonPivot = firstPersonPivot,
            };
        }

        void LateUpdate()
        {
            RefreshContext();
            if (topologyLstm != null && pathingSolver != null)
            {
                var leaves = pathingSolver.Leaves;
                if (topologyLstm.TryPredict(rigCamera, leaves, activeMode, _ctx.actorVisionSalience, out var bias, out var mem))
                    CameraPathingHeuristic.ApplyLstmHints(ref hints, bias, mem);
            }
        }

        void RefreshContext()
        {
            _ctx.camera = rigCamera;
            _ctx.objectTarget = objectTarget;
            _ctx.characterRoot = characterRoot;
            _ctx.headSocket = headSocket;
            _ctx.firstPersonPivot = firstPersonPivot;
        }

        public void SetFocusMode(CameraFocusMode mode, bool transition = true)
        {
            activeMode = mode;
            RefreshContext();
            if (pathingSolver == null || rigCamera == null) return;

            var pose = pathingSolver.ComputePose(mode, _ctx);
            if (transition && transitionController != null)
                transitionController.RequestTransition(pose, TransitionProfile.Default());
            else
                pose.ApplyTo(rigCamera);
        }

        public void PlanAndApplyPath(CameraRigPose goal)
        {
            RefreshContext();
            if (pathingSolver == null) return;
            var start = CameraRigPose.FromCamera(rigCamera, activeMode);
            var path = pathingSolver.PlanCameraPath(start, goal, in hints, _ctx);
            if (path.Count > 1 && transitionController != null)
                transitionController.RequestTransition(path[path.Count - 1], TransitionProfile.Default(1.2f));
        }
    }
}
