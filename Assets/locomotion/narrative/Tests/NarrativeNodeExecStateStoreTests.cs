using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public class NarrativeNodeExecStateStoreTests
{
    GameObject _storeGo;
    GameObject _targetGo;

    [SetUp]
    public void SetUp()
    {
        _storeGo = new GameObject("store");
        _storeGo.AddComponent<NarrativeNodeExecStateStore>();
        _targetGo = new GameObject("target");
        _targetGo.transform.position = new Vector3(1f, 2f, 3f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_storeGo);
        Object.DestroyImmediate(_targetGo);
    }

    [Test]
    public void CaptureAndRestore_RoundTripPosition()
    {
        var store = NarrativeNodeExecStateStore.Instance;
        Assert.IsNotNull(store);

        string key = store.Capture("evt", "node", new[] { _targetGo }, 10f);
        _targetGo.transform.position = Vector3.zero;
        Assert.IsTrue(store.Restore(key));
        Assert.AreEqual(new Vector3(1f, 2f, 3f), _targetGo.transform.position);
    }
}
