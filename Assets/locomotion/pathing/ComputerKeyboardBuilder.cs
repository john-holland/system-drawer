using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds a procedural computer keyboard under a host transform.</summary>
public static class ComputerKeyboardBuilder
{
    public static ComputerKeyboardRuntime Build(ComputerKeyboardSpec spec, Transform parent)
    {
        if (spec == null)
            spec = new ComputerKeyboardSpec();
        var rootGo = new GameObject("ComputerKeyboard");
        if (parent != null)
            rootGo.transform.SetParent(parent, false);
        rootGo.transform.localRotation = Quaternion.Euler(spec.slantTowardUserDeg, 0f, 0f);

        var runtime = rootGo.AddComponent<ComputerKeyboardRuntime>();
        runtime.spec = spec;
        runtime.keys = new List<ComputerKey>();

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "KeyboardBody";
        body.transform.SetParent(rootGo.transform, false);
        body.transform.localScale = new Vector3(spec.baseWidth, spec.baseHeight, spec.baseDepth);
        body.transform.localPosition = Vector3.zero;
        Object.Destroy(body.GetComponent<Collider>());
        runtime.bodyTransform = body.transform;

        var layout = ComputerKeyboardLayout.BuildDefault(spec.auxKeyCount);
        float unit = spec.chicletWidth + 0.002f;
        float rowPitch = spec.chicletWidth + 0.004f;
        var rowCursorX = new Dictionary<int, float>();
        var sectionOffset = new Dictionary<ComputerKeySection, float>
        {
            { ComputerKeySection.Function, -spec.baseWidth * 0.48f },
            { ComputerKeySection.Main, -spec.baseWidth * 0.48f },
            { ComputerKeySection.Nav, spec.baseWidth * 0.12f },
            { ComputerKeySection.Numpad, spec.baseWidth * 0.28f },
            { ComputerKeySection.Aux, spec.baseWidth * 0.05f },
            { ComputerKeySection.Volume, spec.baseWidth * 0.42f }
        };

        for (int i = 0; i < layout.Count; i++)
        {
            var e = layout[i];
            int row = e.row;
            if (!rowCursorX.ContainsKey(row))
                rowCursorX[row] = sectionOffset.TryGetValue(e.section, out float ox) ? ox : 0f;

            float x = rowCursorX[row] + e.unitWidth * unit * 0.5f;
            float z = spec.baseDepth * 0.42f - row * rowPitch;
            float y = spec.baseHeight * 0.5f + spec.chicletHeight * 0.5f;
            rowCursorX[row] = x + e.unitWidth * unit * 0.5f + 0.001f;

            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyGo.name = "Key_" + e.id;
            keyGo.transform.SetParent(rootGo.transform, false);
            keyGo.transform.localScale = new Vector3(
                spec.chicletWidth * e.unitWidth,
                spec.chicletHeight,
                spec.chicletWidth * e.unitHeight);
            keyGo.transform.localPosition = new Vector3(x, y, z);

            var key = keyGo.AddComponent<ComputerKey>();
            key.id = e.id;
            key.section = e.section;
            key.legend = e.legend;
            key.unicode = e.unicode;
            key.unitWidth = e.unitWidth;
            key.unitHeight = e.unitHeight;
            key.isKnob = e.isKnob;
            key.minPressImpulse = spec.minPressImpulse;
            Vector2 band = spec.ComputeTravelBand(row / 7f);
            key.travelMin = band.x;
            key.travelMax = band.y;
            key.meshFilter = keyGo.GetComponent<MeshFilter>();
            key.meshCollider = keyGo.GetComponent<MeshCollider>();
            if (key.meshCollider == null)
            {
                Object.Destroy(keyGo.GetComponent<Collider>());
                key.meshCollider = keyGo.AddComponent<MeshCollider>();
                key.meshCollider.sharedMesh = key.meshFilter.sharedMesh;
                key.meshCollider.convex = true;
            }

            if (e.unicode != '\0' && e.unicode != ' ')
            {
                var glyph = FontFamilyGlyphMesher.ExtrudeCharacter(e.unicode, 0.004f, spec.chicletWidth * 0.45f);
                var legendHost = new GameObject("Legend_" + e.legend);
                legendHost.transform.SetParent(keyGo.transform, false);
                legendHost.transform.localPosition = Vector3.up * (spec.chicletHeight * 0.51f);
                GlyphConvexTreeBaker.TryBake(glyph, legendHost.transform, out _, out _);
                GlyphSdfMaxComposer.ComposeFromMesh(glyph, new Vector3(
                    spec.chicletWidth * e.unitWidth * 0.5f,
                    spec.chicletHeight * 0.5f,
                    spec.chicletWidth * e.unitHeight * 0.5f));
                var lightGo = new GameObject("LegendLight");
                lightGo.transform.SetParent(keyGo.transform, false);
                lightGo.transform.localPosition = Vector3.up * 0.002f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 0.05f;
                light.intensity = 0f;
                key.legendLight = light;
            }

            key.CaptureRest();
            runtime.keys.Add(key);

            if (e.isKnob)
            {
                var knob = keyGo.AddComponent<VolumeKnobRuntime>();
                knob.height = spec.volumeKnobHeight;
                knob.radius = spec.volumeKnobRadius;
                knob.topBevel = spec.volumeKnobTopBevel;
                knob.clearance = spec.volumeKnobClearance;
                knob.travel = spec.volumeKnobTravel;
                knob.clickCount = spec.volumeKnobClicks;
                knob.knobLight = key.legendLight;
                runtime.volumeKnob = knob;
            }
        }

        runtime.RecalculateCentroid();
        return runtime;
    }
}
