#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public class CausalityFamilyAuditTests
{
    [Test]
    public void CompatiblePrefix_ChildExtendsParent()
    {
        Assert.IsTrue(CausalityFamilyAudit.IsCompatiblePrefix("S3.O2", "S3.O2.1.7"));
        Assert.IsFalse(CausalityFamilyAudit.IsCompatiblePrefix("S3.O2.1.7", "S3.O2"));
    }

    [Test]
    public void ValidateTreeRegistry_RejectsBisectingSnake()
    {
        var registry = new NetworkTreeRegistry();
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "a",
            CausalityLeafPrefix = "S3.O2.1"
        });
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "b",
            CausalityLeafPrefix = "S3.O2.2"
        });
        var result = CausalityFamilyAudit.ValidateTreeRegistry(registry);
        Assert.IsFalse(result.Ok);
        Assert.Greater(result.Violations.Count, 0);
    }

    [Test]
    public void ValidateTreeRegistry_AcceptsLinearChain()
    {
        var registry = new NetworkTreeRegistry();
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "root",
            CausalityLeafPrefix = "S3"
        });
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "child",
            CausalityLeafPrefix = "S3.O2.1"
        });
        var result = CausalityFamilyAudit.ValidateTreeRegistry(registry);
        Assert.IsTrue(result.Ok);
    }
}
#endif
