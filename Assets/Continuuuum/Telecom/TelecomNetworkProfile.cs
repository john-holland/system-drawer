using UnityEngine;

namespace Continuuuum.Telecom
{
    [CreateAssetMenu(fileName = "TelecomNetworkProfile", menuName = "Continuuuum/Telecom Network Profile")]
    public class TelecomNetworkProfile : ScriptableObject
    {
        public string playbookPath = "base/ubiquitous-net.playbook.yaml";
    }
}
