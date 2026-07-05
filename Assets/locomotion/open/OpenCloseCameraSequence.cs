using Locomotion.Camera;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Drives CameraPathingRig through open/close topology stops.</summary>
    public sealed class OpenCloseCameraSequence : MonoBehaviour
    {
        public CameraPathingRig rig;

        [System.NonSerialized] OpenCloseTopologyNode _parentStop;
        OpenCloseTopologyNode _current;

        void Awake()
        {
            if (rig == null)
                rig = FindAnyObjectByType<CameraPathingRig>();
        }

        public void FocusStop(OpenCloseTopologyNode node, OpenCloseLemmaProperties lemmaOverrides = default)
        {
            if (node == null || rig == null)
                return;

            _current = node;
            if (node.target != null)
                rig.objectTarget = node.target.transform;

            float blend = lemmaOverrides.arrivalBlendCoefficient > 0f
                ? lemmaOverrides.arrivalBlendCoefficient
                : node.arrivalBlendCoefficient;

            bool strictSync = blend <= 0f;
            var stop = OpenCloseCameraStop.Compute(node, _parentStop, rig.rigCamera != null ? rig.rigCamera.fieldOfView : 60f);
            var goal = new CameraRigPose
            {
                position = stop.position,
                rotation = stop.rotation,
                fieldOfView = stop.fieldOfView,
                focusMode = CameraFocusMode.ObjectFocus,
            };

            if (strictSync)
            {
                rig.SetFocusMode(CameraFocusMode.ObjectFocus, true);
                if (rig.transitionController != null)
                    rig.transitionController.RequestTransition(goal, TransitionProfile.Default(0.8f));
            }
            else
                rig.PlanAndApplyPath(goal);

            _parentStop = node;
        }

        public void RestoreCharacter()
        {
            if (rig != null)
                rig.SetFocusMode(CameraFocusMode.Character, true);
            _current = null;
        }
    }
}
