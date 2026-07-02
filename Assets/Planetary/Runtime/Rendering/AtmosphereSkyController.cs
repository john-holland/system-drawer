using Planetary.Celestial;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Day/night atmosphere: live sun disk + blended night cubemap.</summary>
    public sealed class AtmosphereSkyController : MonoBehaviour
    {
        public Material skyMaterial;
        public AtmosphereRegressionProfile atmosphereProfile;
        public float twilightDegrees = 6f;

        StarBody _nearestStar;

        void Update()
        {
            if (skyMaterial == null)
                return;
            _nearestStar = FindNearestStar();
            float solarElev = ComputeSolarElevation();
            float dayNight = Mathf.InverseLerp(-twilightDegrees, twilightDegrees, solarElev);
            skyMaterial.SetFloat("_DayNightBlend", dayNight);
            skyMaterial.SetFloat("_RayleighBlue", atmosphereProfile != null ? atmosphereProfile.cloudDensityCoeff : 0.35f);

            if (_nearestStar != null && _nearestStar.renderProfile != null && _nearestStar.renderProfile.bypassBakeForNearbySun)
            {
                Vector3 sunDir = (_nearestStar.transform.position - transform.position).normalized;
                skyMaterial.SetVector("_SunDirection", sunDir);
                skyMaterial.SetColor("_SunColor", _nearestStar.renderProfile.color * _nearestStar.renderProfile.superSaturation);
                skyMaterial.SetFloat("_SunIntensity", _nearestStar.renderProfile.intensity);
            }
        }

        StarBody FindNearestStar()
        {
            StarBody best = null;
            float d = float.MaxValue;
            var stars = FindObjectsByType<StarBody>(FindObjectsSortMode.None);
            for (int i = 0; i < stars.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, stars[i].transform.position);
                if (dist < d)
                {
                    d = dist;
                    best = stars[i];
                }
            }
            return best;
        }

        float ComputeSolarElevation()
        {
            if (_nearestStar == null)
                return -90f;
            Vector3 toSun = (_nearestStar.transform.position - transform.position).normalized;
            Vector3 up = transform.up;
            return Mathf.Asin(Mathf.Clamp(Vector3.Dot(toSun, up), -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
