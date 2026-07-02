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
        readonly List<IFastMoverTarget> _targets = new List<IFastMoverTarget>();

        public void Register(Transform t, bool shipPort, float maxSpeed) =>
            _entries.Add(new Entry { Transform = t, IsShipPort = shipPort, MaxSpeed = maxSpeed });

        public void RegisterTarget(IFastMoverTarget target)
        {
            if (target != null && !_targets.Contains(target))
                _targets.Add(target);
        }

        public void UnregisterTarget(IFastMoverTarget target) => _targets.Remove(target);

        public bool TryGetNearestTarget(Vector3 world, out IFastMoverTarget target)
        {
            target = null;
            float best = float.MaxValue;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i]?.TargetTransform == null)
                    continue;
                float d = Vector3.Distance(world, _targets[i].TargetTransform.position);
                if (d < best)
                {
                    best = d;
                    target = _targets[i];
                }
            }
            return target != null;
        }

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
