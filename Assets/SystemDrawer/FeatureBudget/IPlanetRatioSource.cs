using System.Collections.Generic;

/// <summary>Planet ratio readings from Composition UI / PlanetBody without asmdef cycle.</summary>
public interface IPlanetRatioSource
{
    float AnchorRadius { get; }
    void CaptureRatioFields(List<RatioFieldSnapshot> output);
}

public struct RatioFieldSnapshot
{
    public string id;
    public float ratio;
    public bool ratioLocked;
    public float manualOverride;
}
