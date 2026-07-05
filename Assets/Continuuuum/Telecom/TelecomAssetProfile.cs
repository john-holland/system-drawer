using UnityEngine;

namespace Continuuuum.Telecom
{
    [CreateAssetMenu(fileName = "TelecomAssetProfile", menuName = "Continuuuum/Telecom Asset Profile")]
    public class TelecomAssetProfile : ScriptableObject
    {
        public AudioClip ringtone;
        public GameObject terminalMeshPrefab;
        public string uscAssetId;
    }
}
