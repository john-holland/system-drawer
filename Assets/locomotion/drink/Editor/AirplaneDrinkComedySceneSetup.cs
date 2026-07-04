#if UNITY_EDITOR
using Locomotion.Drink;
using Locomotion.Drink.Flow;
using Locomotion.Liquid;
using Locomotion.Liquid.Flood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weather;

namespace Locomotion.Drink.Editor
{
    public static class AirplaneDrinkComedySceneSetup
    {
        const string PrefabDir = "Assets/locomotion/drink/Prefabs";
        const string SceneDir = "Assets/Scenes";
        const string CupPrefabPath = PrefabDir + "/AirplaneCoffeeCup.prefab";
        const string AirplaneScenePath = SceneDir + "/airplane_drink_comedy.unity";
        const string FantasiaScenePath = SceneDir + "/fantasia_drain_comedy.unity";

        [MenuItem("GameObject/Locomotion/Airplane Coffee Cup", false, 11)]
        public static void CreateCoffeeCup()
        {
            EnsureFolder(PrefabDir);
            var cup = BuildCoffeeCupRoot();
            Selection.activeGameObject = cup;
            Undo.RegisterCreatedObjectUndo(cup, "Create Airplane Coffee Cup");
            PrefabUtility.SaveAsPrefabAsset(cup, CupPrefabPath);
            Debug.Log($"Saved {CupPrefabPath}");
        }

        [MenuItem("GameObject/Locomotion/Airplane Drink Comedy Scene", false, 12)]
        public static void CreateAirplaneScene()
        {
            EnsureFolder(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var weather = CreateWeatherRoot(new Vector3(0f, 0f, 0f), new Vector3(4f, 3f, 6f));
            var cabin = CreateCabinBlockout();
            var actor = CreateActorHierarchy(cabin.transform);
            var cup = BuildCoffeeCupRoot();
            cup.transform.SetParent(cabin.transform);
            cup.transform.localPosition = new Vector3(0.3f, 0.85f, 0.2f);

            WireActorDrinkNode(actor, cup);
            BindSpillPool(cabin, weather);

            var policy = actor.GetComponent<AnimationPlaybackPolicyContext>();
            if (policy != null)
            {
                policy.activeScriptText =
                    "{P:beat1} She tries to {drink} the {coffee} almost to her {mouth} while {turbulence} hits.\n" +
                    "{P:beat2} {stalled} — the cup hovers, shaking.\n" +
                    "{P:beat3} {drink} again — {spilled} everywhere.\n" +
                    "{P:beat4} {empty-handed} on the {tray}.";
            }

            EditorSceneManager.SaveScene(scene, AirplaneScenePath);
            Debug.Log($"Saved {AirplaneScenePath}");
        }

        [MenuItem("GameObject/Locomotion/Fantasia Drain Comedy Scene", false, 13)]
        public static void CreateFantasiaScene()
        {
            EnsureFolder(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var weather = CreateWeatherRoot(Vector3.zero, new Vector3(6f, 4f, 6f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "FantasiaFloor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            var bucket = new GameObject("EndlessBucket");
            bucket.transform.position = new Vector3(0f, 0.5f, 0f);
            var vessel = bucket.AddComponent<DrinkVesselComponent>();
            vessel.capacityLiters = 2f;
            vessel.currentVolumeLiters = 2f;
            bucket.AddComponent<DrinkVesselLiquidAdapter>();
            var rim = new GameObject("Rim");
            rim.transform.SetParent(bucket.transform);
            rim.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var spout = rim.AddComponent<OpenEdgeLoopSpoutSimulator>();
            spout.rimCenter = rim.transform;
            spout.rimRadiusM = 0.12f;

            var flow = bucket.AddComponent<DrinkFlowModel>();
            flow.vessel = vessel;
            flow.openRim = spout;
            flow.infiniteDrain = true;
            flow.weatherManifold = weather.GetComponent<WeatherPhysicsManifold>();

            var flood = bucket.AddComponent<RollingSphereFloodSimulator>();
            flood.spout = spout;
            flood.infiniteDrain = true;
            flood.weatherBridge = weather.GetComponent<LiquidWeatherManifoldBridge>();

            var actor = CreateActorHierarchy(floor.transform);
            actor.transform.position = new Vector3(-1f, 0f, -1f);
            WireActorDrinkNode(actor, bucket);

            var policy = actor.GetComponent<AnimationPlaybackPolicyContext>();
            if (policy != null)
                policy.activeScriptText = "{P:endless} {endless} drain from the hose.";

            EditorSceneManager.SaveScene(scene, FantasiaScenePath);
            Debug.Log($"Saved {FantasiaScenePath}");
        }

        static GameObject BuildCoffeeCupRoot()
        {
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = "AirplaneCoffeeCup";
            cup.transform.localScale = new Vector3(0.07f, 0.08f, 0.07f);
            Object.DestroyImmediate(cup.GetComponent<Collider>());

            var vessel = cup.AddComponent<DrinkVesselComponent>();
            vessel.capacityLiters = 0.2f;
            vessel.currentVolumeLiters = 0.2f;
            var adapter = cup.AddComponent<DrinkVesselLiquidAdapter>();
            adapter.vessel = vessel;

            var liquid = cup.AddComponent<DrinkLiquidContent>();
            liquid.sloshMassKg = 0.18f;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(cup.transform);
            rimGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var spout = rimGo.AddComponent<OpenEdgeLoopSpoutSimulator>();
            spout.rimCenter = rimGo.transform;
            spout.rimRadiusM = 0.035f;

            var flow = cup.AddComponent<DrinkFlowModel>();
            flow.vessel = vessel;
            flow.openRim = spout;
            flow.liquidContent = liquid;

            cup.AddComponent<LiquidDrivingInstrument>();
            cup.AddComponent<RollingSphereFloodSimulator>().spout = spout;

            var streamGo = new GameObject("StreamRenderer");
            streamGo.transform.SetParent(cup.transform);
            streamGo.transform.localPosition = rimGo.transform.localPosition;
            streamGo.AddComponent<DrinkStreamRenderer>().flowModel = flow;

            return cup;
        }

        static GameObject CreateWeatherRoot(Vector3 center, Vector3 size)
        {
            var root = new GameObject("WeatherService");
            root.transform.position = center;
            var manifold = root.AddComponent<WeatherPhysicsManifold>();
            manifold.worldBounds = new Bounds(center, size);
            manifold.cellResolution = 0.25f;
            manifold.cellCount = new Vector3Int(
                Mathf.Max(4, Mathf.CeilToInt(size.x / manifold.cellResolution)),
                Mathf.Max(4, Mathf.CeilToInt(size.y / manifold.cellResolution)),
                Mathf.Max(4, Mathf.CeilToInt(size.z / manifold.cellResolution)));
            var bridge = root.AddComponent<LiquidWeatherManifoldBridge>();
            bridge.manifold = manifold;
            return root;
        }

        static GameObject CreateCabinBlockout()
        {
            var cabin = new GameObject("Cabin");
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(cabin.transform);
            seat.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            seat.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);

            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Tray";
            tray.transform.SetParent(cabin.transform);
            tray.transform.localPosition = new Vector3(0f, 0.75f, 0.35f);
            tray.transform.localScale = new Vector3(0.5f, 0.02f, 0.35f);
            return cabin;
        }

        static GameObject CreateActorHierarchy(Transform parent)
        {
            var actor = new GameObject("PassengerActor");
            actor.transform.SetParent(parent);
            actor.AddComponent<Consider>();
            actor.AddComponent<NervousSystem>();
            var policy = actor.AddComponent<AnimationPlaybackPolicyContext>();
            policy.activePhrase = "beat1";
            policy.activeEventIndex = 0;

            var ledger = actor.AddComponent<LiquidConsumptionLedger>();
            var closure = actor.AddComponent<LemmaConsumptionClosure>();
            closure.ledger = ledger;
            closure.policyContext = policy;

            actor.AddComponent<CabinTurbulenceDriver>();
            actor.AddComponent<DrinkFromVesselNode>();

            return actor;
        }

        static void WireActorDrinkNode(GameObject actor, GameObject vessel)
        {
            var node = actor.GetComponent<DrinkFromVesselNode>();
            if (node == null)
                return;
            var so = new SerializedObject(node);
            so.FindProperty("vessel").objectReferenceValue = vessel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ledger = actor.GetComponent<LiquidConsumptionLedger>();
            if (ledger != null)
            {
                ledger.vessel = vessel.GetComponent<DrinkVesselLiquidAdapter>();
                ledger.weatherBridge = Object.FindAnyObjectByType<LiquidWeatherManifoldBridge>();
            }

            var flood = vessel.GetComponent<RollingSphereFloodSimulator>();
            if (flood != null)
                flood.weatherBridge = Object.FindAnyObjectByType<LiquidWeatherManifoldBridge>();
        }

        static void BindSpillPool(GameObject cabin, GameObject weather)
        {
            var pool = cabin.AddComponent<DrinkSpillSurfacePool>();
            pool.weatherBridge = weather.GetComponent<LiquidWeatherManifoldBridge>();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
