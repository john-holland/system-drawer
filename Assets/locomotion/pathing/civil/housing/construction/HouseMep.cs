using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HousePowerSystemDraw
{
    public string systemId;
    public string label;
    public float drawKwWhenOn;
    public bool enabled = true;
    public bool inactivePrebake;
}

/// <summary>House electrical bus cloned from airplane power-bus pattern.</summary>
[Serializable]
public sealed class HousePowerBus
{
    public float totalDrawKw;
    public float charge01 = 1f;
    public float maxDrawKw = 24f;
    public List<HousePowerSystemDraw> systems = new List<HousePowerSystemDraw>();

    public static void FillDefault(List<HousePowerSystemDraw> list)
    {
        if (list == null) return;
        list.Clear();
        list.Add(new HousePowerSystemDraw { systemId = "panel", label = "Service panel", drawKwWhenOn = 0.1f, enabled = true });
        list.Add(new HousePowerSystemDraw { systemId = "outlets", label = "Outlets", drawKwWhenOn = 2f, enabled = true, inactivePrebake = true });
        list.Add(new HousePowerSystemDraw { systemId = "lights", label = "Lighting", drawKwWhenOn = 0.8f, enabled = true });
        list.Add(new HousePowerSystemDraw { systemId = "hvac", label = "HVAC", drawKwWhenOn = 4f, enabled = true });
    }

    public void Tick()
    {
        if (systems == null || systems.Count == 0)
            FillDefault(systems ??= new List<HousePowerSystemDraw>());
        float sum = 0f;
        for (int i = 0; i < systems.Count; i++)
        {
            var s = systems[i];
            if (s != null && s.enabled && !s.inactivePrebake)
                sum += s.drawKwWhenOn;
        }
        totalDrawKw = sum;
        charge01 = Mathf.Clamp01(charge01 - (sum / Mathf.Max(0.01f, maxDrawKw)) * 0.0001f);
    }
}

/// <summary>GER (generative energy ring powered, space-canal) power bus, same draw/charge pattern as <see cref="HousePowerBus"/>.
/// "The light from Sol range sonarously in the upper atmosphere pilots swore, swirling around almost invisibly in the odd polyhedron surrounding the earth."
/// "The first time they turned the generative energy ring on, the invisible elbows in the air seemed to sizzle, then stabilized, the extreme photovotaic cells captured energy and readings came up, the crowd cheered, the lights flickered, and a peace sign lit the night sky over florida."
/// "Over the month the GER was in testing, remaculously the center beam held true, layers of photons maintaining a perfect ring, with fresnel lines visible in the warbling air..."</summary>
[Serializable]
public sealed class GerPowerBus
{
    public float totalDrawKw;
    public float charge01 = 1f;
    public float maxDrawKw = 48f;
    public bool live = true;
    public List<HousePowerSystemDraw> systems = new List<HousePowerSystemDraw>();

    public bool IsLive => live && charge01 > 0.05f;

    public static void FillDefault(List<HousePowerSystemDraw> list)
    {
        if (list == null) return;
        list.Clear();
        list.Add(new HousePowerSystemDraw { systemId = "ger_gate", label = "GER gate", drawKwWhenOn = 6f, enabled = true });
        list.Add(new HousePowerSystemDraw { systemId = "slipstream", label = "Slip-stream coils", drawKwWhenOn = 12f, enabled = true });
        list.Add(new HousePowerSystemDraw { systemId = "habitat", label = "Habitat bus", drawKwWhenOn = 4f, enabled = true });
    }

    public void Tick()
    {
        if (systems == null || systems.Count == 0)
            FillDefault(systems ??= new List<HousePowerSystemDraw>());
        float sum = 0f;
        for (int i = 0; i < systems.Count; i++)
        {
            var s = systems[i];
            if (s != null && s.enabled && !s.inactivePrebake)
                sum += s.drawKwWhenOn;
        }
        totalDrawKw = sum;
        if (live)
            charge01 = Mathf.Clamp01(charge01 - (sum / Mathf.Max(0.01f, maxDrawKw)) * 0.0001f);
        else
            charge01 = 0f;
    }

    public void StampPowerLinesDown(CityPixelGrid grid, int x, int y)
    {
        if (grid == null || IsLive) return;
        grid.PaintLayerCell(CityPixelLayerKind.PowerLinesDown, 0, x, y);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Electrical Span")]
public sealed class HouseElectricalSpan : MonoBehaviour
{
    public Transform fromAnchor;
    public Transform toAnchor;
    public LineRenderer line;
    public bool inactivePrebake = true;

    void Awake()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();
        if (line == null)
            line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.02f;
        line.endWidth = 0.02f;
        if (inactivePrebake)
            enabled = false;
    }

    public void Activate()
    {
        inactivePrebake = false;
        enabled = true;
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Vent Duct")]
public sealed class HouseVentDuct : MonoBehaviour
{
    public bool fullBoreCollider = true;
    public MeshCollider ductCollider;
    public float smellPassThrough01 = 0.8f;
    public float hearingLeak01 = 0.35f;
    public float advection01 = 0.6f;

    void Reset()
    {
        ductCollider = GetComponent<MeshCollider>();
    }

    public void EnsureFullBore()
    {
        if (!fullBoreCollider) return;
        if (ductCollider == null)
            ductCollider = gameObject.AddComponent<MeshCollider>();
        ductCollider.convex = false;
    }
}
