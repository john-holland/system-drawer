using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Reads hinge/configurable joint metadata from a rigidbody panel.</summary>
    public static class OpenableJointProbe
    {
        public struct JointInfo
        {
            public OpenCloseJointKind kind;
            public Vector3 axis;
            public float minAngle;
            public float maxAngle;
            public float restAngle;
            public bool isLocked;
            public Rigidbody body;
        }

        public static bool TryProbe(GameObject go, out JointInfo info)
        {
            info = default;
            if (go == null)
                return false;

            var latch = go.GetComponent<OpenableLatch>();
            bool latched = latch != null && !latch.isUnlocked;

            var hinge = go.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                info.kind = OpenCloseJointKind.Hinge;
                info.axis = hinge.axis.normalized;
                info.body = hinge.GetComponent<Rigidbody>() ?? go.GetComponent<Rigidbody>();
                if (hinge.useLimits)
                {
                    info.minAngle = hinge.limits.min;
                    info.maxAngle = hinge.limits.max;
                }
                else
                {
                    info.minAngle = -120f;
                    info.maxAngle = 120f;
                }
                info.restAngle = hinge.spring.targetPosition;
                info.isLocked = latched;
                return true;
            }

            var cfg = go.GetComponent<ConfigurableJoint>();
            if (cfg != null)
            {
                info.kind = OpenCloseJointKind.Configurable;
                info.axis = cfg.axis.normalized;
                info.body = cfg.GetComponent<Rigidbody>() ?? go.GetComponent<Rigidbody>();
                info.minAngle = cfg.lowAngularXLimit.limit;
                info.maxAngle = cfg.highAngularXLimit.limit;
                info.restAngle = 0f;
                info.isLocked = latched;
                return true;
            }

            if (latch != null)
            {
                info.kind = OpenCloseJointKind.LatchOnly;
                info.isLocked = latched;
                info.body = go.GetComponent<Rigidbody>();
                return true;
            }

            return false;
        }

        public static OpenCloseJointKind InferKind(GameObject go)
        {
            return TryProbe(go, out var info) ? info.kind : OpenCloseJointKind.Hinge;
        }
    }
}
