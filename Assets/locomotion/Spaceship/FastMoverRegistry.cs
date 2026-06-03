using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Spaceship
{
    public sealed class FastMoverRegistry : MonoBehaviour
    {
        public struct Entry
        {
            public Transform Transform;
            public bool IsShipPort;
            public float MaxSpeed;
        }

        readonly List<Entry> _entries = new List<Entry>();

        public void Register(Transform t, bool shipPort, float maxSpeed) =>
            _entries.Add(new Entry { Transform = t, IsShipPort = shipPort, MaxSpeed = maxSpeed });

        public bool TryGetNearest(Vector3 world, bool shipPortOnly, out Entry entry)
        {
            entry = default;
            float best = float.MaxValue;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (shipPortOnly && !_entries[i].IsShipPort)
                    continue;
                float d = Vector3.Distance(world, _entries[i].Transform.position);
                if (d < best)
                {
                    best = d;
                    entry = _entries[i];
                }
            }
            return best < float.MaxValue;
        }
    }
}
