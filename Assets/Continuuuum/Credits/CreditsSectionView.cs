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
        readonly List<float> _rowSpeeds = new List<float>();
        readonly List<float> _rowOffsets = new List<float>();
        float _offset;
        bool _usePerEntrySpeeds;

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

            _usePerEntrySpeeds = false;
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
                float speed = e.scrollSpeed.HasValue && e.scrollSpeed.Value > 0f
                    ? e.scrollSpeed.Value
                    : scrollSpeed;
                if (e.scrollSpeed.HasValue && e.scrollSpeed.Value > 0f)
                    _usePerEntrySpeeds = true;
                _rows.Add(row);
                _rowSpeeds.Add(speed);
                _rowOffsets.Add(0f);
            }
            _offset = 0f;
        }

        public void Tick(float dt)
        {
            if (content == null || _rows.Count == 0)
                return;

            if (_usePerEntrySpeeds)
            {
                TickPerEntry(dt);
                return;
            }

            _offset += scrollSpeed * dt;
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

        void TickPerEntry(float dt)
        {
            float spacing = 48f;
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row == null)
                    continue;
                var rt = row.transform as RectTransform;
                if (rt == null)
                    continue;
                _rowOffsets[i] += _rowSpeeds[i] * dt;
                float y = _rowOffsets[i] + i * spacing;
                float wrap = Mathf.Max(200f, content.rect.height + 200f);
                if (y > wrap)
                    _rowOffsets[i] -= wrap + spacing * _rows.Count;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
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
            _rowSpeeds.Clear();
            _rowOffsets.Clear();
            _usePerEntrySpeeds = false;
        }
    }
}
