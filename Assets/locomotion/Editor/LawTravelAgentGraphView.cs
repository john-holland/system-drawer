using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LawTravelAgentGraphView : GraphView
{
    LawTravelAgent _agent;
    readonly Dictionary<string, LawStageGraphNode> _nodes =
        new Dictionary<string, LawStageGraphNode>(StringComparer.Ordinal);

    public LawTravelAgentGraphView()
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

    public void Populate(LawTravelAgent agent)
    {
        _agent = agent;
        DeleteElements(graphElements.ToList());
        _nodes.Clear();
        if (agent == null || agent.stages == null) return;
        for (int i = 0; i < agent.stages.Count; i++)
        {
            var data = agent.stages[i];
            if (data == null) continue;
            if (string.IsNullOrEmpty(data.id))
                data.id = Guid.NewGuid().ToString("N");
            float x = 40f + i * 240f;
            float y = 40f;
            var gn = new LawStageGraphNode(data, i, OnRemove);
            gn.SetPosition(new Rect(x, y, 200f, 88f));
            AddElement(gn);
            _nodes[data.id] = gn;
        }
        for (int i = 0; i + 1 < agent.stages.Count; i++)
        {
            var a = agent.stages[i];
            var b = agent.stages[i + 1];
            if (a == null || b == null) continue;
            if (!_nodes.TryGetValue(a.id, out var from)) continue;
            if (!_nodes.TryGetValue(b.id, out var to)) continue;
            AddElement(from.output.ConnectTo(to.input));
        }
    }

    void OnRemove(int index)
    {
        if (_agent == null) return;
        Undo.RecordObject(_agent, "Remove law stage");
        _agent.RemoveStageAt(index);
        EditorUtility.SetDirty(_agent);
        Populate(_agent);
    }
}

sealed class LawStageGraphNode : Node
{
    public Port input;
    public Port output;

    public LawStageGraphNode(LawTravelStage data, int index, Action<int> remove)
    {
        title = data != null ? data.displayName : "Stage";
        input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "";
        inputContainer.Add(input);
        output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "";
        outputContainer.Add(output);
        var bar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var more = new Button(() =>
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () => remove?.Invoke(index));
            menu.ShowAsContext();
        }) { text = "..." };
        bar.Add(more);
        titleContainer.Add(bar);
        RefreshExpandedState();
        RefreshPorts();
    }
}
