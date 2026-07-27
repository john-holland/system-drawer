using System.Collections.Generic;
using UnityEngine;

/// <summary>In-game trade UI: yours / your offer / their offer + accept/reject.</summary>
[AddComponentMenu("Locomotion/UI/Trade Panel")]
public sealed class TradePanel : MonoBehaviour
{
    public InventoryManager inventory;
    public ActorInventory playerInventory;
    public string playerActorId = "player";
    public GameObject preselectedOther;
    public bool disableActorDropdown;
    public NarrativeTradeAction boundTradeAction;

    public readonly List<InventoryItem> yourOffer = new List<InventoryItem>();
    public readonly List<InventoryItem> theirOffer = new List<InventoryItem>();
    ActorInventory _other;
    Vector2 _scroll;
    bool _visible = true;

    public void Show(GameObject other = null, bool lockDropdown = false)
    {
        if (other != null) preselectedOther = other;
        disableActorDropdown = lockDropdown || disableActorDropdown;
        _other = preselectedOther != null
            ? preselectedOther.GetComponent<ActorInventory>() ?? preselectedOther.GetComponentInChildren<ActorInventory>()
            : null;
        _visible = true;
    }

    public void Hide() => _visible = false;

    void OnGUI()
    {
        if (!_visible) return;
        float w = 520f, h = 360f;
        Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(r, "Trade");
        GUILayout.BeginArea(new Rect(r.x + 10, r.y + 28, w - 20, h - 40));
        if (!disableActorDropdown)
        {
            GUILayout.Label("Trade with");
            // Simple object field substitute: name list of ActorInventory in scene
            var all = FindObjectsByType<ActorInventory>(FindObjectsSortMode.None);
            string[] names = new string[all.Length];
            int sel = 0;
            for (int i = 0; i < all.Length; i++)
            {
                names[i] = all[i].actorId;
                if (_other == all[i]) sel = i;
            }
            if (names.Length > 0)
            {
                int n = GUILayout.SelectionGrid(sel, names, Mathf.Min(4, names.Length));
                _other = all[n];
            }
        }
        else
            GUILayout.Label("Partner: " + (_other != null ? _other.actorId : (preselectedOther != null ? preselectedOther.name : "(none)")));

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
        GUILayout.BeginHorizontal();
        DrawGrid("Yours", playerInventory != null ? playerInventory.items : null, yourOffer, true);
        DrawGrid("Your offer", yourOffer, null, false);
        DrawGrid("Their offer", theirOffer, null, false);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Accept"))
        {
            ApplyOffersToAction();
            boundTradeAction?.PlayerAccept();
            Hide();
        }
        if (GUILayout.Button("Reject"))
        {
            boundTradeAction?.PlayerReject();
            Hide();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DrawGrid(string title, List<InventoryItem> source, List<InventoryItem> clickTarget, bool clickMovesToOffer)
    {
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label(title);
        if (source == null)
        {
            GUILayout.Label("(empty)");
            GUILayout.EndVertical();
            return;
        }
        for (int i = 0; i < source.Count; i++)
        {
            var it = source[i];
            if (it == null) continue;
            if (GUILayout.Button(it.name ?? "?"))
            {
                if (clickMovesToOffer && clickTarget != null && !clickTarget.Contains(it))
                {
                    clickTarget.Add(it);
                    boundTradeAction?.NotifyOfferChanged();
                }
            }
        }
        GUILayout.EndVertical();
    }

    void ApplyOffersToAction()
    {
        if (boundTradeAction == null) return;
        boundTradeAction.selfOfferItemNames.Clear();
        boundTradeAction.otherOfferItemNames.Clear();
        for (int i = 0; i < yourOffer.Count; i++)
            if (yourOffer[i] != null) boundTradeAction.selfOfferItemNames.Add(yourOffer[i].name);
        for (int i = 0; i < theirOffer.Count; i++)
            if (theirOffer[i] != null) boundTradeAction.otherOfferItemNames.Add(theirOffer[i].name);
        if (_other != null)
        {
            // peek their inventory into theirOffer if empty
            if (theirOffer.Count == 0 && _other.items != null)
                for (int i = 0; i < _other.items.Count && i < 3; i++)
                    if (_other.items[i] != null)
                        boundTradeAction.otherOfferItemNames.Add(_other.items[i].name);
        }
    }
}
