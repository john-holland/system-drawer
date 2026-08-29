using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RelationshipDialogColumn
{
    public string label;
    public RomanceSeverity severity = RomanceSeverity.Notion;
}

[Serializable]
public sealed class RelationshipDialogNode
{
    public string id;
    public int columnIndex;
    public string dialogTreeSetId;
    public string title;
    public float y = 40f;
}

[Serializable]
public sealed class RelationshipDialogEdge
{
    public string fromNodeId;
    public string toNodeId;
}

/// <summary>
/// Authoring graph for relationship dialog. Columns are relationship states; nodes are beats
/// compatible with Scribe <c>dialogTreeSetId</c>. Does not fork the narrative tree asset format.
/// </summary>
[CreateAssetMenu(fileName = "RelationshipDialogTree", menuName = "Locomotion/Civil/Relationship Dialog Tree")]
public sealed class RelationshipDialogTree : ScriptableObject
{
    public List<RelationshipDialogColumn> columns = new List<RelationshipDialogColumn>();
    public List<RelationshipDialogNode> nodes = new List<RelationshipDialogNode>();
    public List<RelationshipDialogEdge> edges = new List<RelationshipDialogEdge>();

    public void EnsureDefaultColumns()
    {
        if (columns != null && columns.Count > 0) return;
        columns = new List<RelationshipDialogColumn>
        {
            new RelationshipDialogColumn { label = "FriendZone", severity = RomanceSeverity.FriendZone },
            new RelationshipDialogColumn { label = "Crush", severity = RomanceSeverity.Crush },
            new RelationshipDialogColumn { label = "GoingOut", severity = RomanceSeverity.GoingOut },
            new RelationshipDialogColumn { label = "GoingSteady", severity = RomanceSeverity.GoingSteady },
            new RelationshipDialogColumn { label = "Married", severity = RomanceSeverity.Married }
        };
    }

    public bool HasEdge(string fromNodeId, string toNodeId)
    {
        if (edges == null || string.IsNullOrEmpty(fromNodeId) || string.IsNullOrEmpty(toNodeId))
            return false;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e != null && e.fromNodeId == fromNodeId && e.toNodeId == toNodeId)
                return true;
        }
        return false;
    }

    public void AddEdge(string fromNodeId, string toNodeId)
    {
        if (string.IsNullOrEmpty(fromNodeId) || string.IsNullOrEmpty(toNodeId)) return;
        if (HasEdge(fromNodeId, toNodeId)) return;
        if (edges == null) edges = new List<RelationshipDialogEdge>();
        edges.Add(new RelationshipDialogEdge { fromNodeId = fromNodeId, toNodeId = toNodeId });
    }

    public RelationshipDialogNode FindNode(string id)
    {
        if (nodes == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == id)
                return nodes[i];
        }
        return null;
    }

    public RelationshipDialogNode PickInColumn(int columnIndex)
    {
        if (nodes == null) return null;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n != null && n.columnIndex == columnIndex)
                return n;
        }
        return null;
    }

    public void Fire(GameObject host, RelationshipDialogNode node)
    {
        if (host == null || node == null) return;
        string id = !string.IsNullOrEmpty(node.dialogTreeSetId) ? node.dialogTreeSetId : node.id;
        host.SendMessage("OnDialogTree", id, SendMessageOptions.DontRequireReceiver);
    }
}
