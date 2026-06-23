using System.IO;
using UnityEngine;

namespace Continuum.Telecom
{
    /// <summary>Resolves playbook resources (static vs USC).</summary>
    public class TelecomPlaybookLoader : MonoBehaviour
    {
        public TelecomNetworkProfile networkProfile;
        public string playbooksRoot = "telecom/playbooks";

        public string ResolveResourcePath(string relativePath)
        {
            var root = Path.Combine(Application.dataPath, "..", playbooksRoot);
            return Path.GetFullPath(Path.Combine(root, relativePath));
        }

        public AudioClip LoadRingtone(TelecomAssetProfile assetProfile)
        {
            return assetProfile != null ? assetProfile.ringtone : null;
        }
    }
}
