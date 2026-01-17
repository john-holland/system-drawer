using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Temporal graph structure for good sections. Represents connections between sections
/// as state transitions. Supports angular topology for pathfinding through physical space.
/// </summary>
public class TemporalGraph
{
    // Graph structure
    private Dictionary<GoodSection, List<GoodSection>> nodes = new Dictionary<GoodSection, List<GoodSection>>();
    private Dictionary<GoodSection, Dictionary<GoodSection, float>> edgeWeights = new Dictionary<GoodSection, Dictionary<GoodSection, float>>();
    private Dictionary<GoodSection, RagdollState> stateTransitions = new Dictionary<GoodSection, RagdollState>();

    /// <summary>
    /// Add a node (good section) to the graph.
    /// </summary>
    public void AddNode(GoodSection node)
    {
        if (node == null)
            return;

        if (!nodes.ContainsKey(node))
        {
            nodes[node] = new List<GoodSection>();
            edgeWeights[node] = new Dictionary<GoodSection, float>();

            // Store state transition
            if (node.targetState != null)
            {
                stateTransitions[node] = node.targetState;
            }
        }

        // Add connections from node's connected sections
        if (node.connectedSections != null)
        {
            foreach (var connected in node.connectedSections)
            {
                if (connected != null)
                {
                    AddEdge(node, connected);
                }
            }
        }
    }

    /// <summary>
    /// Add an edge (connection) between two nodes.
    /// </summary>
    public void AddEdge(GoodSection from, GoodSection to, float weight = 1f)
    {
        if (from == null || to == null)
            return;

        // Ensure both nodes exist
        if (!nodes.ContainsKey(from))
        {
            AddNode(from);
        }

        if (!nodes.ContainsKey(to))
        {
            AddNode(to);
        }

        // Add edge
        if (!nodes[from].Contains(to))
        {
            nodes[from].Add(to);
        }

        edgeWeights[from][to] = weight;
    }

    /// <summary>
    /// Find path from current state to goal section using graph search.
    /// </summary>
    public List<GoodSection> FindPath(RagdollState currentState, GoodSection goal)
    {
        if (goal == null)
            return new List<GoodSection>();

        // Use A* search to find path
        return AStarSearch(currentState, goal);
    }

    /// <summary>
    /// A* search algorithm for finding optimal path through graph.
    /// </summary>
    private List<GoodSection> AStarSearch(RagdollState currentState, GoodSection goal)
    {
        // Priority queue: (node, cost, path)
        var openSet = new List<(GoodSection node, float cost, List<GoodSection> path)>();
        var closedSet = new HashSet<GoodSection>();

        // Find starting node (closest feasible section to current state)
        GoodSection start = FindClosestFeasibleNode(currentState);
        if (start == null)
        {
            // No feasible start node - return direct path to goal if feasible
            if (goal.IsFeasible(currentState))
            {
                return new List<GoodSection> { goal };
            }
            return new List<GoodSection>();
        }

        // If start is goal, return it
        if (start == goal)
        {
            return new List<GoodSection> { goal };
        }

        // Initialize open set with start node
        openSet.Add((start, 0f, new List<GoodSection> { start }));

        // Search loop
        while (openSet.Count > 0)
        {
            // Get node with lowest cost
            var current = openSet.OrderBy(x => x.cost).First();
            openSet.Remove(current);

            if (current.node == goal)
            {
                // Found path
                return current.path;
            }

            closedSet.Add(current.node);

            // Expand neighbors
            if (nodes.TryGetValue(current.node, out List<GoodSection> neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    // Calculate cost
                    float edgeWeight = edgeWeights[current.node].TryGetValue(neighbor, out float weight) ? weight : 1f;
                    float heuristic = EstimateCost(neighbor, goal);
                    float totalCost = current.cost + edgeWeight + heuristic;

                    // Create new path
                    List<GoodSection> newPath = new List<GoodSection>(current.path) { neighbor };

                    // Check if neighbor is in open set
                    var existing = openSet.FirstOrDefault(x => x.node == neighbor);
                    if (existing.node != null && existing.cost <= totalCost)
                    {
                        continue; // Skip if better path already exists
                    }

                    // Remove existing entry if present
                    if (existing.node != null)
                    {
                        openSet.Remove(existing);
                    }

                    // Add to open set
                    openSet.Add((neighbor, totalCost, newPath));
                }
            }
        }

        // No path found - return empty list
        return new List<GoodSection>();
    }

    /// <summary>
    /// Find closest feasible node to current state.
    /// </summary>
    private GoodSection FindClosestFeasibleNode(RagdollState currentState)
    {
        GoodSection closest = null;
        float minDistance = float.MaxValue;

        foreach (var node in nodes.Keys)
        {
            if (node.IsFeasible(currentState))
            {
                // Calculate distance (using feasibility score as inverse distance)
                float score = node.CalculateFeasibilityScore(currentState);
                float distance = 1f - score; // Invert score to get distance

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = node;
                }
            }
        }

        return closest;
    }

    /// <summary>
    /// Estimate cost from one node to another (heuristic for A*).
    /// </summary>
    private float EstimateCost(GoodSection from, GoodSection to)
    {
        if (from == null || to == null)
            return float.MaxValue;

        if (from == to)
            return 0f;

        // Use state distance as heuristic if available
        if (from.targetState != null && to.requiredState != null)
        {
            return from.targetState.CalculateDistance(to.requiredState);
        }

        // Default: uniform cost
        return 1f;
    }

    /// <summary>
    /// Get all nodes connected to a given node.
    /// </summary>
    public List<GoodSection> GetConnectedNodes(GoodSection node)
    {
        if (node == null || !nodes.TryGetValue(node, out List<GoodSection> connected))
        {
            return new List<GoodSection>();
        }

        return new List<GoodSection>(connected);
    }

    /// <summary>
    /// Get all nodes in the graph.
    /// </summary>
    public List<GoodSection> GetAllNodes()
    {
        return new List<GoodSection>(nodes.Keys);
    }

    /// <summary>
    /// Check if graph contains a node.
    /// </summary>
    public bool ContainsNode(GoodSection node)
    {
        return node != null && nodes.ContainsKey(node);
    }

    /// <summary>
    /// Remove a node from the graph.
    /// </summary>
    public void RemoveNode(GoodSection node)
    {
        if (node == null || !nodes.ContainsKey(node))
            return;

        // Remove edges to this node
        foreach (var kvp in nodes)
        {
            kvp.Value.Remove(node);
            if (edgeWeights.TryGetValue(kvp.Key, out var weights))
            {
                weights.Remove(node);
            }
        }

        // Remove node
        nodes.Remove(node);
        edgeWeights.Remove(node);
        stateTransitions.Remove(node);
    }

    /// <summary>
    /// Get state transition for a node (target state after executing).
    /// </summary>
    public RagdollState GetStateTransition(GoodSection node)
    {
        stateTransitions.TryGetValue(node, out RagdollState state);
        return state;
    }
}
