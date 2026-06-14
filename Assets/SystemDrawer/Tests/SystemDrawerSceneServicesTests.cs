using NUnit.Framework;
using UnityEngine;

public class SystemDrawerSceneServicesTests
{
    GameObject _serviceGo;

    [SetUp]
    public void SetUp()
    {
        _serviceGo = new GameObject("SystemDrawerServiceTest");
        _serviceGo.AddComponent<SystemDrawerService>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceGo != null)
            Object.DestroyImmediate(_serviceGo);
    }

    [Test]
    public void TryResolve_ReturnsRegisteredObject()
    {
        var target = new GameObject("Planet");
        SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PlanetBody, target);

        Assert.IsTrue(SystemDrawerSceneServices.TryResolve(SystemDrawerServiceKeys.PlanetBody, out GameObject resolved));
        Assert.AreEqual(target, resolved);
    }

    [Test]
    public void TryResolve_RejectsTypeMismatch()
    {
        var target = new GameObject("Planet");
        SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PlanetBody, target);

        Assert.IsFalse(SystemDrawerSceneServices.TryResolve(SystemDrawerServiceKeys.PlanetBody, out Transform _));
    }

    [Test]
    public void GetUnresolvedRequiredKeys_ListsMissing()
    {
        var missing = SystemDrawerSceneServices.GetUnresolvedRequiredKeys(SystemDrawerServiceKeys.PlanetBody);
        Assert.That(missing, Does.Contain(SystemDrawerServiceKeys.PlanetBody));
    }
}
