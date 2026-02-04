#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Linq;

/// <summary>
/// Unit tests for ShaderGrammarIndex: GetEntries, ToPromptSpec, role filtering.
/// </summary>
public class ShaderGrammarIndexTests
{
    [Test]
    public void GetEntries_EmptyIndex_ReturnsNothing()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>();
        var list = index.GetEntries().ToList();
        Assert.AreEqual(0, list.Count);
        Object.DestroyImmediate(index);
    }

    [Test]
    public void GetEntries_SkipsEmptyTerm()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>
        {
            new ShaderGrammarIndex.Entry { term = "", role = "adj", shaderPropertyOrSlot = "_X" },
            new ShaderGrammarIndex.Entry { term = "icy", role = "adjective", shaderPropertyOrSlot = "_IceTint" }
        };
        var list = index.GetEntries().ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("icy", list[0].term);
        Object.DestroyImmediate(index);
    }

    [Test]
    public void GetEntries_WithRoleFilter_ReturnsOnlyMatchingRole()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>
        {
            new ShaderGrammarIndex.Entry { term = "icy", role = "adjective", shaderPropertyOrSlot = "_IceTint" },
            new ShaderGrammarIndex.Entry { term = "glow", role = "material", shaderPropertyOrSlot = "emission" }
        };
        var list = index.GetEntries("adjective").ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("icy", list[0].term);
        Object.DestroyImmediate(index);
    }

    [Test]
    public void GetEntries_RoleFilter_IsCaseInsensitive()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>
        {
            new ShaderGrammarIndex.Entry { term = "wet", role = "Adjective", shaderPropertyOrSlot = "_Wetness" }
        };
        var list = index.GetEntries("adjective").ToList();
        Assert.AreEqual(1, list.Count);
        Object.DestroyImmediate(index);
    }

    [Test]
    public void ToPromptSpec_Empty_ReturnsEmptyString()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>();
        Assert.AreEqual("", index.ToPromptSpec());
        Object.DestroyImmediate(index);
    }

    [Test]
    public void ToPromptSpec_WithEntries_ContainsHeaderAndLines()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>
        {
            new ShaderGrammarIndex.Entry { term = "icy", role = "adjective", shaderPropertyOrSlot = "_IceTint" },
            new ShaderGrammarIndex.Entry { term = "wet", role = "", shaderPropertyOrSlot = "_Wetness" }
        };
        var spec = index.ToPromptSpec();
        Assert.That(spec, Does.Contain("Allowed terms -> properties/slots:"));
        Assert.That(spec, Does.Contain("icy"));
        Assert.That(spec, Does.Contain("(adjective)"));
        Assert.That(spec, Does.Contain("_IceTint"));
        Assert.That(spec, Does.Contain("wet -> _Wetness"));
        Object.DestroyImmediate(index);
    }

    [Test]
    public void ToPromptSpec_MaxEntries_LimitsOutput()
    {
        var index = ScriptableObject.CreateInstance<ShaderGrammarIndex>();
        index.entries = new System.Collections.Generic.List<ShaderGrammarIndex.Entry>();
        for (int i = 0; i < 30; i++)
            index.entries.Add(new ShaderGrammarIndex.Entry { term = "t" + i, role = "", shaderPropertyOrSlot = "_P" });
        var spec = index.ToPromptSpec(5);
        var lines = spec.Split('\n');
        Assert.GreaterOrEqual(lines.Length, 1);
        // Header + at most 5 entry lines
        Assert.LessOrEqual(lines.Length, 6);
        Object.DestroyImmediate(index);
    }
}
#endif
