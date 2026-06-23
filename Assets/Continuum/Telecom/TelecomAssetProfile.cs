using UnityEngine;

namespace Continuum.Telecom
{
    [CreateAssetMenu(fileName = "TelecomAssetProfile", menuName = "Continuum/Telecom Asset Profile")]
    public class TelecomAssetProfile : ScriptableObject
    {
        public AudioClip ringtone;
        public GameObject terminalMeshPrefab;
        public string uscAssetId;
    }
}
