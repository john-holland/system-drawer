using UnityEngine;

/// <summary>Attaches PixelLightRig signal heads on a utility / traffic pole.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Light Pole Decorator")]
public sealed class TrafficLightPoleDecorator : MonoBehaviour
{
    public UtilityPoleAssembly pole;
    public TrafficLightController controller;
    public Transform headRoot;
    public bool createHeadsIfMissing = true;

    void Awake()
    {
        if (pole == null)
            pole = GetComponent<UtilityPoleAssembly>() ?? GetComponentInParent<UtilityPoleAssembly>();
        if (controller == null)
            controller = GetComponent<TrafficLightController>() ?? gameObject.AddComponent<TrafficLightController>();
        if (headRoot == null)
        {
            var go = new GameObject("SignalHeads");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.4f, 3.2f, 0f);
            headRoot = go.transform;
        }
        if (createHeadsIfMissing)
            EnsureHeads();
    }

    public void EnsureHeads()
    {
        controller.mainRed = EnsureHead("MainRed", new Vector3(0f, 0.3f, 0f), TrafficLightController.Red);
        controller.mainYellow = EnsureHead("MainYellow", new Vector3(0f, 0.15f, 0f), TrafficLightController.Yellow);
        controller.mainGreen = EnsureHead("MainGreen", new Vector3(0f, 0f, 0f), TrafficLightController.Green);
        controller.sideRed = EnsureHead("SideRed", new Vector3(0.25f, 0.3f, 0.2f), TrafficLightController.Red);
        controller.sideYellow = EnsureHead("SideYellow", new Vector3(0.25f, 0.15f, 0.2f), TrafficLightController.Yellow);
        controller.sideGreen = EnsureHead("SideGreen", new Vector3(0.25f, 0f, 0.2f), TrafficLightController.Green);
        controller.Enter(TrafficSignalPhase.MainGreen);
    }

    PixelLightRig EnsureHead(string name, Vector3 localPos, Color c)
    {
        var t = headRoot.Find(name);
        GameObject go;
        if (t == null)
        {
            go = PixelLightPrefabFactory.CreateDefaultRuntime(headRoot);
            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.35f;
        }
        else go = t.gameObject;

        var rig = go.GetComponent<PixelLightRig>() ?? go.AddComponent<PixelLightRig>();
        rig.syncMode = PixelLightSyncMode.Free;
        rig.SetSolidChannel(c, false);
        return rig;
    }
}
