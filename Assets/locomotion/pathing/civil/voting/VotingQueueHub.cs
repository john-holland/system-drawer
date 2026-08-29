using System.Collections.Generic;
using UnityEngine;

/// <summary>Central ingress queue fans out to painted (or default) feeder lanes, then booth stations.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Voting Queue Hub")]
public sealed class VotingQueueHub : MonoBehaviour
{
    public LaneGrid centralQueue;
    public List<LaneGrid> feeders = new List<LaneGrid>();
    public List<VotingBoothStation> booths = new List<VotingBoothStation>();
    public VotingPlaceCard placeCard;
    public VotePropertyBag propertyBag;
    public int defaultFeederCount = 1;
    public int assignSeed = 17;
    public VotingBoothQueueLayout defaultBoothLayout = VotingBoothQueueLayout.Single;
    public Dictionary<BaseAmbulatingActor, string> homeAddresses = new Dictionary<BaseAmbulatingActor, string>();
    [TextArea] public string inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;

    public const string DefaultInpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;

    public LaneGrid CentralQueue
    {
        get
        {
            if (centralQueue == null)
                centralQueue = GetComponent<LaneGrid>();
            return centralQueue;
        }
    }

    public bool InpaintBlocksAdvance()
    {
        var card = placeCard;
        if (card == null)
        {
            var bio = GetComponent<VotingPlaceBioRhythm>();
            card = bio != null ? bio.perimeter : null;
        }
        return card != null && card.developerInpaint;
    }

    public void SetHomeAddress(BaseAmbulatingActor actor, string address)
    {
        if (actor == null) return;
        if (homeAddresses == null) homeAddresses = new Dictionary<BaseAmbulatingActor, string>();
        if (string.IsNullOrEmpty(address))
            homeAddresses.Remove(actor);
        else
            homeAddresses[actor] = address;
    }

    public string HomeAddressFor(BaseAmbulatingActor actor)
    {
        if (actor != null && homeAddresses != null && homeAddresses.TryGetValue(actor, out var mapped) && !string.IsNullOrEmpty(mapped))
            return mapped;
        if (propertyBag != null)
        {
            var fromBag = propertyBag.Get(VotePropertyBag.HomeAddressKey)
                          ?? propertyBag.Get(VoteLemmaPropertyKeys.HomeAddress);
            if (!string.IsNullOrEmpty(fromBag))
                return fromBag;
        }
        return null;
    }

    public void CollectFeeders()
    {
        if (feeders == null) feeders = new List<LaneGrid>();
        var central = CentralQueue;
        var found = GetComponentsInChildren<LaneGrid>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var g = found[i];
            if (g == null || g == central || feeders.Contains(g)) continue;
            if (g.GetComponentInParent<VotingBoothStation>() != null) continue;
            feeders.Add(g);
        }
        if (feeders.Count == 0)
            EnsureDefaultFeeders();
        for (int i = 0; i < feeders.Count; i++)
            if (feeders[i] != null)
                feeders[i].EnsureCells();
    }

    public void EnsureBooths()
    {
        if (booths == null) booths = new List<VotingBoothStation>();
        var found = GetComponentsInChildren<VotingBoothStation>(true);
        for (int i = 0; i < found.Length; i++)
            if (found[i] != null && !booths.Contains(found[i]))
                booths.Add(found[i]);
        if (booths.Count == 0)
        {
            var child = new GameObject("voting_booth");
            child.transform.SetParent(transform, false);
            var booth = child.AddComponent<VotingBoothStation>();
            booth.layout = defaultBoothLayout;
            booth.hub = this;
            booths.Add(booth);
        }
        int needFeeders = Mathf.Max(1, defaultFeederCount);
        if (booths.Count > needFeeders)
            needFeeders = booths.Count;
        defaultFeederCount = needFeeders;
        for (int i = 0; i < booths.Count; i++)
        {
            if (booths[i] == null) continue;
            booths[i].hub = this;
            booths[i].EnsureSections();
        }
    }

    public int AssignFeederIndex(string homeAddress, int actorSeed)
    {
        CollectFeeders();
        int n = 0;
        for (int i = 0; i < feeders.Count; i++)
            if (feeders[i] != null) n++;
        if (n <= 0) return -1;
        int pick;
        if (!string.IsNullOrEmpty(homeAddress))
            pick = StableHash(homeAddress) % n;
        else
            pick = new System.Random(assignSeed ^ actorSeed).Next(n);
        int seen = 0;
        for (int i = 0; i < feeders.Count; i++)
        {
            if (feeders[i] == null) continue;
            if (seen == pick) return i;
            seen++;
        }
        return 0;
    }

    public LaneGrid AssignFeeder(BaseAmbulatingActor actor)
    {
        int idx = AssignFeederIndex(HomeAddressFor(actor), actor != null ? actor.GetInstanceID() : 0);
        if (idx < 0 || idx >= feeders.Count) return null;
        return feeders[idx];
    }

    public bool TryAdvance()
    {
        if (InpaintBlocksAdvance()) return false;
        var central = CentralQueue;
        if (central == null || central.Peek() == null) return false;
        CollectFeeders();
        var actor = central.Peek();
        var feeder = AssignFeeder(actor);
        if (feeder == null || !feeder.TryEnqueue(actor)) return false;
        central.TryDequeueToBooth();
        return true;
    }

    public bool TryAdvanceToBooth()
    {
        if (InpaintBlocksAdvance()) return false;
        EnsureBooths();
        CollectFeeders();
        bool moved = false;
        for (int i = 0; i < feeders.Count; i++)
        {
            var feeder = feeders[i];
            if (feeder == null || feeder.Peek() == null) continue;
            var booth = BoothForFeeder(i);
            if (booth == null) continue;
            var actor = feeder.Peek();
            if (!booth.TryAccept(actor)) continue;
            feeder.TryDequeueToBooth();
            moved = true;
        }
        return moved;
    }

    public bool TryOccupyBooths()
    {
        if (InpaintBlocksAdvance()) return false;
        EnsureBooths();
        bool any = false;
        for (int i = 0; i < booths.Count; i++)
            if (booths[i] != null && booths[i].TryOccupyHead())
                any = true;
        return any;
    }

    public void Tick()
    {
        TryAdvance();
        TryAdvanceToBooth();
        TryOccupyBooths();
    }

    public string ExecuteInpaintPrompt()
    {
        if (string.IsNullOrEmpty(inpaintPrompt))
            inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
        SendMessage("OnVotingPlaceInpaint", inpaintPrompt, SendMessageOptions.DontRequireReceiver);
        return inpaintPrompt;
    }

    VotingBoothStation BoothForFeeder(int feederIndex)
    {
        if (booths == null || booths.Count == 0) return null;
        int n = 0;
        for (int i = 0; i < booths.Count; i++)
            if (booths[i] != null) n++;
        if (n == 0) return null;
        int pick = ((feederIndex % n) + n) % n;
        int seen = 0;
        for (int i = 0; i < booths.Count; i++)
        {
            if (booths[i] == null) continue;
            if (seen == pick) return booths[i];
            seen++;
        }
        return booths[0];
    }

    void EnsureDefaultFeeders()
    {
        EnsureBooths();
        int need = Mathf.Max(1, defaultFeederCount);
        if (booths != null && booths.Count > need)
            need = booths.Count;
        for (int i = feeders.Count; i < need; i++)
        {
            var child = new GameObject("feeder_" + i);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3((i + 1) * 1.2f, 0f, 0f);
            var grid = child.AddComponent<LaneGrid>();
            grid.width = 1;
            grid.height = 6;
            grid.EnsureCells();
            feeders.Add(grid);
        }
    }

    static int StableHash(string s)
    {
        unchecked
        {
            int h = 23;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h & 0x7fffffff;
        }
    }
}
