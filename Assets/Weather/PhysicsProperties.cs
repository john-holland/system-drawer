using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Material properties for physics calculations.
    /// Defines density, viscosity, specific heat, thermal conductivity, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicsProperties", menuName = "Weather/Physics Properties")]
    public class PhysicsProperties : ScriptableObject
    {
        [Header("Material Properties")]
        [Tooltip("Density in kg/m³")]
        public float density = 1.225f; // Air at sea level

        [Tooltip("Viscosity in Pa·s (Pascal-seconds)")]
        public float viscosity = 0.0000181f; // Air at 20°C

        [Tooltip("Specific heat in J/(kg·K)")]
        public float specificHeat = 1005f; // Air at constant pressure

        [Tooltip("Thermal conductivity in W/(m·K)")]
        public float thermalConductivity = 0.025f; // Air at 20°C

        // Standard Material Presets (static properties, not shown in Inspector)
        public static PhysicsProperties Air => CreateAir();
        public static PhysicsProperties Water => CreateWater();
        public static PhysicsProperties Steam => CreateSteam();

        private static PhysicsProperties CreateAir()
        {
            var props = CreateInstance<PhysicsProperties>();
            props.density = 1.225f;
            props.viscosity = 0.0000181f;
            props.specificHeat = 1005f;
            props.thermalConductivity = 0.025f;
            return props;
        }

        private static PhysicsProperties CreateWater()
        {
            var props = CreateInstance<PhysicsProperties>();
            props.density = 1000f;
            props.viscosity = 0.001f;
            props.specificHeat = 4182f;
            props.thermalConductivity = 0.6f;
            return props;
        }

        private static PhysicsProperties CreateSteam()
        {
            var props = CreateInstance<PhysicsProperties>();
            props.density = 0.6f; // At 100°C
            props.viscosity = 0.000012f;
            props.specificHeat = 2010f;
            props.thermalConductivity = 0.025f;
            return props;
        }
    }
}
