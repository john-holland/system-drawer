using UnityEngine;
using System;
using System.Reflection;

namespace Weather
{
    /// <summary>
    /// Component that adjusts global illumination with a spotlight based on narrative calendar date.
    /// Calculates sun angle from latitude/longitude and time of day.
    /// Uses reflection to access NarrativeClock to avoid compile-time dependency cycles.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class GlobalIlluminationController : MonoBehaviour
    {
        [Header("Light Reference")]
        [Tooltip("Directional light used for sun simulation (auto-found if null)")]
        public Light directionalLight;

        [Header("Location")]
        [Tooltip("Latitude in degrees (-90 to 90)")]
        [Range(-90f, 90f)]
        public float latitude = 40f; // Default: New York

        [Tooltip("Longitude in degrees (-180 to 180)")]
        [Range(-180f, 180f)]
        public float longitude = -74f; // Default: New York

        [Header("Narrative Time")]
        [Tooltip("Reference to narrative clock GameObject (auto-found if null, uses reflection to avoid compile-time dependency)")]
        public GameObject narrativeClockObject;

        [Tooltip("Use narrative time instead of system time")]
        public bool useNarrativeTime = true;

        [Header("Sun Settings")]
        [Tooltip("Sun light intensity")]
        [Range(0f, 8f)]
        public float sunIntensity = 1f;

        [Tooltip("Sun light color")]
        public Color sunColor = Color.white;

        [Header("Update Settings")]
        [Tooltip("Update interval in seconds (0 = every frame)")]
        [Range(0f, 60f)]
        public float updateInterval = 0f;

        // Internal state (using object to avoid compile-time dependency)
        private float lastUpdateTime = 0f;
        private object lastDateTime;
        private object narrativeClockComponent; // NarrativeClock via reflection
        private Type narrativeDateTimeType;
        private Type narrativeClockType;
        private PropertyInfo nowProperty;
        private FieldInfo yearField, monthField, dayField, hourField, minuteField, secondField;

        private void Awake()
        {
            // Auto-find directional light if not assigned
            if (directionalLight == null)
            {
                directionalLight = GetComponent<Light>();
                if (directionalLight == null)
                {
                    // Try to find a directional light in the scene
                    Light[] lights = FindObjectsOfType<Light>();
                    foreach (var light in lights)
                    {
                        if (light.type == LightType.Directional)
                        {
                            directionalLight = light;
                            break;
                        }
                    }
                }
            }

            // Set light type to directional if not already
            if (directionalLight != null && directionalLight.type != LightType.Directional)
            {
                directionalLight.type = LightType.Directional;
            }

            // Initialize reflection types for NarrativeClock
            InitializeNarrativeTypes();
        }

        private void InitializeNarrativeTypes()
        {
            // Use reflection to access NarrativeClock and NarrativeDateTime types
            narrativeClockType = Type.GetType("Locomotion.Narrative.NarrativeClock, Locomotion.Narrative.Runtime");
            narrativeDateTimeType = Type.GetType("Locomotion.Narrative.NarrativeDateTime, Locomotion.Narrative.Runtime");

            if (narrativeClockType != null)
            {
                nowProperty = narrativeClockType.GetProperty("Now");
            }

            if (narrativeDateTimeType != null)
            {
                yearField = narrativeDateTimeType.GetField("year");
                monthField = narrativeDateTimeType.GetField("month");
                dayField = narrativeDateTimeType.GetField("day");
                hourField = narrativeDateTimeType.GetField("hour");
                minuteField = narrativeDateTimeType.GetField("minute");
                secondField = narrativeDateTimeType.GetField("second");
            }

            // Auto-find narrative clock if not assigned
            if (narrativeClockObject == null && useNarrativeTime && narrativeClockType != null)
            {
                MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allMonoBehaviours)
                {
                    if (narrativeClockType.IsAssignableFrom(mb.GetType()))
                    {
                        narrativeClockObject = mb.gameObject;
                        narrativeClockComponent = mb;
                        break;
                    }
                }
            }
            else if (narrativeClockObject != null)
            {
                narrativeClockComponent = narrativeClockObject.GetComponent(narrativeClockType);
            }
        }

        private void Start()
        {
            UpdateSunPosition();
        }

        private void Update()
        {
            // Check if we need to update
            if (updateInterval > 0f)
            {
                if (Time.time - lastUpdateTime < updateInterval)
                {
                    return;
                }
            }

            // Check if time has changed (for narrative time)
            if (useNarrativeTime && narrativeClockComponent != null && nowProperty != null)
            {
                object currentDateTime = nowProperty.GetValue(narrativeClockComponent);
                if (!AreDateTimesEqual(currentDateTime, lastDateTime))
                {
                    UpdateSunPosition();
                    lastDateTime = currentDateTime;
                }
            }
            else if (!useNarrativeTime)
            {
                // Update every frame or based on interval
                UpdateSunPosition();
            }

            lastUpdateTime = Time.time;
        }

        private bool AreDateTimesEqual(object dt1, object dt2)
        {
            if (dt1 == null && dt2 == null) return true;
            if (dt1 == null || dt2 == null) return false;
            if (dt1.GetType() != dt2.GetType()) return false;

            // Compare fields
            return (int)yearField.GetValue(dt1) == (int)yearField.GetValue(dt2) &&
                   (int)monthField.GetValue(dt1) == (int)monthField.GetValue(dt2) &&
                   (int)dayField.GetValue(dt1) == (int)dayField.GetValue(dt2) &&
                   (int)hourField.GetValue(dt1) == (int)hourField.GetValue(dt2) &&
                   (int)minuteField.GetValue(dt1) == (int)minuteField.GetValue(dt2) &&
                   (int)secondField.GetValue(dt1) == (int)secondField.GetValue(dt2);
        }

        /// <summary>
        /// Calculate sun angle for given date/time (using reflection to access NarrativeDateTime).
        /// </summary>
        public Vector3 CalculateSunAngle(object dateTime)
        {
            if (dateTime == null || narrativeDateTimeType == null)
            {
                // Fallback to system time
                System.DateTime now = System.DateTime.Now;
                return CalculateSunAngleFromSystemTime(now);
            }

            int year = (int)yearField.GetValue(dateTime);
            int month = (int)monthField.GetValue(dateTime);
            int day = (int)dayField.GetValue(dateTime);
            int hour = (int)hourField.GetValue(dateTime);
            int minute = (int)minuteField.GetValue(dateTime);
            int second = (int)secondField.GetValue(dateTime);

            return CalculateSunAngleFromValues(year, month, day, hour, minute, second);
        }

        private Vector3 CalculateSunAngleFromSystemTime(System.DateTime dateTime)
        {
            return CalculateSunAngleFromValues(dateTime.Year, dateTime.Month, dateTime.Day, 
                dateTime.Hour, dateTime.Minute, dateTime.Second);
        }

        private Vector3 CalculateSunAngleFromValues(int year, int month, int day, int hour, int minute, int second)
        {
            // Calculate day of year (1-365)
            int dayOfYear = GetDayOfYear(year, month, day);

            // Calculate solar declination (angle of sun relative to equator)
            float declination = 23.45f * Mathf.Sin(Mathf.Deg2Rad * 360f * (284f + dayOfYear) / 365f);

            // Calculate hour angle (time of day in degrees)
            float solarTime = hour + minute / 60f + second / 3600f;
            float hourAngle = 15f * (solarTime - 12f); // 15 degrees per hour, noon = 0

            // Convert to radians
            float latRad = Mathf.Deg2Rad * latitude;
            float declRad = Mathf.Deg2Rad * declination;
            float hourRad = Mathf.Deg2Rad * hourAngle;

            // Calculate altitude (elevation angle above horizon)
            float sinAltitude = Mathf.Sin(latRad) * Mathf.Sin(declRad) + 
                               Mathf.Cos(latRad) * Mathf.Cos(declRad) * Mathf.Cos(hourRad);
            float altitude = Mathf.Asin(Mathf.Clamp(sinAltitude, -1f, 1f));

            // Calculate azimuth (compass direction, 0 = North, 90 = East)
            float sinAzimuth = Mathf.Sin(hourRad);
            float cosAzimuth = Mathf.Cos(hourRad) * Mathf.Sin(latRad) - Mathf.Tan(declRad) * Mathf.Cos(latRad);
            float azimuth = Mathf.Atan2(sinAzimuth, cosAzimuth);

            // Convert to Unity coordinate system
            // Unity: Y-up, Z-forward, X-right
            // Altitude: angle from horizon (0 = horizon, 90 = zenith)
            // Azimuth: angle from North (0 = North, 90 = East, 180 = South, 270 = West)
            
            // Convert to Unity rotation
            // X rotation = altitude (up/down)
            // Y rotation = azimuth (compass direction)
            float altitudeDeg = Mathf.Rad2Deg * altitude;
            float azimuthDeg = Mathf.Rad2Deg * azimuth;

            // Unity rotation: X = pitch (altitude), Y = yaw (azimuth), Z = roll
            return new Vector3(altitudeDeg, azimuthDeg, 0f);
        }

        /// <summary>
        /// Update sun position and rotation based on current time.
        /// </summary>
        public void UpdateSunPosition()
        {
            if (directionalLight == null)
                return;

            object dateTime = null;
            if (useNarrativeTime && narrativeClockComponent != null && nowProperty != null)
            {
                dateTime = nowProperty.GetValue(narrativeClockComponent);
            }
            else
            {
                // Use system time - will be handled in CalculateSunAngle
            }

            Vector3 sunAngle;
            if (dateTime != null)
            {
                sunAngle = CalculateSunAngle(dateTime);
            }
            else
            {
                // Use system time
                System.DateTime now = System.DateTime.Now;
                sunAngle = CalculateSunAngleFromSystemTime(now);
            }

            // Set light rotation
            // Unity's directional light points in the direction of its forward (Z) axis
            // We need to rotate so the light points toward the sun position
            Quaternion rotation = Quaternion.Euler(sunAngle.x, sunAngle.y, 0f);
            directionalLight.transform.rotation = rotation;

            // Set light properties
            directionalLight.intensity = sunIntensity;
            directionalLight.color = sunColor;
        }

        /// <summary>
        /// Event handler for narrative time changes (can be called by NarrativeScheduler via reflection).
        /// </summary>
        public void OnNarrativeTimeChanged(object newTime)
        {
            UpdateSunPosition();
        }

        /// <summary>
        /// Get day of year (1-365/366).
        /// </summary>
        private int GetDayOfYear(int year, int month, int day)
        {
            int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            
            // Check for leap year
            if (IsLeapYear(year))
            {
                daysInMonth[1] = 29;
            }

            int dayOfYear = day;
            for (int i = 0; i < month - 1; i++)
            {
                dayOfYear += daysInMonth[i];
            }

            return dayOfYear;
        }

        /// <summary>
        /// Check if year is a leap year.
        /// </summary>
        private bool IsLeapYear(int year)
        {
            return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
        }
    }
}
