using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Solves climbing paths by traversing graph between grab holds and preposition placements.
/// Uses modified PhysicsCardSolver to generate cards needed for climbing.
/// </summary>
public class ClimbingPathSolver : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [Tooltip("Maximum path length (number of cards)")]
    public int maxPathLength = 20;

    [Tooltip("Maximum search distance for grab holds")]
    public float maxSearchDistance = 10f;

    [Tooltip("Preferred path cost weight for distance")]
    [Range(0f, 1f)]
    public float distanceWeight = 0.4f;

    [Tooltip("Preferred path cost weight for difficulty")]
    [Range(0f, 1f)]
    public float difficultyWeight = 0.6f;

    [Header("Card Generation")]
    [Tooltip("Reference to climbing card generator")]
    public ClimbingCardGenerator cardGenerator;

    private PhysicsCardSolver cardSolver;
    private SurfaceGrabDetector grabDetector;

    /// <summary>
    /// Represents a node in the climbing graph.
    /// </summary>
    [System.Serializable]
    public class ClimbingNode
    {
        public Vector3 position;
        public SurfaceGrabDetector.GrabHold grabHold;
        public GoodSection card;
        public List<ClimbingNode> neighbors = new List<ClimbingNode>();
        public float costFromStart = float.MaxValue;
        public float estimatedCostToGoal = float.MaxValue;
        public ClimbingNode previous;

        public float TotalCost => costFromStart + estimatedCostToGoal;
    }

    private void Awake()
    {
        cardSolver = GetComponent<PhysicsCardSolver>();
        grabDetector = GetComponent<SurfaceGrabDetector>();
        if (grabDetector == null)
        {
            grabDetector = FindAnyObjectByType<SurfaceGrabDetector>();
        }

        if (cardGenerator == null)
        {
            cardGenerator = GetComponent<ClimbingCardGenerator>();
            if (cardGenerator == null)
            {
                cardGenerator = FindAnyObjectByType<ClimbingCardGenerator>();
            }
        }
    }

    /// <summary>
    /// Find a climbing path from start position to goal position.
    /// </summary>
    public List<GoodSection> FindClimbingPath(
        Vector3 startPosition,
        Vector3 goalPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        if (grabDetector == null)
        {
            Debug.LogError("[ClimbingPathSolver] No SurfaceGrabDetector found");
            return new List<GoodSection>();
        }

        if (cardGenerator == null)
        {
            Debug.LogError("[ClimbingPathSolver] No ClimbingCardGenerator found");
            return new List<GoodSection>();
        }

        // Find grab holds near start and goal
        SurfaceGrabDetector.GrabHold startHold = grabDetector.FindBestGrabHold(startPosition, maxSearchDistance);
        SurfaceGrabDetector.GrabHold goalHold = grabDetector.FindBestGrabHold(goalPosition, maxSearchDistance);

        if (startHold == null)
        {
            Debug.LogWarning("[ClimbingPathSolver] No grab hold found near start position");
            return new List<GoodSection>();
        }

        // Build graph of grab holds
        List<ClimbingNode> graph = BuildClimbingGraph(startPosition, goalPosition, currentState, ragdollActor);

        if (graph.Count == 0)
        {
            Debug.LogWarning("[ClimbingPathSolver] Could not build climbing graph");
            return new List<GoodSection>();
        }

        // Find start and goal nodes
        ClimbingNode startNode = FindNearestNode(graph, startPosition);
        ClimbingNode goalNode = FindNearestNode(graph, goalPosition);

        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("[ClimbingPathSolver] Could not find start or goal node in graph");
            return new List<GoodSection>();
        }

        // Use A* pathfinding to find path
        List<ClimbingNode> pathNodes = FindPathAStar(startNode, goalNode, graph);

        if (pathNodes == null || pathNodes.Count == 0)
        {
            Debug.LogWarning("[ClimbingPathSolver] No path found");
            return new List<GoodSection>();
        }

        // Generate cards for the path
        List<GoodSection> pathCards = new List<GoodSection>();

        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            ClimbingNode currentNode = pathNodes[i];
            ClimbingNode nextNode = pathNodes[i + 1];

            if (currentNode.grabHold != null && nextNode.grabHold != null)
            {
                // Generate cards between these two holds
                List<GoodSection> segmentCards = cardGenerator.GenerateClimbingCards(
                    currentNode.grabHold,
                    nextNode.position,
                    currentState,
                    ragdollActor
                );

                pathCards.AddRange(segmentCards);
            }
            else if (currentNode.card != null)
            {
                // Use existing card
                pathCards.Add(currentNode.card);
            }
        }

        // Add final card to goal
        if (goalNode.grabHold != null)
        {
            List<GoodSection> finalCards = cardGenerator.GenerateClimbingCards(
                goalNode.grabHold,
                goalPosition,
                currentState,
                ragdollActor
            );
            pathCards.AddRange(finalCards);
        }

        return pathCards;
    }

    /// <summary>
    /// Build a graph of climbing nodes from grab holds.
    /// </summary>
    private List<ClimbingNode> BuildClimbingGraph(
        Vector3 startPosition,
        Vector3 goalPosition,
        RagdollState currentState,
        GameObject ragdollActor)
    {
        List<ClimbingNode> graph = new List<ClimbingNode>();

        // Find all grab holds in the area
        Bounds searchBounds = new Bounds(
            (startPosition + goalPosition) * 0.5f,
            Vector3.one * maxSearchDistance * 2f
        );

        List<SurfaceGrabDetector.GrabHold> grabHolds = grabDetector.GetGrabHoldsInBounds(searchBounds);

        // Create nodes for each grab hold
        foreach (var hold in grabHolds)
        {
            ClimbingNode node = new ClimbingNode
            {
                position = hold.position,
                grabHold = hold
            };

            // Generate card for this node
            if (cardGenerator != null)
            {
                List<GoodSection> cards = cardGenerator.GenerateClimbingCards(
                    hold,
                    hold.position,
                    currentState,
                    ragdollActor
                );
                if (cards.Count > 0)
                {
                    node.card = cards[0]; // Use first card
                }
            }

            graph.Add(node);
        }

        // Connect nodes that are reachable from each other
        for (int i = 0; i < graph.Count; i++)
        {
            for (int j = i + 1; j < graph.Count; j++)
            {
                if (AreNodesReachable(graph[i], graph[j]))
                {
                    graph[i].neighbors.Add(graph[j]);
                    graph[j].neighbors.Add(graph[i]);
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// Check if two nodes are reachable from each other.
    /// </summary>
    private bool AreNodesReachable(ClimbingNode node1, ClimbingNode node2)
    {
        float distance = Vector3.Distance(node1.position, node2.position);
        
        // Nodes are reachable if within reasonable climbing distance
        float maxReachDistance = 1.5f; // Maximum reach for climbing
        return distance <= maxReachDistance;
    }

    /// <summary>
    /// Find nearest node to a position.
    /// </summary>
    private ClimbingNode FindNearestNode(List<ClimbingNode> graph, Vector3 position)
    {
        ClimbingNode nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var node in graph)
        {
            float distance = Vector3.Distance(node.position, position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = node;
            }
        }

        return nearest;
    }

    /// <summary>
    /// A* pathfinding algorithm.
    /// </summary>
    private List<ClimbingNode> FindPathAStar(ClimbingNode start, ClimbingNode goal, List<ClimbingNode> graph)
    {
        // Reset all nodes
        foreach (var node in graph)
        {
            node.costFromStart = float.MaxValue;
            node.estimatedCostToGoal = float.MaxValue;
            node.previous = null;
        }

        start.costFromStart = 0f;
        start.estimatedCostToGoal = EstimateCost(start, goal);

        List<ClimbingNode> openSet = new List<ClimbingNode> { start };
        HashSet<ClimbingNode> closedSet = new HashSet<ClimbingNode>();

        while (openSet.Count > 0)
        {
            // Find node with lowest total cost
            ClimbingNode current = openSet.OrderBy(n => n.TotalCost).First();
            
            if (current == goal)
            {
                // Reconstruct path
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // Check neighbors
            foreach (var neighbor in current.neighbors)
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeCost = current.costFromStart + CalculateEdgeCost(current, neighbor);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeCost >= neighbor.costFromStart)
                {
                    continue;
                }

                neighbor.previous = current;
                neighbor.costFromStart = tentativeCost;
                neighbor.estimatedCostToGoal = EstimateCost(neighbor, goal);
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Calculate cost of edge between two nodes.
    /// </summary>
    private float CalculateEdgeCost(ClimbingNode from, ClimbingNode to)
    {
        float distance = Vector3.Distance(from.position, to.position);
        
        // Calculate difficulty based on angle, etc.
        float difficulty = 1f;
        if (from.grabHold != null && to.grabHold != null)
        {
            // Difficulty increases with angle difference
            float angleDiff = Mathf.Abs(from.grabHold.edgeAngle - to.grabHold.edgeAngle);
            difficulty = 1f + angleDiff / 90f;
        }

        return distance * distanceWeight + difficulty * difficultyWeight;
    }

    /// <summary>
    /// Estimate cost from node to goal (heuristic).
    /// </summary>
    private float EstimateCost(ClimbingNode node, ClimbingNode goal)
    {
        return Vector3.Distance(node.position, goal.position);
    }

    /// <summary>
    /// Reconstruct path from goal node.
    /// </summary>
    private List<ClimbingNode> ReconstructPath(ClimbingNode goal)
    {
        List<ClimbingNode> path = new List<ClimbingNode>();
        ClimbingNode current = goal;

        while (current != null)
        {
            path.Insert(0, current);
            current = current.previous;
        }

        return path;
    }

    /// <summary>
    /// Integrate climbing cards into PhysicsCardSolver.
    /// </summary>
    public void IntegrateCardsIntoSolver(List<GoodSection> climbingCards)
    {
        if (cardSolver == null)
            return;

        // Add cards to solver's available cards
        foreach (var card in climbingCards)
        {
            if (card != null && !cardSolver.availableCards.Contains(card))
            {
                cardSolver.availableCards.Add(card);
            }
        }
    }
}
