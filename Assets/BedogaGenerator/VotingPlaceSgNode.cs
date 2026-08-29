using UnityEngine;

/// <summary>Local SG node for a voting place. Default in-paint: queued by address, or randomly, if so.</summary>
[AddComponentMenu("Bedoga/Voting Place SG Node")]
public sealed class VotingPlaceSgNode : SGBehaviorTreeNode
{
    [TextArea] public string inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
    public bool executed;
    public string[] lastTokens;
    public IfPredicateHit[] lastIfs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachToHubs()
    {
        var hubs = Object.FindObjectsByType<VotingQueueHub>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hubs.Length; i++)
            if (hubs[i] != null)
                Ensure(hubs[i].gameObject);
    }

    public static VotingPlaceSgNode Ensure(GameObject host)
    {
        if (host == null) return null;
        var node = host.GetComponent<VotingPlaceSgNode>();
        if (node == null)
            node = host.AddComponent<VotingPlaceSgNode>();
        var hub = host.GetComponent<VotingQueueHub>();
        if (hub != null && !string.IsNullOrEmpty(hub.inpaintPrompt))
            node.inpaintPrompt = hub.inpaintPrompt;
        else if (string.IsNullOrEmpty(node.inpaintPrompt))
            node.inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
        node.ExecuteInpaint();
        return node;
    }

    void Awake()
    {
        ExecuteInpaint();
    }

    public string ExecuteInpaint()
    {
        if (string.IsNullOrEmpty(inpaintPrompt))
            inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
        ResolveTokens();
        SendMessage("OnVotingPlaceInpaint", inpaintPrompt, SendMessageOptions.DontRequireReceiver);
        return inpaintPrompt;
    }

    void OnVotingPlaceInpaint(string prompt)
    {
        if (!string.IsNullOrEmpty(prompt))
            inpaintPrompt = prompt;
        ResolveTokens();
    }

    public string[] ResolveTokens()
    {
        if (string.IsNullOrEmpty(inpaintPrompt))
            inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
        lastTokens = AdverbIfPostfix.ApplyToText(inpaintPrompt);
        lastIfs = IfPredicate.FindAll(lastTokens);
        executed = true;
        return lastTokens;
    }
}
