using UnityEngine;

namespace DestructibleEnvironment
{
    public struct DestructibleImpactContext
    {
        public Vector3 worldPoint;
        public float impulseN;
        public Vector3 impulseDir;
        public Vector3 gravityDir;
        public PhysicsMaterial colliderMaterial;

        public static DestructibleImpactContext FromCollision(Collision collision, Vector3 gravityDir)
        {
            var ctx = new DestructibleImpactContext
            {
                gravityDir = gravityDir.sqrMagnitude > 1e-6f ? gravityDir.normalized : Vector3.down,
                impulseN = collision.impulse.magnitude,
                impulseDir = collision.impulse.sqrMagnitude > 1e-6f ? collision.impulse.normalized : Vector3.forward
            };

            if (collision.contactCount > 0)
            {
                ContactPoint cp = collision.GetContact(0);
                ctx.worldPoint = cp.point;
                if (collision.collider != null)
                    ctx.colliderMaterial = collision.collider.sharedMaterial;
            }
            else
            {
                ctx.worldPoint = collision.transform.position;
            }

            return ctx;
        }
    }
}
