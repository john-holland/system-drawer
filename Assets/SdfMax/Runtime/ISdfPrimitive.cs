using UnityEngine;

namespace SdfMax
{
    public interface ISdfPrimitive
    {
        float Evaluate(Vector3 localPoint, float t);
    }
}
