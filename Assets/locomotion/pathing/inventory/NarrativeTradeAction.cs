using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

public enum TradeNarrativePhase
{
    Idle,
    Approach,
    Conversation,
    OfferOpen,
    OfferChanged,
    Accepted,
    Rejected,
    Complete
}

public enum TradeIconMode
{
    TextureBillboard,
    SkinnedMesh
}

/// <summary>Face-to-face trade; transfers only after conversation mode + accept.</summary>
[Serializable]
public sealed class NarrativeTradeAction : NarrativeActionSpec
{
    public string selfKey = "self";
    public string otherKey = "other";
    public string inventoryManagerKey = "inventory";
    public float faceDistance = 1.2f;
    public bool requireConversationBeforeTransfer = true;
    public TradeIconMode iconMode = TradeIconMode.TextureBillboard;
    public Texture2D iconTexture;
    public GameObject iconMeshPrefab;
    public string iconAnimationBtKey = "trade.icon";
    public string dialogueHintFromOffers = true.ToString();
    public List<string> selfOfferItemNames = new List<string>();
    public List<string> otherOfferItemNames = new List<string>();
    public bool aiAutoAccept;

    [NonSerialized] public TradeNarrativePhase phase = TradeNarrativePhase.Idle;
    [NonSerialized] bool _conversationOpen;
    [NonSerialized] GameObject _iconGo;
    [NonSerialized] bool _transferred;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        if (!ctx.TryResolveGameObject(selfKey, out var self) || self == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        if (!ctx.TryResolveGameObject(otherKey, out var other) || other == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;

        switch (phase)
        {
            case TradeNarrativePhase.Idle:
                phase = TradeNarrativePhase.Approach;
                Emit(ctx, "trade.approach");
                return Locomotion.Narrative.BehaviorTreeStatus.Running;

            case TradeNarrativePhase.Approach:
                FaceEachOther(self, other);
                if ((self.transform.position - other.transform.position).sqrMagnitude >
                    faceDistance * faceDistance * 1.5f)
                {
                    // Soft pull toward midpoint
                    Vector3 mid = (self.transform.position + other.transform.position) * 0.5f;
                    self.transform.position = Vector3.MoveTowards(self.transform.position, mid, Time.deltaTime * 1.5f);
                    other.transform.position = Vector3.MoveTowards(other.transform.position, mid, Time.deltaTime * 1.5f);
                    return Locomotion.Narrative.BehaviorTreeStatus.Running;
                }
                phase = TradeNarrativePhase.Conversation;
                _conversationOpen = true;
                Emit(ctx, "trade.conversation");
                EnsureIcon(self, other);
                TryBeginDialogue(self);
                return Locomotion.Narrative.BehaviorTreeStatus.Running;

            case TradeNarrativePhase.Conversation:
            case TradeNarrativePhase.OfferOpen:
            case TradeNarrativePhase.OfferChanged:
                FaceEachOther(self, other);
                UpdateIcon(self, other);
                if (aiAutoAccept)
                {
                    phase = TradeNarrativePhase.Accepted;
                    Emit(ctx, "trade.accept");
                }
                return Locomotion.Narrative.BehaviorTreeStatus.Running;

            case TradeNarrativePhase.Accepted:
                if (requireConversationBeforeTransfer && !_conversationOpen)
                    return Locomotion.Narrative.BehaviorTreeStatus.Failure;
                if (!_transferred)
                {
                    TransferOffers(self, other);
                    _transferred = true;
                    Emit(ctx, "trade.complete");
                }
                phase = TradeNarrativePhase.Complete;
                CleanupIcon();
                return Locomotion.Narrative.BehaviorTreeStatus.Success;

            case TradeNarrativePhase.Rejected:
                Emit(ctx, "trade.reject");
                CleanupIcon();
                phase = TradeNarrativePhase.Idle;
                return Locomotion.Narrative.BehaviorTreeStatus.Failure;

            case TradeNarrativePhase.Complete:
                return Locomotion.Narrative.BehaviorTreeStatus.Success;
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Running;
    }

    public void PlayerAccept()
    {
        if (_conversationOpen ||
            phase == TradeNarrativePhase.Conversation ||
            phase == TradeNarrativePhase.OfferOpen ||
            phase == TradeNarrativePhase.OfferChanged)
        {
            _conversationOpen = true;
            phase = TradeNarrativePhase.Accepted;
        }
    }
    public void PlayerReject() { phase = TradeNarrativePhase.Rejected; }
    public void NotifyOfferChanged()
    {
        if (_conversationOpen)
        {
            phase = TradeNarrativePhase.OfferChanged;
        }
    }

    void TransferOffers(GameObject self, GameObject other)
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;
        string selfId = self.GetComponent<ActorInventory>()?.actorId ?? self.name;
        string otherId = other.GetComponent<ActorInventory>()?.actorId ?? other.name;
        if (selfOfferItemNames != null)
            for (int i = 0; i < selfOfferItemNames.Count; i++)
            {
                mgr.NoteScriptMention(selfOfferItemNames[i]);
                mgr.TryPossessiveOrTransfer(selfOfferItemNames[i], selfId, otherId, requireMention: true);
            }
        if (otherOfferItemNames != null)
            for (int i = 0; i < otherOfferItemNames.Count; i++)
            {
                mgr.NoteScriptMention(otherOfferItemNames[i]);
                mgr.TryPossessiveOrTransfer(otherOfferItemNames[i], otherId, selfId, requireMention: true);
            }
    }

    void FaceEachOther(GameObject a, GameObject b)
    {
        Vector3 d = b.transform.position - a.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 1e-4f)
        {
            a.transform.rotation = Quaternion.Slerp(a.transform.rotation, Quaternion.LookRotation(d), 0.2f);
            b.transform.rotation = Quaternion.Slerp(b.transform.rotation, Quaternion.LookRotation(-d), 0.2f);
        }
    }

    void EnsureIcon(GameObject a, GameObject b)
    {
        if (_iconGo != null) return;
        _iconGo = new GameObject("TradeIcon");
        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f + Vector3.up * 1.4f;
        _iconGo.transform.position = mid;
        if (iconMode == TradeIconMode.SkinnedMesh && iconMeshPrefab != null)
        {
            var inst = UnityEngine.Object.Instantiate(iconMeshPrefab, _iconGo.transform);
            inst.transform.localPosition = Vector3.zero;
        }
        else
        {
            var billboard = _iconGo.AddComponent<TradeIconBillboard>();
            billboard.texture = iconTexture;
        }
    }

    void UpdateIcon(GameObject a, GameObject b)
    {
        if (_iconGo == null) return;
        _iconGo.transform.position = (a.transform.position + b.transform.position) * 0.5f + Vector3.up * 1.4f;
    }

    void CleanupIcon()
    {
        if (_iconGo != null) UnityEngine.Object.Destroy(_iconGo);
        _iconGo = null;
    }

    void TryBeginDialogue(GameObject speaker)
    {
        var runner = speaker.GetComponent<DialogueRunner>() ?? speaker.GetComponentInChildren<DialogueRunner>();
        if (runner == null) return;
        string hint = BuildOfferDialogueHint();
        runner.SendMessage("BeginFromSpanRef", hint, SendMessageOptions.DontRequireReceiver);
    }

    string BuildOfferDialogueHint()
    {
        var parts = new List<string>();
        if (selfOfferItemNames != null)
            for (int i = 0; i < selfOfferItemNames.Count; i++)
                parts.Add(selfOfferItemNames[i]);
        if (otherOfferItemNames != null)
            for (int i = 0; i < otherOfferItemNames.Count; i++)
                parts.Add(otherOfferItemNames[i]);
        return "{P:dialogue|trade=1|items=" + string.Join(",", parts) + "}";
    }

    static void Emit(NarrativeExecutionContext ctx, string evt)
    {
        // Soft event channel for tests / listeners
        Debug.Log($"[NarrativeTrade] {evt}");
    }
}

/// <summary>Simple billboard for trade icon texture.</summary>
public sealed class TradeIconBillboard : MonoBehaviour
{
    public Texture2D texture;
    MeshRenderer _r;

    void Start()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(transform, false);
        _r = quad.GetComponent<MeshRenderer>();
        if (texture != null && _r != null)
        {
            _r.material = new Material(Shader.Find("Sprites/Default"));
            _r.material.mainTexture = texture;
        }
        Destroy(quad.GetComponent<Collider>());
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null)
            transform.forward = cam.transform.forward;
    }
}
