using UnityEngine;

namespace Locomotion.Spaceship
{
    public sealed class SpacecraftControlProxy : MonoBehaviour
    {
        public VehicleActor vehicle;
        public Transform desiredRotationTarget;
        public float impulseStrength = 10f;

        public void ApplyPilotInput(Vector3 worldDirection, float throttle)
        {
            if (vehicle == null)
                return;
            var rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(worldDirection.normalized * impulseStrength * throttle, ForceMode.Force);
            if (desiredRotationTarget != null)
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotationTarget.rotation, Time.deltaTime * 2f);
        }
    }
}
