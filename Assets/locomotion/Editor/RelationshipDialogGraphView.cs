using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GraphView for relationship dialog: X is pinned to state column index (not tree depth).
/// </summary>
public sealed class RelationshipDialogGraphView : GraphView
{
    RelationshipDialogTree _tree;
    readonly Dictionary<string, RelationshipDialogGraphNode> _nodes =
        new Dictionary<string, RelationshipDialogGraphNode>(StringComparer.Ordinal);

    public RelationshipDialogGraphView()
    {
        style.flexGrow = 1f;
        Insert(0, new GridBackground());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var list = new List<Port>();
        ports.ForEach(port =>
        {
            if (port != startPort && port.direction != startPort.direction && port.node != startPort.node)
                list.Add(port);
        });
        return list;
    }

    public void Populate(RelationshipDialogTree tree)
    {
        _tree = tree;
        DeleteElements(graphElements.ToList());
        _nodes.Clear();
        if (tree == null) return;
        tree.EnsureDefaultColumns();
        if (tree.nodes == null) return;

        int[] columnCounts = new int[Mathf.Max(1, tree.columns != null ? tree.columns.Count : 1)];
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var data = tree.nodes[i];
            if (data == null) continue;
            if (string.IsNullOrEmpty(data.id))
                data.id = Guid.NewGuid().ToString("N");
            int col = Mathf.Max(0, data.columnIndex);
            float x = 40f + col * 240f;
            int row = col < columnCounts.Length ? columnCounts[col] : 0;
            if (col < columnCounts.Length) columnCounts[col]++;
            float y = data.y > 1f ? data.y : 40f + row * 120f;
            var gn = new RelationshipDialogGraphNode(data);
            gn.SetPosition(new Rect(x, y, 200f, 80f));
            AddElement(gn);
            _nodes[data.id] = gn;
        }

        if (tree.edges == null) return;
        for (int i = 0; i < tree.edges.Count; i++)
        {
            var e = tree.edges[i];
            if (e == null) continue;
            if (!_nodes.TryGetValue(e.fromNodeId, out var from)) continue;
            if (!_nodes.TryGetValue(e.toNodeId, out var to)) continue;
            var edge = from.output.ConnectTo(to.input);
            AddElement(edge);
        }
    }
}

sealed class RelationshipDialogGraphNode : Node
{
    public RelationshipDialogNode data;
    public Port input;
    public Port output;

    public RelationshipDialogGraphNode(RelationshipDialogNode data)
    {
        this.data = data;
        title = data != null && !string.IsNullOrEmpty(data.title)
            ? data.title
            : (data != null ? data.id : "Node");
        input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "";
        inputContainer.Add(input);
        output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "";
        outputContainer.Add(output);
        RefreshExpandedState();
        RefreshPorts();
    }
}
