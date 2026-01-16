using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Weather shader library for exposing WeatherPhysicsManifold data to shaders.
    /// Provides utilities for water rendering, cloud rendering, and weather effects.
    /// </summary>
    public static class WeatherShaderLibrary
    {
        // Shader property names
        public static readonly int WeatherDataTexture = Shader.PropertyToID("_WeatherDataTexture");
        public static readonly int WeatherDataBuffer = Shader.PropertyToID("_WeatherDataBuffer");
        public static readonly int WeatherBounds = Shader.PropertyToID("_WeatherBounds");
        public static readonly int WeatherCellResolution = Shader.PropertyToID("_WeatherCellResolution");
        public static readonly int WeatherCellCount = Shader.PropertyToID("_WeatherCellCount");
        public static readonly int WindVelocity = Shader.PropertyToID("_WindVelocity");
        public static readonly int WindDirection = Shader.PropertyToID("_WindDirection");
        public static readonly int Temperature = Shader.PropertyToID("_Temperature");
        public static readonly int Pressure = Shader.PropertyToID("_Pressure");
        public static readonly int Humidity = Shader.PropertyToID("_Humidity");

        /// <summary>
        /// Set up shader properties from WeatherPhysicsManifold
        /// </summary>
        public static void SetupShaderProperties(WeatherPhysicsManifold manifold, Material material)
        {
            if (manifold == null || material == null)
                return;

            ShaderParameters shaderParams = manifold.GetShaderParameters();
            
            // Set bounds
            material.SetVector(WeatherBounds, new Vector4(
                shaderParams.bounds.center.x,
                shaderParams.bounds.center.y,
                shaderParams.bounds.center.z,
                shaderParams.bounds.size.magnitude
            ));

            // Set cell resolution
            material.SetFloat(WeatherCellResolution, shaderParams.cellResolution);

            // Set cell count
            material.SetVector(WeatherCellCount, new Vector4(
                shaderParams.cellCount.x,
                shaderParams.cellCount.y,
                shaderParams.cellCount.z,
                0f
            ));

            // Note: Texture/buffer setup would be done here
            // This requires creating a RenderTexture or ComputeBuffer from manifold data
        }

        /// <summary>
        /// Set up shader properties from Wind system
        /// </summary>
        public static void SetupWindProperties(Wind wind, Material material)
        {
            if (wind == null || material == null)
                return;

            Vector3 windVector = wind.GetWindAtPosition(Vector3.zero, 0f);
            material.SetVector(WindVelocity, windVector);
            material.SetFloat(WindDirection, wind.direction);
        }

        /// <summary>
        /// Set up shader properties from Meteorology system
        /// </summary>
        public static void SetupMeteorologyProperties(Meteorology meteorology, Material material)
        {
            if (meteorology == null || material == null)
                return;

            material.SetFloat(Temperature, meteorology.temperature);
            material.SetFloat(Pressure, meteorology.pressure);
            material.SetFloat(Humidity, meteorology.humidity);
        }

        /// <summary>
        /// Get weather data at position for shader sampling
        /// </summary>
        public static Vector4 GetWeatherDataAtPosition(WeatherPhysicsManifold manifold, Vector3 position)
        {
            if (manifold == null)
                return Vector4.zero;

            ManifoldCellData data = manifold.GetDataAtPosition(position);
            return new Vector4(
                data.velocity.x,
                data.velocity.y,
                data.velocity.z,
                data.pressure
            );
        }
    }
}
