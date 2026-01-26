using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

/// <summary>
/// Custom GraphView node for representing behavior tree nodes in state machine style (supports branching).
/// </summary>
public class AnimationStateMachineNode : Node
{
    private BehaviorTreeNode behaviorNode;
    private NodeType nodeType;
    private float estimatedDuration;
    private bool isBreakoutNode;
    private GoodSection physicsCardPreview;

    public AnimationStateMachineNode(BehaviorTreeNode node)
    {
        behaviorNode = node;
        if (node != null)
        {
            nodeType = node.nodeType;
            estimatedDuration = node.estimatedDuration;
        }

        // Create basic node structure
        title = node != null ? node.GetType().Name : "Unknown Node";
        
        // Add input port
        var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Add output ports based on node type
        CreateBranchingPorts();

        // Update visualization
        UpdateVisualization();
    }

    /// <summary>
    /// Create multiple output ports for branching nodes.
    /// </summary>
    private void CreateBranchingPorts()
    {
        if (behaviorNode == null)
            return;

        // Clear existing output ports
        outputContainer.Clear();

        switch (nodeType)
        {
            case NodeType.Selector:
                // Multiple output ports (one per child)
                if (behaviorNode.children != null)
                {
                    for (int i = 0; i < behaviorNode.children.Count; i++)
                    {
                        var child = behaviorNode.children[i];
                        if (child != null)
                        {
                            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                            port.portName = $"Child {i}";
                            outputContainer.Add(port);
                        }
                    }
                }
                break;

            case NodeType.Condition:
                // Two output ports (true/false)
                var truePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                truePort.portName = "True";
                outputContainer.Add(truePort);

                var falsePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                falsePort.portName = "False";
                outputContainer.Add(falsePort);
                break;

            case NodeType.Sequence:
            default:
                // Single output port
                var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "Output";
                outputContainer.Add(outputPort);
                break;
        }
    }

    /// <summary>
    /// Update node appearance based on state.
    /// </summary>
    public void UpdateVisualization()
    {
        // Set color based on node type
        Color nodeColor = Color.blue; // Default

        if (isBreakoutNode)
        {
            nodeColor = new Color(1f, 0.5f, 0f); // Orange for breakout nodes
        }
        else if (nodeType == NodeType.Selector || nodeType == NodeType.Condition)
        {
            nodeColor = Color.green; // Green for branching nodes
        }

        // Apply color to title container
        titleContainer.style.backgroundColor = new StyleColor(nodeColor);

        // Update title with duration
        if (behaviorNode != null)
        {
            title = $"{behaviorNode.GetType().Name} ({estimatedDuration:F2}s)";
        }
    }

    /// <summary>
    /// Handle node selection (syncs to timeline).
    /// </summary>
    public new void OnSelected()
    {
        // This would sync to timeline scrubber
        // Implementation depends on timeline window integration
    }

    /// <summary>
    /// Draw physics card preview in tooltip/hover.
    /// </summary>
    public void DrawPhysicsCardPreview()
    {
        if (physicsCardPreview != null)
        {
            // Create tooltip with card info
            tooltip = $"Card: {physicsCardPreview.sectionName}\nDescription: {physicsCardPreview.description}";
        }
    }
}
