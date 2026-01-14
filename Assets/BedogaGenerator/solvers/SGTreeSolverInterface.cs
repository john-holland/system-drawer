using System.Collections.Generic;
using UnityEngine;

// Interface for tree solver operations - abstracted for Quad/Oct tree solvers
// This interface abstractly declares methods required to update or rebuild the SceneGraph
// from the BehaviorTreeNode collection using a new or existing Quad or Oct tree.
public interface SGTreeSolverInterface
{
    // Insert an object into the spatial tree
    bool Insert(Bounds bounds, object behaviorTreeProperties, GameObject gameObject);
    
    // Search for objects within given bounds
    List<GameObject> Search(Bounds searchBounds);
    
    // Check intersection with bounds
    bool Intersects(Bounds bounds);
    
    // Clear the tree
    void Clear();
    
    // Get all objects in the tree
    List<GameObject> GetAllObjects();
    
    // Update tree with new bounds
    void UpdateTree(Bounds newBounds);
    
    // Compare with another tree to determine if update or rebuild is needed
    bool CompareTree(SGTreeSolverInterface otherTree);
}
