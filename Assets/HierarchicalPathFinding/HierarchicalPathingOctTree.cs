/*
just to be clear, if the object has a rigid body collider, we use that, 
otherwise we examine the mesh and oct-trocept (?lol bisect in an octtree)
 convex pieces of their mesh until all concave is convex, capturing any 
 overlapting or fully enclosed spaces (with an option normal vertex padding to 
 prevent porus objects from being overly pokable etc) and making those 
 appropriately pruned for the collision tree - this is a recursive process that should be cached and memoized to avoid re-calculating the same octtree for the same object multiple times
 we should have a way to rarify the cache and rebuild it on demand / loosely repaint / remark for rebuild with any changes so calculations are fresh
    maybe a redblue tree algorithm for the cache?
 we should have a way to visualize the octtree in the editor and in the scene view
*/
