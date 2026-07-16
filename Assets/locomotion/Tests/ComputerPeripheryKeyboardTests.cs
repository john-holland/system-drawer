#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SdfMax;
using Locomotion.Musculature;

public sealed class FontFamilyGlyphMesherTests
{
    [Test]
    public void Extrude_Ascii_ProducesMesh()
    {
        Assert.IsTrue(FontFamilyGlyphMesher.TryExtrudeAscii('A', out Mesh m));
        Assert.GreaterOrEqual(m.vertexCount, 8);
        Assert.IsTrue(FontFamilyGlyphMesher.TryExtrudeAscii('0', out Mesh m0));
        Assert.Greater(m0.bounds.size.sqrMagnitude, 0f);
        Assert.IsTrue(FontFamilyGlyphMesher.TryExtrudeAscii('@', out Mesh mAt));
        Assert.Greater(mAt.triangles.Length, 0);
        Object.DestroyImmediate(m);
        Object.DestroyImmediate(m0);
        Object.DestroyImmediate(mAt);
    }

    [Test]
    public void GlyphSdf_SubtractComposition_HasRoot()
    {
        var mesh = FontFamilyGlyphMesher.ExtrudeCharacter('B', 0.02f);
        var asset = GlyphSdfMaxComposer.ComposeFromMesh(mesh, Vector3.one * 0.05f);
        Assert.IsNotNull(asset);
        Assert.AreEqual(SdfMaxOp.Subtract, asset.nodes[asset.ResolveRootIndex()].op);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }
}

public sealed class ComputerKeyboardBuilderTests
{
    [Test]
    public void Layout_HasFGroups_AndNumpadSpans()
    {
        var layout = ComputerKeyboardLayout.BuildDefault(3);
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.F1));
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.F12));
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.Aux1));
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.Aux3));
        Assert.IsFalse(layout.Exists(e => e.id == ComputerKeyId.Aux4));
        var add = layout.Find(e => e.id == ComputerKeyId.NumpadAdd);
        Assert.IsNotNull(add);
        Assert.AreEqual(2f, add.unitHeight, 0.01f);
        var zero = layout.Find(e => e.id == ComputerKeyId.Numpad0);
        Assert.AreEqual(2f, zero.unitWidth, 0.01f);
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.VolumeKnob && e.isKnob));
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.Escape));
        Assert.IsTrue(layout.Exists(e => e.id == ComputerKeyId.ScrollLock));
    }

    [Test]
    public void Builder_CreatesKeys_AndTravelBand()
    {
        var host = new GameObject("KbHost");
        try
        {
            var spec = new ComputerKeyboardSpec { auxKeyCount = 3 };
            var runtime = ComputerKeyboardBuilder.Build(spec, host.transform);
            Assert.IsNotNull(runtime);
            Assert.Greater(runtime.keys.Count, 40);
            Assert.IsNotNull(runtime.volumeKnob);
            Assert.AreEqual(12, runtime.volumeKnob.clickCount);
            Vector2 band = spec.ComputeTravelBand(0.2f);
            Assert.LessOrEqual(band.x, band.y);
            Assert.Greater(band.x, 0f);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}

public sealed class KeyboardHandPickerAndPumpTests
{
    [Test]
    public void HandPicker_LeftOfCentroid_IsLeft()
    {
        var side = KeyboardHandPicker.PickHand(new Vector3(-1f, 0f, 0f), Vector3.zero);
        Assert.AreEqual(KeyboardHandPicker.HandSide.Left, side);
        Assert.AreEqual(FingerKind.Thumb, KeyboardHandPicker.PreferFinger(ComputerKeyId.Space));
        Assert.AreEqual(FingerKind.Pinky, KeyboardHandPicker.PreferFinger(ComputerKeyId.LeftShift));
        Assert.AreEqual(FingerKind.Index, KeyboardHandPicker.PreferFinger(ComputerKeyId.A));
    }

    [Test]
    public void Pump_EnqueueText_DequeuesChars()
    {
        var go = new GameObject("Pump");
        try
        {
            var pump = go.AddComponent<KeyboardMessagePump>();
            pump.EnqueueText("hi");
            Assert.IsTrue(pump.TryDequeue(out var a));
            Assert.AreEqual('h', a.unicode);
            Assert.IsTrue(pump.TryDequeue(out var b));
            Assert.AreEqual('i', b.unicode);
            Assert.IsFalse(pump.TryDequeue(out _));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void FingerCache_And_JumpPress_FallbackAfter5()
    {
        var go = new GameObject("Jump");
        var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var cache = go.AddComponent<FingerPositionCache>();
            var jump = go.AddComponent<PeripheralJumpPress>();
            jump.maxJumpPressAttempts = 5;
            jump.fingerCache = cache;
            var key = keyGo.AddComponent<ComputerKey>();
            key.minPressImpulse = 0.9f;
            key.CaptureRest();

            for (int i = 0; i < 4; i++)
            {
                bool ok = jump.TryJumpPress(key, 0.1f, KeyboardHandPicker.HandSide.Right, FingerKind.Index, out bool need);
                Assert.IsFalse(ok);
                Assert.IsFalse(need);
            }
            bool last = jump.TryJumpPress(key, 0.1f, KeyboardHandPicker.HandSide.Right, FingerKind.Index, out bool build);
            Assert.IsFalse(last);
            Assert.IsTrue(build);

            cache.Remember(KeyboardHandPicker.HandSide.Right, FingerKind.Index, key.WorldPressPoint, key.id);
            jump.ResetAttempts();
            Assert.IsTrue(jump.TryJumpPress(key, 1f, KeyboardHandPicker.HandSide.Right, FingerKind.Index, out _));
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(keyGo);
        }
    }
}

public sealed class ConsiderPathingAndPlaceBuildTests
{
    [Test]
    public void PathingPrep_AddsDeskSitSegment_WhenStationPresent()
    {
        var stationGo = new GameObject("Station");
        var actor = new GameObject("Actor");
        try
        {
            var station = stationGo.AddComponent<ComputerPeripheryStation>();
            station.EnsureSeatContact();
            var plan = new GenericMultiModalPathPlan();
            plan.segments = new List<MultiModalSegment>();
            int before = plan.segments.Count;
            ConsiderPathingPrep.EnrichPlan(plan, actor);
            Assert.Greater(plan.segments.Count, before);
        }
        finally
        {
            Object.DestroyImmediate(stationGo);
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void PlaceBuild_FindGrabbable_ByName()
    {
        var box = new GameObject("wooden_box_prop");
        box.AddComponent<BoxCollider>();
        try
        {
            var found = PlaceBuildTopologyBtBuilder.FindGrabbable(box.transform.position, 2f,
                new SeatStandBridgeSpec());
            Assert.AreEqual(box, found);
            var steps = PlaceBuildTopologyBtBuilder.BuildStepIds(ScriptableObject.CreateInstance<PlaceBuildTopologyAsset>());
            Assert.AreEqual(0, steps.Count);
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    [Test]
    public void SpatialPaint_SetsFilterKeys()
    {
        var go = new GameObject("Paint");
        try
        {
            var paint = go.AddComponent<SpatialDescriptionComponent>();
            paint.PaintFromModifiers(new[] { "bathroom", "icy" });
            Assert.IsTrue(paint.FilterMatches("bathroom"));
            Assert.IsTrue(paint.paintedAdjectives.Contains("icy"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IkCategories_KeyboardMousePeripheralExist()
    {
        Assert.AreEqual(PhysicsIKTrainingCategory.KeyboardType, PhysicsIKTrainingCategory.KeyboardType);
        Assert.AreEqual(PhysicsIKTrainingCategory.MousePoint, PhysicsIKTrainingCategory.MousePoint);
        Assert.AreEqual(PhysicsIKTrainingCategory.PeripheralButtonPress, PhysicsIKTrainingCategory.PeripheralButtonPress);
        Assert.AreEqual(PhysicsIKTrainingCategory.PlaceBuild, PhysicsIKTrainingCategory.PlaceBuild);
    }
}
#endif
