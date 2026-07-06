using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Weather
{
    /// <summary>Shared one-click weather setup for the editor window and WeatherServiceWizardComponent.</summary>
    public static class WeatherStandardAssets
    {
        enum WeatherPreset
        {
            ClearDay,
            Storm,
            Winter
        }

        public static WizardSetupReport SetupForWizard(WeatherServiceWizardComponent target)
        {
            var report = new WizardSetupReport();
            var state = new SetupState();

            state.FindExisting();
            EnsureMain(ref state, report);
            EnsureSubsystems(ref state, report);
            AutoLinkReferences(state, report, silent: true);
            ApplyRecommendedSettings(state, report);
            ApplyPreset(state, WeatherPreset.ClearDay, report);

            if (target != null && state.MainWeatherObject != null &&
                target.weatherSystemObject != state.MainWeatherObject)
            {
                Undo.RecordObject(target, "Assign weather system");
                target.weatherSystemObject = state.MainWeatherObject;
                EditorUtility.SetDirty(target);
                report.Linked.Add("WeatherServiceWizardComponent.weatherSystemObject");
            }

            if (state.MainWeatherObject != null && state.MainWeatherObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(state.MainWeatherObject.scene);

            return report;
        }

        sealed class SetupState
        {
            public GameObject MainWeatherObject;
            public WeatherSystem WeatherSystem;
            public Meteorology Meteorology;
            public Precipitation Precipitation;
            public WeatherPhysicsManifold WeatherPhysicsManifold;
            public GameObject CloudObject;
            public GameObject WindObject;
            public GameObject TerrainObject;
            public GameObject WaterObject;

            public void FindExisting()
            {
                var ws = Object.FindFirstObjectByType<WeatherSystem>(FindObjectsInactive.Include);
                if (ws != null)
                {
                    MainWeatherObject = ws.gameObject;
                    WeatherSystem = ws;
                    Meteorology = MainWeatherObject.GetComponent<Meteorology>();
                    Precipitation = MainWeatherObject.GetComponent<Precipitation>();
                    WeatherPhysicsManifold = MainWeatherObject.GetComponent<WeatherPhysicsManifold>();
                }

                var cloud = Object.FindFirstObjectByType<Cloud>(FindObjectsInactive.Include);
                if (cloud != null) CloudObject = cloud.gameObject;
                var wind = Object.FindFirstObjectByType<Wind>(FindObjectsInactive.Include);
                if (wind != null) WindObject = wind.gameObject;
                var terrain = Object.FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);
                if (terrain != null) TerrainObject = terrain.gameObject;
                var water = Object.FindFirstObjectByType<Water>(FindObjectsInactive.Include);
                if (water != null) WaterObject = water.gameObject;
            }
        }

        static void EnsureMain(ref SetupState state, WizardSetupReport report)
        {
            if (state.MainWeatherObject != null)
            {
                report.Skipped.Add("WeatherSystem");
                return;
            }

            var go = new GameObject("WeatherSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create WeatherSystem");
            state.MainWeatherObject = go;
            state.WeatherSystem = Undo.AddComponent<WeatherSystem>(go);
            state.Meteorology = Undo.AddComponent<Meteorology>(go);
            state.Precipitation = Undo.AddComponent<Precipitation>(go);
            state.WeatherPhysicsManifold = Undo.AddComponent<WeatherPhysicsManifold>(go);
            Undo.AddComponent<PhysicsManifold>(go);
            report.Created.Add("WeatherSystem with core components");
            EditorUtility.SetDirty(go);
        }

        static void EnsureSubsystems(ref SetupState state, WizardSetupReport report)
        {
            var parent = ResolveParent(state.MainWeatherObject);

            if (state.CloudObject == null)
            {
                state.CloudObject = CreateSubsystem<Cloud>(parent, "Cloud", report);
            }
            else report.Skipped.Add("Cloud");

            if (state.WindObject == null)
            {
                state.WindObject = CreateSubsystem<Wind>(parent, "Wind", report);
            }
            else report.Skipped.Add("Wind");

            if (state.TerrainObject == null)
            {
                var obj = new GameObject("Terrain");
                Undo.RegisterCreatedObjectUndo(obj, "Create Terrain");
                if (parent != null)
                    Undo.SetTransformParent(obj.transform, parent, "Parent Terrain");
                var terrain = Undo.AddComponent<Terrain>(obj);
                var collider = Undo.AddComponent<TerrainCollider>(obj);
                var terrainData = new TerrainData();
                terrainData.size = new Vector3(100, 30, 100);
                terrainData.heightmapResolution = 257;
                terrain.terrainData = terrainData;
                collider.terrainData = terrainData;
                state.TerrainObject = obj;
                report.Created.Add("Terrain");
                EditorUtility.SetDirty(obj);
            }
            else report.Skipped.Add("Terrain");

            if (state.WaterObject == null)
            {
                state.WaterObject = CreateSubsystem<Water>(parent, "Water", report);
            }
            else report.Skipped.Add("Water");
        }

        static Transform ResolveParent(GameObject weatherRoot)
        {
            if (weatherRoot == null)
                return null;
            var fac = weatherRoot.GetComponentInParent<SystemDrawerFacilitator>();
            if (fac != null)
            {
                var scene = fac.transform.Find("_StandardScene");
                if (scene != null)
                    return scene;
            }
            return weatherRoot.transform.parent;
        }

        static GameObject CreateSubsystem<T>(Transform parent, string name, WizardSetupReport report) where T : MonoBehaviour
        {
            var obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            if (parent != null)
                Undo.SetTransformParent(obj.transform, parent, "Parent " + name);
            Undo.AddComponent<T>(obj);
            report.Created.Add(name);
            EditorUtility.SetDirty(obj);
            return obj;
        }

        static void AutoLinkReferences(SetupState state, WizardSetupReport report, bool silent)
        {
            if (state.WeatherSystem == null || state.MainWeatherObject == null)
                return;

            var so = new SerializedObject(state.WeatherSystem);
            if (state.Meteorology != null)
                so.FindProperty("meteorology").objectReferenceValue = state.Meteorology;
            if (state.WindObject != null)
                so.FindProperty("wind").objectReferenceValue = state.WindObject.GetComponent<Wind>();
            if (state.Precipitation != null)
                so.FindProperty("precipitation").objectReferenceValue = state.Precipitation;
            if (state.WaterObject != null)
                so.FindProperty("water").objectReferenceValue = state.WaterObject.GetComponent<Water>();
            if (state.CloudObject != null)
                so.FindProperty("cloud").objectReferenceValue = state.CloudObject.GetComponent<Cloud>();
            if (state.WeatherPhysicsManifold != null)
                so.FindProperty("weatherPhysicsManifold").objectReferenceValue = state.WeatherPhysicsManifold;
            so.ApplyModifiedProperties();

            if (state.CloudObject != null && state.Meteorology != null)
            {
                var cloud = state.CloudObject.GetComponent<Cloud>();
                if (cloud != null && cloud.isManagedByMeteorology)
                {
                    var cloudSo = new SerializedObject(cloud);
                    cloudSo.FindProperty("meteorology").objectReferenceValue = state.Meteorology;
                    cloudSo.ApplyModifiedProperties();
                }
            }

            if (state.WaterObject != null && state.TerrainObject != null)
            {
                var water = state.WaterObject.GetComponent<Water>();
                if (water != null)
                {
                    var waterSo = new SerializedObject(water);
                    waterSo.FindProperty("terrain").objectReferenceValue = state.TerrainObject.GetComponent<Terrain>();
                    waterSo.ApplyModifiedProperties();
                }
            }

            report.Linked.Add("WeatherSystem subsystem references");
        }

        static void ApplyRecommendedSettings(SetupState state, WizardSetupReport report)
        {
            if (state.Meteorology != null)
            {
                state.Meteorology.autoCalculateDewPoint = true;
                EditorUtility.SetDirty(state.Meteorology);
            }

            if (state.WindObject != null)
            {
                var wind = state.WindObject.GetComponent<Wind>();
                if (wind != null)
                {
                    wind.autoGenerateAltitudeLevels = true;
                    EditorUtility.SetDirty(wind);
                }
            }

            if (state.WaterObject != null)
            {
                var water = state.WaterObject.GetComponent<Water>();
                if (water != null)
                {
                    water.autoFindWaterBodies = true;
                    EditorUtility.SetDirty(water);
                }
            }

            if (state.CloudObject != null)
            {
                var cloud = state.CloudObject.GetComponent<Cloud>();
                if (cloud != null)
                {
                    cloud.isManagedByMeteorology = true;
                    EditorUtility.SetDirty(cloud);
                }
            }

            report.Linked.Add("Recommended weather settings");
        }

        static void ApplyPreset(SetupState state, WeatherPreset preset, WizardSetupReport report)
        {
            if (state.Meteorology == null || state.WindObject == null || state.Precipitation == null)
                return;

            var wind = state.WindObject.GetComponent<Wind>();
            var cloud = state.CloudObject != null ? state.CloudObject.GetComponent<Cloud>() : null;

            switch (preset)
            {
                case WeatherPreset.ClearDay:
                    state.Meteorology.temperature = 25f;
                    state.Meteorology.pressure = 1020f;
                    state.Meteorology.humidity = 40f;
                    state.Meteorology.cloudCover = 10f;
                    if (wind != null)
                    {
                        wind.speed = 3f;
                        wind.direction = 180f;
                        wind.gustSpeed = 5f;
                    }
                    state.Precipitation.precipitationRate = 0f;
                    state.Precipitation.type = PrecipitationType.Rain;
                    if (cloud != null)
                    {
                        cloud.altitude = new Vector2(2000f, 3000f);
                        cloud.coverage = 10f;
                    }
                    break;
                default:
                    Debug.LogError("Unavailable weather preset - skipping: " + preset);
                    return;
            }

            EditorUtility.SetDirty(state.Meteorology);
            if (wind != null) EditorUtility.SetDirty(wind);
            if (cloud != null) EditorUtility.SetDirty(cloud);
            EditorUtility.SetDirty(state.Precipitation);
            report.Linked.Add("Clear Day weather preset");
        }
    }
}
