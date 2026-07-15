using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Continuuuum.Credits
{
    /// <summary>Canvas credits UI laid out in screen-space quadrants.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CreditsQuadTreeUI : MonoBehaviour
    {
        public CreditsApiClient apiClient;
        public string listId;
        public string episodeId;
        public bool loadOnStart = true;
        public bool updateListOnStart;
        public string updateMode = "work_orders";
        public CreditsSectionView sectionPrefab;
        public CreditsSpecialUiLeaf specialPrefab;
        public CreditsEntryView entryPrefab;
        public RectTransform leafHost;

        CreditsListDto _list;
        readonly List<CreditsSectionView> _sections = new List<CreditsSectionView>();
        readonly List<GameObject> _leafGos = new List<GameObject>();

        async void Start()
        {
            if (!loadOnStart || string.IsNullOrEmpty(listId) || apiClient == null)
                return;
            try
            {
                if (updateListOnStart)
                    _list = await apiClient.UpdateListAsync(listId, updateMode, episodeId);
                else
                    _list = await apiClient.GetListAsync(listId, includeHidden: false);
                Rebuild();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CreditsQuadTreeUI] {ex.Message}");
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _sections.Count; i++)
                _sections[i]?.Tick(dt);
        }

        public void Rebuild()
        {
            ClearLeaves();
            if (_list == null || leafHost == null)
                return;

            var hostRect = leafHost.rect;
            var tree = new CreditsQuadTree(new Rect(0, 0, hostRect.width, hostRect.height));
            var sections = _list.sections ?? new List<CreditsSectionDto>();
            for (int i = 0; i < sections.Count; i++)
            {
                var s = sections[i];
                string path = string.IsNullOrEmpty(s.quadrantPath) ? $"R.{i % 4}" : s.quadrantPath;
                var node = tree.EnsurePath(path);
                node.section = s;
                node.specialUi = s.isSpecialUi;
            }

            var bySection = new Dictionary<string, List<CreditsEntryDto>>();
            if (_list.entries != null)
            {
                for (int i = 0; i < _list.entries.Count; i++)
                {
                    var e = _list.entries[i];
                    if (e == null || !e.IsVisible)
                        continue;
                    if (!bySection.TryGetValue(e.sectionId ?? "", out var list))
                    {
                        list = new List<CreditsEntryDto>();
                        bySection[e.sectionId ?? ""] = list;
                    }
                    list.Add(e);
                }
            }

            foreach (var leaf in tree.Leaves())
            {
                if (leaf.section == null)
                    continue;
                var go = new GameObject($"Leaf_{leaf.pathId}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(leafHost, false);
                var rt = go.GetComponent<RectTransform>();
                ApplyLeafRect(rt, leaf.rect, hostRect);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.05f, 0.06f, 0.08f, 0.85f);
                _leafGos.Add(go);

                if (leaf.specialUi && specialPrefab != null)
                {
                    var special = Instantiate(specialPrefab, rt);
                    special.Bind(leaf.section);
                }
                else if (sectionPrefab != null)
                {
                    var view = Instantiate(sectionPrefab, rt);
                    view.entryPrefab = entryPrefab;
                    bySection.TryGetValue(leaf.section.id, out var entries);
                    view.Bind(leaf.section, entries ?? new List<CreditsEntryDto>());
                    _sections.Add(view);
                }
            }
        }

        static void ApplyLeafRect(RectTransform rt, Rect leaf, Rect host)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(leaf.x, leaf.y);
            rt.sizeDelta = new Vector2(leaf.width, leaf.height);
        }

        void ClearLeaves()
        {
            _sections.Clear();
            for (int i = 0; i < _leafGos.Count; i++)
            {
                if (_leafGos[i] != null)
                    Destroy(_leafGos[i]);
            }
            _leafGos.Clear();
        }
    }
}
