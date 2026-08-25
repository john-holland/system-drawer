using UnityEngine;

/// <summary>Default Create for street-light / signal / phone-pole / ped-button prefabs.</summary>
public static class StreetLightPrefabFactory
{
    public static GameObject CreateStreetLightPhonePole(Transform parent = null)
    {
        var go = new GameObject("StreetLightPhonePole");
        if (parent != null) go.transform.SetParent(parent, false);
        var pole = go.AddComponent<UtilityPoleAssembly>();
        pole.EnsureVisuals();
        var luminaire = PixelLightPrefabFactory.CreateDefaultRuntime(go.transform);
        luminaire.name = "Luminaire";
        luminaire.transform.localPosition = new Vector3(0.6f, pole.heightM * 0.9f, 0f);
        var rig = luminaire.GetComponent<PixelLightRig>();
        if (rig != null)
        {
            rig.colorPackage = PixelLightColorPackage.CreateSignal(new Color(1f, 0.92f, 0.7f));
            rig.playing = true;
        }
        go.AddComponent<StreetLightLemmaResolver>();
        EnsureLemma(go, RoadLaneLemmaPropertyKeys.StreetLuminaire);
        return go;
    }

    public static GameObject CreateTrafficSignalPhonePole(Transform parent = null)
    {
        var go = new GameObject("TrafficSignalPhonePole");
        if (parent != null) go.transform.SetParent(parent, false);
        var pole = go.AddComponent<UtilityPoleAssembly>();
        pole.EnsureVisuals();
        var ctrl = go.AddComponent<TrafficLightController>();
        var decorator = go.AddComponent<TrafficLightPoleDecorator>();
        decorator.controller = ctrl;
        decorator.createHeadsIfMissing = true;
        decorator.EnsureHeads();
        go.AddComponent<StreetLightLemmaResolver>().controller = ctrl;
        EnsureLemma(go, StreetLightLemmaPropertyKeys.TrafficSignal);
        return go;
    }

    public static GameObject CreatePhonePole(Transform parent = null)
    {
        var go = new GameObject("PhonePole");
        if (parent != null) go.transform.SetParent(parent, false);
        var pole = go.AddComponent<UtilityPoleAssembly>();
        pole.EnsureVisuals();
        EnsureLemma(go, RoadLaneLemmaPropertyKeys.PhonePole);
        return go;
    }

    public static GameObject CreateStandaloneButton(Transform parent = null)
    {
        var go = new GameObject("StandaloneButton");
        if (parent != null) go.transform.SetParent(parent, false);
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Pedestal";
        box.transform.SetParent(go.transform, false);
        box.transform.localScale = new Vector3(0.25f, 1.1f, 0.25f);
        box.transform.localPosition = Vector3.up * 0.55f;
        EnsureLemma(go, RoadLaneLemmaPropertyKeys.WalkButton);
        go.AddComponent<RoadComponentMeshActivator>();
        return go;
    }

    static RoadLaneLemmaResolver EnsureLemma(GameObject go, string placeholder)
    {
        var lemma = go.GetComponent<RoadLaneLemmaResolver>() ?? go.AddComponent<RoadLaneLemmaResolver>();
        lemma.placeholderName = placeholder;
        return lemma;
    }
}
