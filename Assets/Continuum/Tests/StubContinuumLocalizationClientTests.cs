using System.Threading.Tasks;
using NUnit.Framework;

public class StubContinuumLocalizationClientTests
{
    [Test]
    public async Task ApplyEdit_ReturnsEmptyLists()
    {
        var client = new StubContinuumLocalizationClient();
        ScriptApplyEditResult result = await client.ApplyScriptEditAsync("draft-1", "a", "b");
        Assert.IsNotNull(result);
        Assert.IsEmpty(result.required);
        Assert.IsEmpty(result.warnings);
    }

    [Test]
    public async Task GetPropertySpecs_IncludesNonIkAnimation()
    {
        var client = new StubContinuumLocalizationClient();
        var specs = await client.GetPropertySpecsAsync();
        Assert.IsNotEmpty(specs);
        Assert.AreEqual("non-ik-animation", specs[0].key);
    }
}
