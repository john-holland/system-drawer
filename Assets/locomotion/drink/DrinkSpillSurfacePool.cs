using System.Collections.Generic;
using Locomotion.Liquid;
using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Cosmetic spill stains and particles on tray/seat/lap.</summary>
    public sealed class DrinkSpillSurfacePool : MonoBehaviour
    {
        public LiquidWeatherManifoldBridge weatherBridge;
        public LayerMask surfaceMask = ~0;
        public int maxStains = 32;
        public Material stainMaterial;

        readonly List<GameObject> _stains = new List<GameObject>();

        public void SpawnSpill(Vector3 origin, float liters)
        {
            if (liters <= 0f)
                return;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, surfaceMask))
            {
                weatherBridge?.PaintSpillFootprint(hit.point, liters, Vector3.down * 0.2f);
                CreateStainQuad(hit.point, hit.normal, liters);
            }
        }

        void CreateStainQuad(Vector3 point, Vector3 normal, float liters)
        {
            while (_stains.Count >= maxStains && _stains.Count > 0)
            {
                var old = _stains[0];
                _stains.RemoveAt(0);
                if (old != null)
                    Destroy(old);
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpillStain";
            float size = Mathf.Clamp(Mathf.Sqrt(liters) * 0.15f, 0.03f, 0.25f);
            go.transform.position = point + normal * 0.002f;
            go.transform.rotation = Quaternion.LookRotation(normal);
            go.transform.localScale = Vector3.one * size;
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var rend = go.GetComponent<Renderer>();
            if (rend != null && stainMaterial != null)
                rend.sharedMaterial = stainMaterial;
            _stains.Add(go);
        }
    }
}
