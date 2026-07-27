#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class ActionInputLemmaTests
{
    [Test]
    public void ControlToken_ParsesKeyMouseAxisAndArrows()
    {
        Assert.IsTrue(ControlTokenResolver.TryParse("x", ActionInputSubscribeMode.KeyDown, out var letter));
        Assert.AreEqual(ControlBindingKind.Key, letter.kind);
        Assert.AreEqual(KeyCode.X, letter.keyCode);

        Assert.IsTrue(ControlTokenResolver.TryParse("KEY_UP", ActionInputSubscribeMode.KeyDown, out var up));
        Assert.AreEqual(KeyCode.UpArrow, up.keyCode);

        Assert.IsTrue(ControlTokenResolver.TryParse("MOUSE_0", ActionInputSubscribeMode.KeyUp, out var mouse));
        Assert.AreEqual(ControlBindingKind.MouseButton, mouse.kind);
        Assert.AreEqual(0, mouse.mouseButton);
        Assert.AreEqual(ActionInputSubscribeMode.KeyUp, mouse.subscribe);

        Assert.IsTrue(ControlTokenResolver.TryParse("X_AXIS", ActionInputSubscribeMode.KeyDown, out var axis));
        Assert.AreEqual(ControlBindingKind.Axis, axis.kind);
        Assert.AreEqual("Horizontal", axis.axisName);
        Assert.AreEqual(ActionInputSubscribeMode.Axis, axis.subscribe);

        Assert.IsTrue(ControlTokenResolver.TryParse("Space", ActionInputSubscribeMode.Held, out var space));
        Assert.AreEqual(KeyCode.Space, space.keyCode);
        Assert.AreEqual(ActionInputSubscribeMode.Held, space.subscribe);
    }

    [Test]
    public void Lemma_MapsTo_RegistersAction()
    {
        var go = new GameObject("ActionMapTest");
        try
        {
            var reg = go.AddComponent<ActionInputMapRegistry>();
            var props = ActionInputLemmaProperties.ResolveFromParams(
                new Dictionary<string, string>
                {
                    { "id", "jump" },
                    { "maps-to", "x" }
                },
                "action");
            reg.ApplyLemma(props);

            Assert.IsTrue(reg.TryGet("jump", out var b));
            Assert.AreEqual(1, b.controls.Count);
            Assert.AreEqual(KeyCode.X, b.controls[0].keyCode);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Lemma_SubscribeKeyUp_MapsToMouse0()
    {
        var go = new GameObject("ActionMapKeyUp");
        try
        {
            var reg = go.AddComponent<ActionInputMapRegistry>();
            var props = ActionInputLemmaProperties.ResolveFromParams(
                new Dictionary<string, string>
                {
                    { "id", "fire" },
                    { "subscribe", "KEY_UP" },
                    { "maps-to", "MOUSE_0" }
                });
            reg.ApplyLemma(props);

            Assert.IsTrue(reg.TryGet("fire", out var b));
            Assert.AreEqual(1, b.controls.Count);
            Assert.AreEqual(ControlBindingKind.MouseButton, b.controls[0].kind);
            Assert.AreEqual(0, b.controls[0].mouseButton);
            Assert.AreEqual(ActionInputSubscribeMode.KeyUp, b.controls[0].subscribe);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Lemma_AndMapsTo_OrBinds_AndClearReplaces()
    {
        var go = new GameObject("ActionMapOr");
        try
        {
            var reg = go.AddComponent<ActionInputMapRegistry>();
            reg.ApplyLemma(ActionInputLemmaProperties.ResolveFromParams(
                new Dictionary<string, string>
                {
                    { "id", "fire" },
                    { "subscribe", "KEY_UP" },
                    { "maps-to", "MOUSE_0" },
                    { "and-maps-to", "MOUSE_1" }
                }));

            Assert.IsTrue(reg.TryGet("fire", out var b));
            Assert.AreEqual(2, b.controls.Count);

            reg.ApplyLemma(ActionInputLemmaProperties.ResolveFromParams(
                new Dictionary<string, string>
                {
                    { "id", "fire" },
                    { "clear", "true" },
                    { "maps-to", "Space" }
                }));

            Assert.IsTrue(reg.TryGet("fire", out b));
            Assert.AreEqual(1, b.controls.Count);
            Assert.AreEqual(KeyCode.Space, b.controls[0].keyCode);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void KeymapAlias_To_Space_ViaPrompt()
    {
        var go = new GameObject("ActionMapPrompt");
        try
        {
            var reg = go.AddComponent<ActionInputMapRegistry>();
            int n = ActionInputLemmaResolver.ApplyFromPrompt(
                "{P:keymap|action=jump|to=Space}", reg);
            Assert.AreEqual(1, n);
            Assert.IsTrue(reg.TryGet("jump", out var b));
            Assert.AreEqual(KeyCode.Space, b.controls[0].keyCode);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IsActionInputLemma_RecognizesPlaceholders()
    {
        Assert.IsTrue(ActionInputLemmaResolver.IsActionInputLemma("action"));
        Assert.IsTrue(ActionInputLemmaResolver.IsActionInputLemma("keymap"));
        Assert.IsTrue(ActionInputLemmaResolver.IsActionInputLemma("maps"));
        Assert.IsFalse(ActionInputLemmaResolver.IsActionInputLemma("kiss"));
    }

    [Test]
    public void Subscribe_ParseModes()
    {
        Assert.AreEqual(ActionInputSubscribeMode.KeyUp, ActionInputLemmaProperties.ParseSubscribe("KEY_UP"));
        Assert.AreEqual(ActionInputSubscribeMode.Held, ActionInputLemmaProperties.ParseSubscribe("KEY_HELD"));
        Assert.AreEqual(ActionInputSubscribeMode.Axis, ActionInputLemmaProperties.ParseSubscribe("AXIS"));
        Assert.AreEqual(ActionInputSubscribeMode.KeyDown, ActionInputLemmaProperties.ParseSubscribe("KEY_DOWN"));
    }
}
#endif
