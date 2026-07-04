using UnityEngine;

namespace Weather.Emergence
{
    /// <summary>Shapes egg center/radii from merged emergence vectors.</summary>
    public static class EmergenceEggShaper
    {
        public static void ShapeEgg(
            Vector3 focus,
            Vector3 defaultRadii,
            EmergenceVectorField field,
            out Vector3 center,
            out Vector3 radii)
        {
            center = focus;
            radii = defaultRadii;
            if (field == null || field.Vectors.Count == 0)
                return;

            Vector3 stretch = Vector3.zero;
            Vector3 offset = Vector3.zero;
            float totalWeight = 0f;

            for (int i = 0; i < field.Vectors.Count; i++)
            {
                EmergenceVector v = field.Vectors[i];
                float w = EmergenceVectorField.InfluenceAt(v, focus);
                if (w <= 0f)
                    continue;

                Vector3 dir = v.direction.sqrMagnitude > 1e-6f ? v.direction.normalized : Vector3.forward;
                stretch += new Vector3(
                    Mathf.Abs(dir.x),
                    Mathf.Abs(dir.y),
                    Mathf.Abs(dir.z)) * w;
                offset += dir * (v.length * 0.25f) * w;
                totalWeight += w;
            }

            if (totalWeight <= 1e-4f)
                return;

            stretch /= totalWeight;
            offset /= totalWeight;
            center = focus + offset;
            radii = new Vector3(
                defaultRadii.x * (1f + stretch.x * 0.5f),
                defaultRadii.y * (1f + stretch.y * 0.35f),
                defaultRadii.z * (1f + stretch.z * 0.5f));
        }
    }
}
