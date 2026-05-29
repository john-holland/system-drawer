using System;
using UnityEngine;

namespace SdfMax
{
    [Serializable]
    public sealed class SdfMaxNode
    {
        public SdfMaxOp op = SdfMaxOp.PrimitiveLeaf;
        public SdfPrimitiveType primitiveType = SdfPrimitiveType.Box;
        public Vector3 localPosition;
        public Vector3 localRotationEuler;
        public Vector3 localScale = Vector3.one;
        public float radius = 0.5f;
        public Vector3 halfExtents = Vector3.one * 0.5f;
        public float constantValue;
        public float blendK = 0.1f;
        public float smoothRadius = 0.25f;
        public float tMin;
        public float tMax = 1f;
        public float weight = 1f;
        public Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.35f);
        public int childIndexA = -1;
        public int childIndexB = -1;
    }
}
