using UnityEngine;
using UnityEngine.UI;

namespace Continuuuum.Credits
{
    public sealed class CreditsEntryView : MonoBehaviour
    {
        public Text nameText;
        public Text metaText;

        public bool Bind(CreditsEntryDto entry)
        {
            if (entry == null || !entry.IsVisible)
            {
                gameObject.SetActive(false);
                return false;
            }
            gameObject.SetActive(true);
            if (nameText != null)
                nameText.text = entry.DisplayName;
            if (metaText != null)
            {
                var bits = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(entry.company)) bits.Add(entry.company);
                if (!string.IsNullOrEmpty(entry.years)) bits.Add(entry.years);
                if (!string.IsNullOrEmpty(entry.rightsMarks)) bits.Add(entry.rightsMarks);
                if (!string.IsNullOrEmpty(entry.quote)) bits.Add(entry.quote);
                metaText.text = string.Join(" · ", bits);
            }
            return true;
        }
    }
}
