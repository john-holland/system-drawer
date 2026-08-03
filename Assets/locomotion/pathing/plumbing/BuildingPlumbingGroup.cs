using System.Collections.Generic;
using UnityEngine;

/// <summary>Groups fixtures in a building; optional toilet-flush cross-talk to sink/shower hot.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Building Plumbing Group")]
public sealed class BuildingPlumbingGroup : MonoBehaviour
{
    public string groupId = "default";
    [Range(0f, 1f)] public float ToiletFlushCrossTalk01 { get; private set; }
    public float crossTalkDecayPerSec = 1.2f;
    readonly List<FixturePlumbingNode> _fixtures = new List<FixturePlumbingNode>();

    void Awake()
    {
        if (string.IsNullOrEmpty(groupId))
            groupId = gameObject.name;
        GetComponentsInChildren(true, _fixtures);
        for (int i = 0; i < _fixtures.Count; i++)
        {
            if (_fixtures[i] != null)
            {
                _fixtures[i].plumbingGroup = this;
                _fixtures[i].buildingPlumbingGroupId = groupId;
            }
        }
    }

    void Update()
    {
        if (ToiletFlushCrossTalk01 > 0f)
            ToiletFlushCrossTalk01 = Mathf.MoveTowards(ToiletFlushCrossTalk01, 0f, crossTalkDecayPerSec * Time.deltaTime);
    }

    public void NotifyToiletFlushed(float intensity01 = 1f)
    {
        ToiletFlushCrossTalk01 = Mathf.Clamp01(Mathf.Max(ToiletFlushCrossTalk01, intensity01));
    }
}
