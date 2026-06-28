using UnityEngine;

namespace Continuum.Society
{
    /// <summary>Stable building metadata synced with society building_registry.</summary>
    public class BuildingMetadata : MonoBehaviour
    {
        public string stableId;
        public string buildingTypeId;
        public string lemmaEntryId;
        public string prefabRef;
        public string zoneId;
        public string propertyClass;
        public string cityId;
    }
}
