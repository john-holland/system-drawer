using UnityEngine;

/// <summary>Menu-style SG2D node for structured chat UI.</summary>
[AddComponentMenu("System Drawer/Networking/Structured Chat Ragdoll Node")]
public class StructuredChatRagdollNode : MenuRagdollNode
{
    public string DisplayLabel;

    protected override bool HandleBubble(MenuRagdollEvent e) => false;

    public void SetLabel(string text)
    {
        DisplayLabel = text ?? "";
        if (!string.IsNullOrEmpty(DisplayLabel))
            gameObject.name = DisplayLabel;
    }
}
