using UnityEngine;

/// <summary>Applies street_light / traffic_signal lemma properties to a TrafficLightController.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Street Light Lemma Resolver")]
public sealed class StreetLightLemmaResolver : MonoBehaviour
{
    public TrafficLightController controller;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<TrafficLightController>()
                ?? GetComponentInParent<TrafficLightController>();
    }

    public void Apply(StreetLightLemmaProperties props)
    {
        if (controller == null) return;
        switch (props.op)
        {
            case StreetLightLemmaOp.SetRed:
                controller.SetPhaseFromLemma("red");
                break;
            case StreetLightLemmaOp.SetGreen:
                controller.SetPhaseFromLemma("green");
                break;
            case StreetLightLemmaOp.SetYellow:
                controller.SetPhaseFromLemma("yellow");
                break;
            case StreetLightLemmaOp.ChangedTo:
                controller.SetPhaseFromLemma(props.color);
                break;
        }
    }

    public void ApplyChangedTo(string color)
    {
        Apply(new StreetLightLemmaProperties
        {
            op = StreetLightLemmaOp.ChangedTo,
            color = color
        });
    }
}
