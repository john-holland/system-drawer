using UnityEngine;

/// <summary>Applies lemma placeholders onto road-lane / sidewalk / wire components.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Road Lane Lemma Resolver")]
public sealed class RoadLaneLemmaResolver : MonoBehaviour
{
    public string placeholderName = RoadLaneLemmaPropertyKeys.RoadLane;
    public bool sidewalkOpen = true;
    public bool lastPressed;

    public bool Apply(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (key == StreetLightLemmaPropertyKeys.ChangedTo || key == "changed-to")
        {
            var lights = GetComponent<StreetLightLemmaResolver>()
                         ?? GetComponentInParent<StreetLightLemmaResolver>();
            lights?.ApplyChangedTo(value);
            return lights != null;
        }

        switch (key)
        {
            case RoadLaneLemmaPropertyKeys.Open:
                sidewalkOpen = value != "false";
                var ribbon = GetComponent<SidewalkRibbon>();
                if (ribbon != null) ribbon.walkOpen = sidewalkOpen;
                return true;
            case RoadLaneLemmaPropertyKeys.Pressed:
            case "activate":
                lastPressed = true;
                GetComponent<RoadComponentMeshActivator>()?.TryPress();
                return true;
            case RoadLaneLemmaPropertyKeys.On:
                GetComponent<EmergencyWarningBar>()?.SetOn(value != "false");
                return true;
            case RoadLaneLemmaPropertyKeys.StopPotential:
                var sign = GetComponent<SignStopPotential>();
                if (sign != null && float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float p))
                    sign.stopPotential01 = p;
                return sign != null;
            default:
                return false;
        }
    }
}
