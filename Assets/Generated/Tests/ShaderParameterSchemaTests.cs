#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unit tests for ShaderParameterSchema: GetSlot, ToPromptSpec, GetDependencySlotIdsForMaterialType (default and custom slots).
/// </summary>
public class ShaderParameterSchemaTests
{
    [Test]
    public void GetSlot_EmptySchema_ReturnsNullForMissingId()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>();
        Assert.IsNull(schema.GetSlot("albedo"));
        Assert.IsNull(schema.GetSlot(""));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void GetSlot_WithSlots_ReturnsMatchingSlot()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>
        {
            new ShaderParameterSchema.MapSlot { id = "albedo", propertyName = "_MainTex", required = true },
            new ShaderParameterSchema.MapSlot { id = "normal", propertyName = "_BumpMap", required = false }
        };
        var slot = schema.GetSlot("albedo");
        Assert.IsNotNull(slot);
        Assert.AreEqual("_MainTex", slot.propertyName);
        Assert.IsTrue(slot.required);
        Assert.IsNull(schema.GetSlot("missing"));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void GetSlot_IsCaseInsensitive()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>
        {
            new ShaderParameterSchema.MapSlot { id = "Albedo", propertyName = "_MainTex", required = false }
        };
        Assert.IsNotNull(schema.GetSlot("albedo"));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void ToPromptSpec_ContainsCoordSpaceAndSlots()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.coordSpace = ShaderParameterSchema.CoordSpace.World;
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>
        {
            new ShaderParameterSchema.MapSlot { id = "albedo", propertyName = "_MainTex", required = true }
        };
        var spec = schema.ToPromptSpec();
        Assert.That(spec, Does.Contain("Schema:"));
        Assert.That(spec, Does.Contain("World"));
        Assert.That(spec, Does.Contain("albedo"));
        Assert.That(spec, Does.Contain("(required)"));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void GetDependencySlotIdsForMaterialType_EmptySchema_ReturnsDefaultAlbedo()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>();
        var ids = schema.GetDependencySlotIdsForMaterialType("Unlit");
        Assert.IsNotNull(ids);
        Assert.Greater(ids.Count, 0);
        Assert.That(ids, Does.Contain("albedo"));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void GetDependencySlotIdsForMaterialType_Lit_IncludesAlbedoNormalSpecular()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>();
        var ids = schema.GetDependencySlotIdsForMaterialType("Lit");
        Assert.That(ids, Does.Contain("albedo"));
        Assert.That(ids, Does.Contain("normal"));
        Assert.That(ids, Does.Contain("specular"));
        Object.DestroyImmediate(schema);
    }

    [Test]
    public void GetDependencySlotIdsForMaterialType_RequiredSlots_AlwaysIncluded()
    {
        var schema = ScriptableObject.CreateInstance<ShaderParameterSchema>();
        schema.mapSlots = new List<ShaderParameterSchema.MapSlot>
        {
            new ShaderParameterSchema.MapSlot { id = "albedo", propertyName = "_MainTex", required = true },
            new ShaderParameterSchema.MapSlot { id = "custom", propertyName = "_Custom", required = true }
        };
        var ids = schema.GetDependencySlotIdsForMaterialType("Unlit");
        Assert.That(ids, Does.Contain("albedo"));
        Assert.That(ids, Does.Contain("custom"));
        Object.DestroyImmediate(schema);
    }
}
#endif
