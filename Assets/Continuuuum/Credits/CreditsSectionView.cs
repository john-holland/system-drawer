using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Continuuuum.Credits
{
    public sealed class CreditsSectionView : MonoBehaviour
    {
        public Text titleText;
        public RectTransform content;
        public CreditsEntryView entryPrefab;
        public float scrollSpeed = 40f;

        readonly List<CreditsEntryView> _rows = new List<CreditsEntryView>();
        float _offset;

        public void Bind(CreditsSectionDto section, List<CreditsEntryDto> entries)
        {
            Clear();
            if (section == null)
                return;
            scrollSpeed = section.scrollSpeed > 0f ? section.scrollSpeed : 40f;
            if (titleText != null)
                titleText.text = section.title;

            if (entries == null || entryPrefab == null || content == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || !e.IsVisible)
                    continue;
                var row = Instantiate(entryPrefab, content);
                if (!row.Bind(e))
                {
                    Destroy(row.gameObject);
                    continue;
                }
                _rows.Add(row);
            }
            _offset = 0f;
        }

        public void Tick(float dt)
        {
            if (content == null || _rows.Count == 0)
                return;
            float speed = scrollSpeed;
            _offset += speed * dt;
            var p = content.anchoredPosition;
            p.y = _offset;
            content.anchoredPosition = p;
            float h = content.rect.height;
            if (_offset > h + 200f)
            {
                _offset = -200f;
                p.y = _offset;
                content.anchoredPosition = p;
            }
        }

        void Clear()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }
            _rows.Clear();
        }
    }
}
