#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

public sealed class InventoryLoadoutTests
{
    [Test]
    public void ScriptMentionGate_BlocksSilentPickup()
    {
        var go = new GameObject("InvMgr");
        try
        {
            var mgr = go.AddComponent<InventoryManager>();
            mgr.scriptMentionGate = true;
            mgr.UpsertLocal(new InventoryItem { id = "1", name = "drink" });
            Assert.IsFalse(mgr.TryPickup("drink", "tim", Vector3.zero));
            mgr.NoteScriptMention("drink");
            Assert.IsTrue(mgr.TryPickup("drink", "tim", Vector3.zero));
            Assert.AreEqual("tim", mgr.FindByName("drink").heldByActorId);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void HaveLemma_MissingName_SilentOk()
    {
        var go = new GameObject("InvMgr2");
        try
        {
            go.AddComponent<InventoryManager>();
            var props = new InventoryLemmaProperties
            {
                op = InventoryLemmaOp.Have,
                item = "not_in_loadouts"
            };
            Assert.AreEqual("ok", InventoryLemmaResolver.Execute(props));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void HaveLemma_Transfer_WhenMentioned()
    {
        var go = new GameObject("InvMgr3");
        try
        {
            var mgr = go.AddComponent<InventoryManager>();
            mgr.UpsertLocal(new InventoryItem { id = "r1", name = "radio", ownedByActorId = "tim" });
            mgr.NoteScriptMention("radio");
            var segs = new List<PromptSegment>
            {
                new PromptSegment
                {
                    isPlaceholder = true,
                    placeholderName = "have",
                    placeholderParams = new Dictionary<string, string>
                    {
                        { "op", "give" }, { "item", "radio" }, { "from", "tim" }, { "to", "sara" }
                    }
                }
            };
            InventoryLemmaResolver.ExecuteFromScript("", segs);
            Assert.AreEqual("sara", mgr.FindByName("radio").heldByActorId);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void PutAwayToVehicleInterior_TransfersIntoVehicleActorInventory()
    {
        var mgrGo = new GameObject("InvMgrPutAway");
        var actorGo = new GameObject("Chef");
        var van = new GameObject("Van");
        try
        {
            var mgr = mgrGo.AddComponent<InventoryManager>();
            mgr.scriptMentionGate = false;
            var actorInv = actorGo.AddComponent<ActorInventory>();
            actorInv.actorId = "Chef";
            var item = new InventoryItem
            {
                id = "flour1",
                name = "flour",
                ownedByActorId = "Chef",
                heldByActorId = "Chef"
            };
            actorInv.items.Add(item);
            mgr.UpsertLocal(item);

            var interior = van.AddComponent<VehicleInterior>();
            Assert.IsTrue(mgr.PutAwayToVehicleInterior(item, interior, "Chef"));

            var vehicleInv = van.GetComponent<ActorInventory>();
            Assert.IsNotNull(vehicleInv);
            Assert.IsNotNull(vehicleInv.FindByName("flour"));
            Assert.IsNull(actorInv.FindByName("flour"));
            Assert.AreEqual(vehicleInv.actorId, item.ownedByActorId);
            Assert.IsNull(item.heldByActorId);
            Assert.AreEqual(van, item.contextGameObject);
        }
        finally
        {
            Object.DestroyImmediate(mgrGo);
            Object.DestroyImmediate(actorGo);
            Object.DestroyImmediate(van);
        }
    }

    [Test]
    public void Trade_DoesNotTransferUntilConversationAccept()
    {
        var self = new GameObject("Self");
        var other = new GameObject("Other");
        var mgrGo = new GameObject("Mgr");
        try
        {
            var mgr = mgrGo.AddComponent<InventoryManager>();
            mgr.UpsertLocal(new InventoryItem { id = "1", name = "knife", ownedByActorId = "Self" });
            mgr.NoteScriptMention("knife");
            self.AddComponent<ActorInventory>().actorId = "Self";
            other.AddComponent<ActorInventory>().actorId = "Other";
            var trade = new NarrativeTradeAction
            {
                selfKey = "self",
                otherKey = "other",
                requireConversationBeforeTransfer = true,
                aiAutoAccept = false,
                selfOfferItemNames = new List<string> { "knife" }
            };
            var bindings = self.AddComponent<NarrativeBindings>();
            bindings.bindings = new List<NarrativeBindings.BindingEntry>
            {
                new NarrativeBindings.BindingEntry { key = "self", value = self },
                new NarrativeBindings.BindingEntry { key = "other", value = other }
            };
            bindings.RebuildIndex();
            var ctx = new NarrativeExecutionContext(null, bindings, null);
            // Idle -> Approach
            Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Running, trade.Execute(ctx, null));
            Assert.AreEqual(TradeNarrativePhase.Approach, trade.phase);
            // Force conversation
            trade.phase = TradeNarrativePhase.Conversation;
            Assert.AreEqual("Self", mgr.FindByName("knife").ownedByActorId);
            trade.PlayerAccept();
            Assert.AreEqual(TradeNarrativePhase.Accepted, trade.phase);
            Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Success, trade.Execute(ctx, null));
            Assert.AreEqual("Other", mgr.FindByName("knife").heldByActorId);
        }
        finally
        {
            Object.DestroyImmediate(self);
            Object.DestroyImmediate(other);
            Object.DestroyImmediate(mgrGo);
        }
    }
}
#endif
