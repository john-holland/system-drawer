using System;
using System.Collections.Generic;
using UnityEngine;

namespace Continuum.Society
{
    [Serializable]
    public class CityRoutingVisit
    {
        public string stableId;
        public string zoneId;
        public string buildingTypeId;
        public string displayName;
    }

    [CreateAssetMenu(fileName = "CityBehaviorTree", menuName = "Continuum/Society/City Behavior Tree")]
    public class CityBehaviorTreeAsset : ScriptableObject
    {
        public string cityId;
        public List<CityRoutingVisit> visitOrder = new();
        public int timelineFrameIndex;
    }
}
