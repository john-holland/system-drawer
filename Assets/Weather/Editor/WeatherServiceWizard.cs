using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Weather
{
    /// <summary>
    /// Interactive wizard for setting up and validating the weather system.
    /// Guides users through creating/linking required GameObjects and components.
    /// </summary>
    public class WeatherServiceWizard : EditorWindow
    {
        // Main Weather Object components
        private GameObject mainWeatherObject;
        private WeatherSystem weatherSystem;
        private Meteorology meteorology;
        private Precipitation precipitation;
        private WeatherPhysicsManifold weatherPhysicsManifold;
        private PhysicsManifold physicsManifold;

        // Subsystem GameObjects
        private GameObject cloudObject;
        private GameObject windObject;
        private GameObject terrainObject;
        private GameObject waterObject;
        private GameObject rainObject;

        // UI State
        private Vector2 scrollPosition;
        private bool readmeExpanded = true;
        private bool mainObjectExpanded = true;
        private bool subsystemsExpanded = true;
        private bool settingsExpanded = false;
        private bool validationExpanded = true;

        // Settings expanded states
        private bool meteorologySettingsExpanded = false;
        private bool windSettingsExpanded = false;
        private bool precipitationSettingsExpanded = false;
        private bool weatherPhysicsManifoldSettingsExpanded = false;
        private bool waterSettingsExpanded = false;
        private bool cloudSettingsExpanded = false;

        // Validation report
        private ValidationReport validationReport;

        // Preset configurations
        private enum WeatherPreset
        {
            ClearDay,
            Storm,
            Winter
        }
        private WeatherPreset selectedPreset = WeatherPreset.ClearDay;

        [MenuItem("Window/Weather/Weather Service Wizard")]
        public static void ShowWindow()
        {
            WeatherServiceWizard window = GetWindow<WeatherServiceWizard>("Weather Service Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            FindExistingComponents();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Title
            EditorGUILayout.Space();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            EditorGUILayout.LabelField("Weather Service Wizard", titleStyle);
            EditorGUILayout.Space();

            // Interactive Readme Section
            DrawReadmeSection();

            EditorGUILayout.Space(10);

            // Step 1: Main Weather Object
            DrawMainWeatherObjectSection();

            EditorGUILayout.Space(10);

            // Step 2: Subsystem Objects
            DrawSubsystemObjectsSection();

            EditorGUILayout.Space(10);

            // Step 3: Component Settings
            DrawSettingsSection();

            EditorGUILayout.Space(10);

            // Step 4: Validation Checklist
            DrawValidationSection();

            EditorGUILayout.Space(10);

            // Auto-Setup and Presets
            DrawAutoSetupSection();

            EditorGUILayout.EndScrollView();

            // Auto-validate on changes
            if (GUI.changed)
            {
                ValidateSetup();
            }
        }

        private void DrawReadmeSection()
        {
            readmeExpanded = EditorGUILayout.Foldout(readmeExpanded, "📖 Interactive Readme", true);
            if (!readmeExpanded)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The Weather System is a physics-based weather simulation that integrates with Unity's physics system. " +
                "It consists of a main weather object with 5 core components, plus 5 subsystem GameObjects for specialized functionality.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Service Update Order", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Meteorology → 2. Wind → 3. Precipitation → 4. Water → 5. Cloud → 6. WeatherPhysicsManifold\n\n" +
                "This order ensures proper data flow and dependencies between systems.",
                MessageType.None
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Component Relationships", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• WeatherSystem orchestrates all subsystems\n" +
                "• Meteorology controls atmospheric conditions\n" +
                "• Wind affects clouds, precipitation, and physics objects\n" +
                "• Precipitation feeds into Water system\n" +
                "• Water manages rivers, ponds, and dams\n" +
                "• Cloud visualizes pressure systems\n" +
                "• WeatherPhysicsManifold aggregates all data for shaders",
                MessageType.None
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Create/Link Main Weather Object with all 5 components\n" +
                "2. Create/Link 5 subsystem GameObjects\n" +
                "3. Configure critical settings\n" +
                "4. Run validation checklist\n" +
                "5. Test the system",
                MessageType.None
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Troubleshooting", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Missing references: Use 'Create GameObject' buttons or link existing objects\n" +
                "• Null reference errors: Check that all subsystem references are linked in WeatherSystem\n" +
                "• No weather effects: Verify WeatherSystem is enabled and updating\n" +
                "• Gizmos not showing: Enable 'showGizmos' on components",
                MessageType.Warning
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawMainWeatherObjectSection()
        {
            mainObjectExpanded = EditorGUILayout.Foldout(mainObjectExpanded, "Step 1: Main Weather Object", true);
            if (!mainObjectExpanded)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "The main weather object should have all 5 core components. " +
                "Link an existing GameObject or create a new one.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // Main Weather Object
            EditorGUILayout.BeginHorizontal();
            mainWeatherObject = (GameObject)EditorGUILayout.ObjectField(
                "Main Weather Object",
                mainWeatherObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Main Weather Object", GUILayout.Width(200)))
            {
                mainWeatherObject = CreateMainWeatherObject();
            }
            EditorGUILayout.EndHorizontal();

            if (mainWeatherObject != null)
            {
                EditorGUI.indentLevel++;

                // WeatherSystem
                EditorGUILayout.BeginHorizontal();
                weatherSystem = (WeatherSystem)EditorGUILayout.ObjectField(
                    "WeatherSystem",
                    weatherSystem ?? mainWeatherObject.GetComponent<WeatherSystem>(),
                    typeof(WeatherSystem),
                    true
                );
                if (GUILayout.Button("Create WeatherSystem", GUILayout.Width(200)))
                {
                    weatherSystem = AddOrGetComponent<WeatherSystem>(mainWeatherObject);
                }
                EditorGUILayout.EndHorizontal();

                // Meteorology
                EditorGUILayout.BeginHorizontal();
                meteorology = (Meteorology)EditorGUILayout.ObjectField(
                    "Meteorology",
                    meteorology ?? mainWeatherObject.GetComponent<Meteorology>(),
                    typeof(Meteorology),
                    true
                );
                if (GUILayout.Button("Create Meteorology", GUILayout.Width(200)))
                {
                    meteorology = AddOrGetComponent<Meteorology>(mainWeatherObject);
                }
                EditorGUILayout.EndHorizontal();

                // Precipitation
                EditorGUILayout.BeginHorizontal();
                precipitation = (Precipitation)EditorGUILayout.ObjectField(
                    "Precipitation",
                    precipitation ?? mainWeatherObject.GetComponent<Precipitation>(),
                    typeof(Precipitation),
                    true
                );
                if (GUILayout.Button("Create Precipitation", GUILayout.Width(200)))
                {
                    precipitation = AddOrGetComponent<Precipitation>(mainWeatherObject);
                }
                EditorGUILayout.EndHorizontal();

                // WeatherPhysicsManifold
                EditorGUILayout.BeginHorizontal();
                weatherPhysicsManifold = (WeatherPhysicsManifold)EditorGUILayout.ObjectField(
                    "WeatherPhysicsManifold",
                    weatherPhysicsManifold ?? mainWeatherObject.GetComponent<WeatherPhysicsManifold>(),
                    typeof(WeatherPhysicsManifold),
                    true
                );
                if (GUILayout.Button("Create WeatherPhysicsManifold", GUILayout.Width(200)))
                {
                    weatherPhysicsManifold = AddOrGetComponent<WeatherPhysicsManifold>(mainWeatherObject);
                }
                EditorGUILayout.EndHorizontal();

                // PhysicsManifold
                EditorGUILayout.BeginHorizontal();
                physicsManifold = (PhysicsManifold)EditorGUILayout.ObjectField(
                    "PhysicsManifold",
                    physicsManifold ?? mainWeatherObject.GetComponent<PhysicsManifold>(),
                    typeof(PhysicsManifold),
                    true
                );
                if (GUILayout.Button("Create PhysicsManifold", GUILayout.Width(200)))
                {
                    physicsManifold = AddOrGetComponent<PhysicsManifold>(mainWeatherObject);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                // Auto-link button
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Auto-Link All References", GUILayout.Height(25)))
                {
                    AutoLinkReferences();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSubsystemObjectsSection()
        {
            subsystemsExpanded = EditorGUILayout.Foldout(subsystemsExpanded, "Step 2: Subsystem Objects", true);
            if (!subsystemsExpanded)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Create or link 5 separate GameObjects for specialized weather subsystems.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // Cloud GameObject
            EditorGUILayout.BeginHorizontal();
            cloudObject = (GameObject)EditorGUILayout.ObjectField(
                "Cloud GameObject",
                cloudObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Cloud GameObject", GUILayout.Width(200)))
            {
                cloudObject = CreateSubsystemObject<Cloud>("Cloud");
            }
            EditorGUILayout.EndHorizontal();

            // Wind GameObject
            EditorGUILayout.BeginHorizontal();
            windObject = (GameObject)EditorGUILayout.ObjectField(
                "Wind GameObject",
                windObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Wind GameObject", GUILayout.Width(200)))
            {
                windObject = CreateSubsystemObject<Wind>("Wind");
            }
            EditorGUILayout.EndHorizontal();

            // Terrain GameObject
            EditorGUILayout.BeginHorizontal();
            terrainObject = (GameObject)EditorGUILayout.ObjectField(
                "Terrain GameObject",
                terrainObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Terrain GameObject", GUILayout.Width(200)))
            {
                terrainObject = CreateTerrainObject();
            }
            EditorGUILayout.EndHorizontal();

            // Water GameObject
            EditorGUILayout.BeginHorizontal();
            waterObject = (GameObject)EditorGUILayout.ObjectField(
                "Water GameObject",
                waterObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Water GameObject", GUILayout.Width(200)))
            {
                waterObject = CreateSubsystemObject<Water>("Water");
            }
            EditorGUILayout.EndHorizontal();

            // Rain GameObject (optional, can share Precipitation with main)
            EditorGUILayout.BeginHorizontal();
            rainObject = (GameObject)EditorGUILayout.ObjectField(
                "Rain GameObject (Optional)",
                rainObject,
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("Create Rain GameObject", GUILayout.Width(200)))
            {
                rainObject = CreateSubsystemObject<Precipitation>("Rain");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsSection()
        {
            settingsExpanded = EditorGUILayout.Foldout(settingsExpanded, "Step 3: Component Settings", true);
            if (!settingsExpanded)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Configure important settings for each component. Critical settings are highlighted.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // Meteorology Settings
            if (meteorology != null)
            {
                meteorologySettingsExpanded = EditorGUILayout.Foldout(meteorologySettingsExpanded, "Meteorology Settings", true);
                if (meteorologySettingsExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawMeteorologySettings();
                    EditorGUI.indentLevel--;
                }
            }

            // Wind Settings
            if (windObject != null)
            {
                Wind wind = windObject.GetComponent<Wind>();
                if (wind != null)
                {
                    windSettingsExpanded = EditorGUILayout.Foldout(windSettingsExpanded, "Wind Settings", true);
                    if (windSettingsExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawWindSettings(wind);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            // Precipitation Settings
            if (precipitation != null)
            {
                precipitationSettingsExpanded = EditorGUILayout.Foldout(precipitationSettingsExpanded, "Precipitation Settings", true);
                if (precipitationSettingsExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawPrecipitationSettings();
                    EditorGUI.indentLevel--;
                }
            }

            // WeatherPhysicsManifold Settings
            if (weatherPhysicsManifold != null)
            {
                weatherPhysicsManifoldSettingsExpanded = EditorGUILayout.Foldout(weatherPhysicsManifoldSettingsExpanded, "WeatherPhysicsManifold Settings", true);
                if (weatherPhysicsManifoldSettingsExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawWeatherPhysicsManifoldSettings();
                    EditorGUI.indentLevel--;
                }
            }

            // Water Settings
            if (waterObject != null)
            {
                Water water = waterObject.GetComponent<Water>();
                if (water != null)
                {
                    waterSettingsExpanded = EditorGUILayout.Foldout(waterSettingsExpanded, "Water Settings", true);
                    if (waterSettingsExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawWaterSettings(water);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            // Cloud Settings
            if (cloudObject != null)
            {
                Cloud cloud = cloudObject.GetComponent<Cloud>();
                if (cloud != null)
                {
                    cloudSettingsExpanded = EditorGUILayout.Foldout(cloudSettingsExpanded, "Cloud Settings", true);
                    if (cloudSettingsExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawCloudSettings(cloud);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSection()
        {
            validationExpanded = EditorGUILayout.Foldout(validationExpanded, "Step 4: Validation Checklist", true);
            if (!validationExpanded)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (validationReport == null)
            {
                validationReport = new ValidationReport();
            }

            EditorGUILayout.LabelField("Setup Validation", EditorStyles.boldLabel);
            DrawCheckbox("Main Weather Object exists with all 5 components", validationReport.mainObjectValid);
            DrawCheckbox("All 5 subsystem GameObjects exist", validationReport.subsystemsValid);
            DrawCheckbox("WeatherSystem has all subsystem references linked", validationReport.referencesValid);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Configuration Validation", EditorStyles.boldLabel);
            DrawCheckbox("Meteorology: Temperature, pressure, humidity set", validationReport.meteorologyConfigured);
            DrawCheckbox("Wind: Speed and direction configured", validationReport.windConfigured);
            DrawCheckbox("Precipitation: Rate and type set", validationReport.precipitationConfigured);
            DrawCheckbox("WeatherPhysicsManifold: Bounds and resolution set", validationReport.manifoldConfigured);
            DrawCheckbox("Water: Terrain reference assigned", validationReport.waterConfigured);
            DrawCheckbox("Cloud: Altitude range set", validationReport.cloudConfigured);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            {
                ValidateSetup();
                EditorUtility.DisplayDialog("Validation Complete", 
                    $"Validation Results:\n" +
                    $"Setup: {(validationReport.IsSetupValid() ? "✓ Valid" : "✗ Invalid")}\n" +
                    $"Configuration: {(validationReport.IsConfigurationValid() ? "✓ Valid" : "✗ Invalid")}\n\n" +
                    $"See checklist above for details.",
                    "OK");
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Auto-Fix Common Issues", GUILayout.Height(25)))
            {
                FixCommonIssues();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCheckbox(string label, bool value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Toggle(value, GUILayout.Width(15));
            EditorGUILayout.LabelField(label, value ? EditorStyles.label : EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        // Settings drawing methods
        private void DrawMeteorologySettings()
        {
            SerializedObject so = new SerializedObject(meteorology);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("temperature"));
            EditorGUILayout.PropertyField(so.FindProperty("pressure"));
            EditorGUILayout.PropertyField(so.FindProperty("humidity"));
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Recommended Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("autoCalculateDewPoint"));
            EditorGUILayout.PropertyField(so.FindProperty("cloudCover"));
            so.ApplyModifiedProperties();
        }

        private void DrawWindSettings(Wind wind)
        {
            SerializedObject so = new SerializedObject(wind);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("speed"));
            EditorGUILayout.PropertyField(so.FindProperty("direction"));
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Recommended Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("autoGenerateAltitudeLevels"));
            EditorGUILayout.PropertyField(so.FindProperty("forceMultiplier"));
            so.ApplyModifiedProperties();
        }

        private void DrawPrecipitationSettings()
        {
            SerializedObject so = new SerializedObject(precipitation);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("precipitationRate"));
            EditorGUILayout.PropertyField(so.FindProperty("type"));
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Recommended Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("snowTemperatureThreshold"));
            EditorGUILayout.PropertyField(so.FindProperty("sleetTemperatureThreshold"));
            so.ApplyModifiedProperties();
        }

        private void DrawWeatherPhysicsManifoldSettings()
        {
            SerializedObject so = new SerializedObject(weatherPhysicsManifold);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("worldBounds"));
            EditorGUILayout.PropertyField(so.FindProperty("cellResolution"));
            EditorGUILayout.PropertyField(so.FindProperty("cellCount"));
            so.ApplyModifiedProperties();
        }

        private void DrawWaterSettings(Water water)
        {
            SerializedObject so = new SerializedObject(water);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("terrain"));
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Recommended Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("autoFindWaterBodies"));
            so.ApplyModifiedProperties();
        }

        private void DrawCloudSettings(Cloud cloud)
        {
            SerializedObject so = new SerializedObject(cloud);
            EditorGUILayout.LabelField("Critical Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("altitude"));
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Recommended Settings:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("isManagedByMeteorology"));
            if (cloud.isManagedByMeteorology)
            {
                EditorGUILayout.PropertyField(so.FindProperty("meteorology"));
            }
            so.ApplyModifiedProperties();
        }

        // Creation methods
        private GameObject CreateMainWeatherObject()
        {
            GameObject obj = new GameObject("WeatherSystem");
            weatherSystem = obj.AddComponent<WeatherSystem>();
            meteorology = obj.AddComponent<Meteorology>();
            precipitation = obj.AddComponent<Precipitation>();
            weatherPhysicsManifold = obj.AddComponent<WeatherPhysicsManifold>();
            physicsManifold = obj.AddComponent<PhysicsManifold>();

            ApplyRecommendedSettings();
            Selection.activeGameObject = obj;
            EditorUtility.SetDirty(obj);

            return obj;
        }

        private GameObject CreateSubsystemObject<T>(string name) where T : MonoBehaviour
        {
            GameObject obj = new GameObject(name);
            T component = obj.AddComponent<T>();
            Selection.activeGameObject = obj;
            EditorUtility.SetDirty(obj);
            return obj;
        }

        private GameObject CreateTerrainObject()
        {
            GameObject obj = new GameObject("Terrain");
            Terrain terrain = obj.AddComponent<Terrain>();
            TerrainCollider collider = obj.AddComponent<TerrainCollider>();
            
            // Create basic terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.size = new Vector3(100, 30, 100);
            terrainData.heightmapResolution = 257;
            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;

            Selection.activeGameObject = obj;
            EditorUtility.SetDirty(obj);

            return obj;
        }

        private T AddOrGetComponent<T>(GameObject obj) where T : MonoBehaviour
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
                EditorUtility.SetDirty(obj);
            }
            return component;
        }

        private void AutoLinkReferences()
        {
            if (weatherSystem == null || mainWeatherObject == null)
                return;

            SerializedObject so = new SerializedObject(weatherSystem);
            if (meteorology != null)
                so.FindProperty("meteorology").objectReferenceValue = meteorology;
            if (windObject != null)
                so.FindProperty("wind").objectReferenceValue = windObject.GetComponent<Wind>();
            if (precipitation != null)
                so.FindProperty("precipitation").objectReferenceValue = precipitation;
            if (waterObject != null)
                so.FindProperty("water").objectReferenceValue = waterObject.GetComponent<Water>();
            if (cloudObject != null)
                so.FindProperty("cloud").objectReferenceValue = cloudObject.GetComponent<Cloud>();
            if (weatherPhysicsManifold != null)
                so.FindProperty("weatherPhysicsManifold").objectReferenceValue = weatherPhysicsManifold;
            so.ApplyModifiedProperties();

            // Link cloud to meteorology if managed
            if (cloudObject != null)
            {
                Cloud cloud = cloudObject.GetComponent<Cloud>();
                if (cloud != null && cloud.isManagedByMeteorology && meteorology != null)
                {
                    SerializedObject cloudSo = new SerializedObject(cloud);
                    cloudSo.FindProperty("meteorology").objectReferenceValue = meteorology;
                    cloudSo.ApplyModifiedProperties();
                }
            }

            // Link water to terrain
            if (waterObject != null && terrainObject != null)
            {
                Water water = waterObject.GetComponent<Water>();
                if (water != null)
                {
                    SerializedObject waterSo = new SerializedObject(water);
                    waterSo.FindProperty("terrain").objectReferenceValue = terrainObject.GetComponent<Terrain>();
                    waterSo.ApplyModifiedProperties();
                }
            }

            EditorUtility.DisplayDialog("Auto-Link Complete", "All references have been linked automatically.", "OK");
        }

        private void ApplyRecommendedSettings()
        {
            if (meteorology != null)
            {
                meteorology.autoCalculateDewPoint = true;
                EditorUtility.SetDirty(meteorology);
            }

            if (windObject != null)
            {
                Wind wind = windObject.GetComponent<Wind>();
                if (wind != null)
                {
                    wind.autoGenerateAltitudeLevels = true;
                    EditorUtility.SetDirty(wind);
                }
            }

            if (waterObject != null)
            {
                Water water = waterObject.GetComponent<Water>();
                if (water != null)
                {
                    water.autoFindWaterBodies = true;
                    EditorUtility.SetDirty(water);
                }
            }

            if (cloudObject != null)
            {
                Cloud cloud = cloudObject.GetComponent<Cloud>();
                if (cloud != null)
                {
                    cloud.isManagedByMeteorology = true;
                    EditorUtility.SetDirty(cloud);
                }
            }
        }

        // Validation methods
        private void ValidateSetup()
        {
            validationReport = new ValidationReport();

            // Setup validation
            validationReport.mainObjectValid = ValidateMainWeatherObject();
            validationReport.subsystemsValid = ValidateSubsystemObjects();
            validationReport.referencesValid = ValidateReferences();

            // Configuration validation
            validationReport.meteorologyConfigured = ValidateMeteorologySettings();
            validationReport.windConfigured = ValidateWindSettings();
            validationReport.precipitationConfigured = ValidatePrecipitationSettings();
            validationReport.manifoldConfigured = ValidateWeatherPhysicsManifoldSettings();
            validationReport.waterConfigured = ValidateWaterSettings();
            validationReport.cloudConfigured = ValidateCloudSettings();
        }

        private bool ValidateMainWeatherObject()
        {
            if (mainWeatherObject == null)
                return false;

            return mainWeatherObject.GetComponent<WeatherSystem>() != null &&
                   mainWeatherObject.GetComponent<Meteorology>() != null &&
                   mainWeatherObject.GetComponent<Precipitation>() != null &&
                   mainWeatherObject.GetComponent<WeatherPhysicsManifold>() != null &&
                   mainWeatherObject.GetComponent<PhysicsManifold>() != null;
        }

        private bool ValidateSubsystemObjects()
        {
            return cloudObject != null &&
                   windObject != null &&
                   terrainObject != null &&
                   waterObject != null;
        }

        private bool ValidateReferences()
        {
            if (weatherSystem == null)
                return false;

            return weatherSystem.meteorology != null &&
                   weatherSystem.wind != null &&
                   weatherSystem.precipitation != null &&
                   weatherSystem.water != null &&
                   weatherSystem.cloud != null &&
                   weatherSystem.weatherPhysicsManifold != null;
        }

        private bool ValidateMeteorologySettings()
        {
            if (meteorology == null)
                return false;

            return meteorology.temperature != 0 || meteorology.pressure != 0 || meteorology.humidity != 0;
        }

        private bool ValidateWindSettings()
        {
            if (windObject == null)
                return false;

            Wind wind = windObject.GetComponent<Wind>();
            return wind != null && (wind.speed != 0 || wind.direction != 0);
        }

        private bool ValidatePrecipitationSettings()
        {
            if (precipitation == null)
                return false;

            return true; // Type is always set (enum default)
        }

        private bool ValidateWeatherPhysicsManifoldSettings()
        {
            if (weatherPhysicsManifold == null)
                return false;

            return weatherPhysicsManifold.worldBounds.size.magnitude > 0 &&
                   weatherPhysicsManifold.cellResolution > 0;
        }

        private bool ValidateWaterSettings()
        {
            if (waterObject == null)
                return false;

            Water water = waterObject.GetComponent<Water>();
            return water != null && water.terrain != null;
        }

        private bool ValidateCloudSettings()
        {
            if (cloudObject == null)
                return false;

            Cloud cloud = cloudObject.GetComponent<Cloud>();
            return cloud != null && cloud.altitude.magnitude > 0;
        }

        private void FixCommonIssues()
        {
            int fixesApplied = 0;

            // Auto-link references if missing
            if (weatherSystem != null && !ValidateReferences())
            {
                AutoLinkReferences();
                fixesApplied++;
            }

            // Apply recommended settings
            ApplyRecommendedSettings();
            fixesApplied++;

            if (fixesApplied > 0)
            {
                EditorUtility.DisplayDialog("Auto-Fix Complete", 
                    $"Applied {fixesApplied} automatic fixes. Please review the changes.", "OK");
                ValidateSetup();
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues Found", 
                    "No common issues were detected that could be auto-fixed.", "OK");
            }
        }

        private void DrawAutoSetupSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "One-click setup that creates all required objects and components with recommended defaults.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Auto-Setup Weather System", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Auto-Setup", 
                    "This will create all required GameObjects and components. Continue?",
                    "Yes", "Cancel"))
                {
                    AutoSetupWeatherSystem();
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Preset Configurations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply preset weather configurations for quick testing.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            selectedPreset = (WeatherPreset)EditorGUILayout.EnumPopup("Weather Preset", selectedPreset);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Clear Day Preset", GUILayout.Height(25)))
            {
                ApplyPreset(WeatherPreset.ClearDay);
            }
            if (GUILayout.Button("Apply Storm Preset", GUILayout.Height(25)))
            {
                ApplyPreset(WeatherPreset.Storm);
            }
            if (GUILayout.Button("Apply Winter Preset", GUILayout.Height(25)))
            {
                ApplyPreset(WeatherPreset.Winter);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AutoSetupWeatherSystem()
        {
            // Create main weather object if it doesn't exist
            if (mainWeatherObject == null)
            {
                mainWeatherObject = CreateMainWeatherObject();
            }

            // Create subsystem objects if they don't exist
            if (cloudObject == null)
                cloudObject = CreateSubsystemObject<Cloud>("Cloud");

            if (windObject == null)
                windObject = CreateSubsystemObject<Wind>("Wind");

            if (terrainObject == null)
                terrainObject = CreateTerrainObject();

            if (waterObject == null)
                waterObject = CreateSubsystemObject<Water>("Water");

            // Auto-link all references
            AutoLinkReferences();

            // Apply recommended settings
            ApplyRecommendedSettings();

            // Apply default preset (Clear Day)
            ApplyPreset(WeatherPreset.ClearDay);

            // Validate setup
            ValidateSetup();

            EditorUtility.DisplayDialog("Auto-Setup Complete",
                "Weather system has been set up automatically!\n\n" +
                "All required GameObjects and components have been created.\n" +
                "References have been linked.\n" +
                "Recommended settings have been applied.\n\n" +
                "Please review the setup in the wizard.",
                "OK");

            // Refresh the window
            FindExistingComponents();
        }

        private void ApplyPreset(WeatherPreset preset)
        {
            if (meteorology == null || windObject == null || precipitation == null)
            {
                EditorUtility.DisplayDialog("Preset Error",
                    "Please set up the weather system first using the wizard.",
                    "OK");
                return;
            }

            Wind wind = windObject.GetComponent<Wind>();
            Cloud cloud = cloudObject != null ? cloudObject.GetComponent<Cloud>() : null;

            switch (preset)
            {
                case WeatherPreset.ClearDay:
                    // Clear, sunny day
                    meteorology.temperature = 25f;
                    meteorology.pressure = 1020f; // High pressure
                    meteorology.humidity = 40f;
                    meteorology.cloudCover = 10f; // Mostly clear

                    if (wind != null)
                    {
                        wind.speed = 3f;
                        wind.direction = 180f; // South wind
                        wind.gustSpeed = 5f;
                    }

                    if (precipitation != null)
                    {
                        precipitation.precipitationRate = 0f;
                        precipitation.type = PrecipitationType.Rain;
                    }

                    if (cloud != null)
                    {
                        cloud.altitude = new Vector2(2000f, 3000f);
                        cloud.coverage = 10f;
                    }

                    EditorUtility.DisplayDialog("Preset Applied", "Clear Day preset applied successfully!", "OK");
                    break;

                case WeatherPreset.Storm:
                    // Stormy weather
                    meteorology.temperature = 15f;
                    meteorology.pressure = 980f; // Low pressure
                    meteorology.humidity = 90f;
                    meteorology.cloudCover = 100f; // Overcast

                    if (wind != null)
                    {
                        wind.speed = 15f;
                        wind.direction = 225f; // Southwest wind
                        wind.gustSpeed = 25f;
                    }

                    if (precipitation != null)
                    {
                        precipitation.precipitationRate = 10f; // Heavy rain
                        precipitation.type = PrecipitationType.Rain;
                    }

                    if (cloud != null)
                    {
                        cloud.altitude = new Vector2(500f, 5000f);
                        cloud.coverage = 100f;
                        cloud.type = CloudType.Cumulonimbus;
                    }

                    EditorUtility.DisplayDialog("Preset Applied", "Storm preset applied successfully!", "OK");
                    break;

                case WeatherPreset.Winter:
                    // Winter weather
                    meteorology.temperature = -5f;
                    meteorology.pressure = 1013f;
                    meteorology.humidity = 70f;
                    meteorology.cloudCover = 60f;

                    if (wind != null)
                    {
                        wind.speed = 8f;
                        wind.direction = 315f; // Northwest wind
                        wind.gustSpeed = 12f;
                    }

                    if (precipitation != null)
                    {
                        precipitation.precipitationRate = 2f; // Light snow
                        precipitation.type = PrecipitationType.Snow;
                        precipitation.snowTemperatureThreshold = 2f;
                    }

                    if (cloud != null)
                    {
                        cloud.altitude = new Vector2(1000f, 2500f);
                        cloud.coverage = 60f;
                        cloud.type = CloudType.Nimbostratus;
                    }

                    EditorUtility.DisplayDialog("Preset Applied", "Winter preset applied successfully!", "OK");
                    break;
            }

            // Mark objects as dirty
            EditorUtility.SetDirty(meteorology);
            if (wind != null) EditorUtility.SetDirty(wind);
            if (precipitation != null) EditorUtility.SetDirty(precipitation);
            if (cloud != null) EditorUtility.SetDirty(cloud);
        }

        private void FindExistingComponents()
        {
            // Find main weather object
            WeatherSystem ws = FindObjectOfType<WeatherSystem>();
            if (ws != null)
            {
                mainWeatherObject = ws.gameObject;
                weatherSystem = ws;
                meteorology = mainWeatherObject.GetComponent<Meteorology>();
                precipitation = mainWeatherObject.GetComponent<Precipitation>();
                weatherPhysicsManifold = mainWeatherObject.GetComponent<WeatherPhysicsManifold>();
                physicsManifold = mainWeatherObject.GetComponent<PhysicsManifold>();
            }

            // Find subsystem objects
            Cloud cloud = FindObjectOfType<Cloud>();
            if (cloud != null) cloudObject = cloud.gameObject;

            Wind wind = FindObjectOfType<Wind>();
            if (wind != null) windObject = wind.gameObject;

            Terrain terrain = FindObjectOfType<Terrain>();
            if (terrain != null) terrainObject = terrain.gameObject;

            Water water = FindObjectOfType<Water>();
            if (water != null) waterObject = water.gameObject;

            Precipitation[] precipitations = FindObjectsOfType<Precipitation>();
            if (precipitations.Length > 1)
            {
                foreach (var p in precipitations)
                {
                    if (p.gameObject != mainWeatherObject)
                    {
                        rainObject = p.gameObject;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validation report structure
    /// </summary>
    public class ValidationReport
    {
        public bool mainObjectValid;
        public bool subsystemsValid;
        public bool referencesValid;
        public bool meteorologyConfigured;
        public bool windConfigured;
        public bool precipitationConfigured;
        public bool manifoldConfigured;
        public bool waterConfigured;
        public bool cloudConfigured;

        public bool IsSetupValid()
        {
            return mainObjectValid && subsystemsValid && referencesValid;
        }

        public bool IsConfigurationValid()
        {
            return meteorologyConfigured && windConfigured && precipitationConfigured &&
                   manifoldConfigured && waterConfigured && cloudConfigured;
        }
    }
}
