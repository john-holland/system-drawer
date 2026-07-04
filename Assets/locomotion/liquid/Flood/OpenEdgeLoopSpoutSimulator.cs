using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Locomotion.Liquid.Flood
{
    /// <summary>Open rim / spout lip for open-top vessels.</summary>
    public sealed class OpenEdgeLoopSpoutSimulator : MonoBehaviour
    {
        public Transform rimCenter;
        public float rimRadiusM = 0.035f;
        public Vector3 loopNormal = Vector3.up;
        public bool willDrain = true;
        public float effectiveOutletAreaM2;

        public Vector3 RimWorldPosition => rimCenter != null ? rimCenter.position : transform.position;

        void OnValidate()
        {
            effectiveOutletAreaM2 = Mathf.PI * rimRadiusM * rimRadiusM;
        }

        public bool TryExitLoop(Vector3 spherePos, out Vector3 exitVelocity)
        {
            exitVelocity = Vector3.zero;
            if (!willDrain)
                return false;
            Vector3 rim = RimWorldPosition;
            float dy = spherePos.y - rim.y;
            if (dy < -0.01f)
                return false;
            float dxz = new Vector2(spherePos.x - rim.x, spherePos.z - rim.z).magnitude;
            if (dxz > rimRadiusM * 1.2f)
                return false;
            exitVelocity = loopNormal.normalized * 0.5f + Vector3.down * 0.3f;
            return true;
        }
    }
}
