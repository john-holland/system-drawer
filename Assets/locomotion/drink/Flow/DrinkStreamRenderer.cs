using Locomotion.Liquid;

using UnityEngine;

using Weather;



namespace Locomotion.Drink.Flow

{

    /// <summary>Particle stream visual driven by flow bake curves, live flow model, and weather manifold.</summary>

    public sealed class DrinkStreamRenderer : MonoBehaviour

    {

        public DrinkNozzleComponent nozzle;

        public DrinkFlowModel flowModel;

        public DrinkFlowBakeAsset bakeAsset;

        public ParticleSystem streamParticles;

        public Material streamMaterial;

        public WeatherPhysicsManifold weatherManifold;

        float _playbackTime;

        void Awake()
        {
            if (streamParticles == null)
                streamParticles = GetComponent<ParticleSystem>();
            if (weatherManifold == null)
                weatherManifold = FindAnyObjectByType<WeatherPhysicsManifold>();
        }

        void Update()
        {
            if (flowModel == null || streamParticles == null)
                return;

            float q = flowModel.ComputeInstantaneousFlowLitersPerSecond();
            if (bakeAsset != null && bakeAsset.flowLitersPerSecond != null)
            {
                _playbackTime += Time.deltaTime;
                q = Mathf.Max(q, bakeAsset.flowLitersPerSecond.Evaluate(_playbackTime % 1f));
            }

            var emission = streamParticles.emission;

            emission.rateOverTime = q * 800f;



            Vector3 force = flowModel.nozzle != null

                ? flowModel.nozzle.TipForward * q

                : flowModel.StreamTipForward() * q;

            flowModel.SyncManifoldVelocity(force);



            if (weatherManifold != null)

            {

                var sample = weatherManifold.GetDataAtPosition(flowModel.StreamTipPosition());

                if (sample.velocity.sqrMagnitude > 1e-6f)

                    force = Vector3.Lerp(force, sample.velocity, 0.35f);

            }



            if (streamMaterial != null)

            {

                streamMaterial.SetVector("_StreamForce", force);

                streamMaterial.SetFloat("_Pressure", flowModel.handPressurePa);

            }

        }

    }

}


