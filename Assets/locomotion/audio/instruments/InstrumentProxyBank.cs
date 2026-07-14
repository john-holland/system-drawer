using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Registry of proxy voices for assembled pieces / orchestral routing.</summary>
    public sealed class InstrumentProxyBank : MonoBehaviour
    {
        public List<InstrumentProxy> proxies = new List<InstrumentProxy>();

        [Range(0f, 1f)]
        public float playerInteractionQuantize01 = 0.5f;

        public bool TryGet(string proxyVoiceId, out InstrumentProxy proxy)
        {
            proxy = null;
            if (string.IsNullOrEmpty(proxyVoiceId) || proxies == null) return false;
            for (int i = 0; i < proxies.Count; i++)
            {
                if (proxies[i] != null && proxies[i].proxyVoiceId == proxyVoiceId)
                {
                    proxy = proxies[i];
                    return true;
                }
            }
            return false;
        }

        public void ApplyBankQuantize()
        {
            if (proxies == null) return;
            for (int i = 0; i < proxies.Count; i++)
            {
                if (proxies[i] != null)
                    proxies[i].playerInteractionQuantize01 = playerInteractionQuantize01;
            }
        }

        public void Register(InstrumentProxy proxy)
        {
            if (proxy == null) return;
            if (!proxies.Contains(proxy))
                proxies.Add(proxy);
        }
    }
}
