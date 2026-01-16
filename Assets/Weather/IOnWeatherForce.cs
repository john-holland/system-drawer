using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Interface for objects that can receive custom weather force handling.
    /// Allows objects to override default wind force behavior.
    /// </summary>
    public interface IOnWeatherForce
    {
        /// <summary>
        /// Called when weather force should be applied to this object.
        /// </summary>
        /// <param name="force">The weather force vector</param>
        /// <param name="forceType">Type of force (wind, tornado, etc.)</param>
        /// <returns>True if force was handled, false to use default behavior</returns>
        bool OnWeatherForce(Vector3 force, WeatherEventType forceType);
    }
}
