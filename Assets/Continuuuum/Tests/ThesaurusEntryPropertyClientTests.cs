using System.Threading.Tasks;
using NUnit.Framework;

public class ThesaurusEntryPropertyClientTests
{
    [Test]
    public async Task Stub_PutAndGetEntryProperty_RoundTrip()
    {
        var client = new StubContinuuuumLocalizationClient();
        await client.PutEntryPropertyAsync("urn:lemma:walk", "non-ik-animation", "true");
        var props = await client.GetEntryPropertiesAsync("urn:lemma:walk");
        Assert.AreEqual(1, props.Length);
        Assert.AreEqual("true", props[0].propertyValue);
    }

    [Test]
    public async Task Stub_DeleteEntryProperty_Removes()
    {
        var client = new StubContinuuuumLocalizationClient();
        await client.PutEntryPropertyAsync("urn:lemma:walk", "non-ik-animation", "true");
        await client.DeleteEntryPropertyAsync("urn:lemma:walk", "non-ik-animation");
        var props = await client.GetEntryPropertiesAsync("urn:lemma:walk");
        Assert.AreEqual(0, props.Length);
    }
}
