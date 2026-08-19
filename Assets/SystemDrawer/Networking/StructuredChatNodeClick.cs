using UnityEngine;

/// <summary>Click plank that appends a lexicon word into the chat ragdoll.</summary>
public sealed class StructuredChatNodeClick : MonoBehaviour
{
    public string wordId;
    public string eventName = "chat.word";

    void OnMouseDown()
    {
        Click();
    }

    public void Click()
    {
        var node = GetComponent<StructuredChatRagdollNode>();
        if (node == null)
            return;
        if (!string.IsNullOrEmpty(wordId))
            node.Send("chat.word", wordId);
        else
            node.Send(string.IsNullOrEmpty(eventName) ? "chat.send" : eventName);
    }
}
