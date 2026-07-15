using UnityEngine;
using UnityEngine.UI;

namespace Continuuuum.Credits
{
    /// <summary>Interactive / logo leaf for isSpecialUi sections (no scroll).</summary>
    public sealed class CreditsSpecialUiLeaf : MonoBehaviour
    {
        public Text titleText;
        public Transform customSlot;

        public void Bind(CreditsSectionDto section)
        {
            if (titleText != null)
                titleText.text = section != null ? section.title : "";
        }
    }
}
