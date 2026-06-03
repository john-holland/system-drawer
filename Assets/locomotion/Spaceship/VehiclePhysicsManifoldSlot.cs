using UnityEngine;

namespace Locomotion.Spaceship
{
    public enum VehicleManifoldSlotKind
    {
        Engine,
        Fuselage,
        Wing,
        Weapon
    }

    public sealed class VehiclePhysicsManifoldSlot : MonoBehaviour
    {
        public VehicleManifoldSlotKind kind;
        public float wingAspectRatio = 8f;
        public float wingAreaSqMeters = 40f;
        public float thrustNewtons;
        public float radiationEmit;
    }
}
