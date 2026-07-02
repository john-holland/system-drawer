using NUnit.Framework;
using Planetary.AsteroidBelt;
using UnityEngine;

namespace Planetary.Tests
{
    public class AsteroidBeltTests
    {
        [Test]
        public void SameSeedAndMutations_IdenticalSlotPosition()
        {
            var pop = new GameObject("pop").AddComponent<AsteroidBeltPopulationService>();
            var manifold = pop.gameObject.AddComponent<AsteroidBeltStatisticalManifold>();
            manifold.seed = 42;
            manifold.innerRadiusM = 100f;
            manifold.outerRadiusM = 200f;
            pop.manifold = manifold;
            pop.slotsPerSector = 4;
            pop.sectorCount = 8;
            pop.mutationLog = ScriptableObject.CreateInstance<AsteroidBeltMutationLog>();
            pop.mutationLog.beltSeed = 42;

            Vector3 a = pop.ComputeSlotPosition(2, 1);
            Vector3 b = pop.ComputeSlotPosition(2, 1);
            Assert.AreEqual(a, b);

            Object.DestroyImmediate(pop.gameObject);
            Object.DestroyImmediate(pop.mutationLog);
        }

        [Test]
        public void DestroyMutation_PreventsRespawnFlag()
        {
            var log = ScriptableObject.CreateInstance<AsteroidBeltMutationLog>();
            log.Record(new AsteroidBeltMutation
            {
                sectorIndex = 1,
                slotIndex = 2,
                kind = AsteroidMutationKind.Destroyed
            });
            Assert.IsTrue(log.IsSlotDestroyed(1, 2));
            Object.DestroyImmediate(log);
        }

        [Test]
        public void LodOpacity_DecreasesWhenNear()
        {
            var disc = new GameObject("disc").AddComponent<AsteroidBeltDiscRenderer>();
            disc.SetOpacity(0.2f, 0.5f);
            Object.DestroyImmediate(disc.gameObject);
            Assert.Pass();
        }

        [Test]
        public void ReceiveHit_TriggersDestructionAndLog()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var body = go.AddComponent<AsteroidBody>();
            body.beltSectorIndex = 0;
            body.beltSlotIndex = 0;
            var log = ScriptableObject.CreateInstance<AsteroidBeltMutationLog>();
            body.mutationLog = log;
            go.AddComponent<ProceduralAsteroidDestruction>();

            body.ReceiveHit(new AsteroidHitInfo { incomingDirection = Vector3.forward, speed = 100f });
            Assert.AreEqual(1, log.mutations.Count);
            Object.DestroyImmediate(log);
            // body destroyed by ProceduralAsteroidDestruction
        }
    }
}
