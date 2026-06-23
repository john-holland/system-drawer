using UnityEngine;

namespace Continuum.Telecom
{
    [CreateAssetMenu(fileName = "TelecomNetworkProfile", menuName = "Continuum/Telecom Network Profile")]
    public class TelecomNetworkProfile : ScriptableObject
    {
        public string playbookPath = "base/ubiquitous-net.playbook.yaml";
    }
}
