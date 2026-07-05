using System;
using System.Collections.Generic;
using UnityEngine;

namespace Continuuuum.Society
{
    [Serializable]
    public class BuildingTypeEntry
    {
        public string buildingTypeId;
        public string displayName;
        public string propertyClass;
        public string prefabId;
        public float defaultOpexUsd;
    }

    [CreateAssetMenu(fileName = "BuildingTypeCatalog", menuName = "Continuuuum/Society/Building Type Catalog")]
    public class BuildingTypeCatalog : ScriptableObject
    {
        public List<BuildingTypeEntry> entries = new();

        public BuildingTypeEntry Find(string buildingTypeId)
        {
            return entries.Find(e => e.buildingTypeId == buildingTypeId);
        }
    }
}
