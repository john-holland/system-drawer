#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Weather;

public static class AquaplaneDemoSetupMenu
{
    [MenuItem("Window/System Drawer/Physics/Aquaplane Demo Setup")]
    public static void SetupDemo()
    {
        var waterGo = GameObject.Find("AquaplaneWaterStrip") ?? new GameObject("AquaplaneWaterStrip");
        var manifold = waterGo.GetComponent<WeatherPhysicsManifold>();
        if (manifold == null)
            manifold = waterGo.AddComponent<WeatherPhysicsManifold>();
        manifold.worldBounds = new Bounds(Vector3.zero, new Vector3(40f, 1f, 80f));
        manifold.cellCount = new Vector3Int(20, 2, 40);

        for (float x = -18f; x <= 18f; x += 2f)
        for (float z = -38f; z <= 38f; z += 2f)
        {
            Vector3 p = new Vector3(x, 0f, z);
            var d = manifold.GetDataAtPosition(p);
            d.mode = WeatherMode.Water;
            d.surfaceFriction = 0.06f;
            d.surfaceTensionCoeff = 0.02f;
            d.velocity = Vector3.forward * 2f;
            manifold.SetDataAtPosition(p, d);
        }

        var vehicleRoot = GameObject.Find("AquaplaneVehicle");
        if (vehicleRoot == null)
        {
            vehicleRoot = new GameObject("AquaplaneVehicle");
            vehicleRoot.AddComponent<VehicleActor>();
            vehicleRoot.AddComponent<VehicleAmbulationSolver>();
            vehicleRoot.AddComponent<VehicleAquaplaneSolver>();
            var rb = vehicleRoot.AddComponent<Rigidbody>();
            rb.mass = 800f;
        }

        for (int i = 0; i < 4; i++)
        {
            string name = $"Tire_{i}";
            Transform t = vehicleRoot.transform.Find(name);
            if (t == null)
            {
                var tireGo = new GameObject(name);
                tireGo.transform.SetParent(vehicleRoot.transform, false);
                tireGo.transform.localPosition = new Vector3(i < 2 ? -0.8f : 0.8f, 0.3f, i % 2 == 0 ? 1.2f : -1.2f);
                t = tireGo.transform;
            }

            if (t.GetComponent<LiquidContactSphere>() == null)
                t.gameObject.AddComponent<LiquidContactSphere>();
            if (t.GetComponent<VehicleTire>() == null)
                t.gameObject.AddComponent<VehicleTire>();
        }

        Selection.activeGameObject = vehicleRoot;
        EditorGUIUtility.PingObject(vehicleRoot);
        Debug.Log("Aquaplane demo: water strip + vehicle with liquid contact spheres created.");
    }
}
#endif
