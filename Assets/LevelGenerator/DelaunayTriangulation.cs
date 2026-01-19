using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Delaunay triangulation implementation for 2D points.
/// Used to determine efficient hallway connections between rooms.
/// </summary>
public class DelaunayTriangulation
{
    /// <summary>
    /// Triangle structure for Delaunay triangulation.
    /// </summary>
    public struct Triangle
    {
        public int v0, v1, v2;

        public Triangle(int v0, int v1, int v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }

        public bool Contains(int vertex)
        {
            return v0 == vertex || v1 == vertex || v2 == vertex;
        }
    }

    /// <summary>
    /// Edge structure for triangulation.
    /// </summary>
    public struct Edge
    {
        public int v0, v1;

        public Edge(int v0, int v1)
        {
            // Ensure consistent ordering (smaller index first)
            if (v0 < v1)
            {
                this.v0 = v0;
                this.v1 = v1;
            }
            else
            {
                this.v0 = v1;
                this.v1 = v0;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return v0 == other.v0 && v1 == other.v1;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return v0 * 1000 + v1;
        }
    }

    /// <summary>
    /// Perform Delaunay triangulation on a set of 2D points.
    /// Returns list of triangles.
    /// </summary>
    public List<Triangle> Triangulate(List<Vector2> points)
    {
        if (points == null || points.Count < 3)
        {
            return new List<Triangle>();
        }

        // Create super triangle that contains all points
        Rect bounds = GetBounds(points);
        float margin = Mathf.Max(bounds.width, bounds.height) * 2f;
        Vector2 p1 = new Vector2(bounds.center.x - margin, bounds.center.y - margin);
        Vector2 p2 = new Vector2(bounds.center.x + margin, bounds.center.y - margin);
        Vector2 p3 = new Vector2(bounds.center.x, bounds.center.y + margin);

        List<Triangle> triangles = new List<Triangle>();
        List<Vector2> pointsWithSuper = new List<Vector2>(points);
        
        // Add super triangle vertices
        int super0 = pointsWithSuper.Count;
        int super1 = pointsWithSuper.Count + 1;
        int super2 = pointsWithSuper.Count + 2;
        pointsWithSuper.Add(p1);
        pointsWithSuper.Add(p2);
        pointsWithSuper.Add(p3);

        triangles.Add(new Triangle(super0, super1, super2));

        // Add points one by one
        for (int i = 0; i < points.Count; i++)
        {
            List<Triangle> badTriangles = new List<Triangle>();
            
            // Find all triangles whose circumcircle contains the point
            foreach (var triangle in triangles)
            {
                if (IsPointInCircumcircle(pointsWithSuper[triangle.v0], pointsWithSuper[triangle.v1], pointsWithSuper[triangle.v2], pointsWithSuper[i]))
                {
                    badTriangles.Add(triangle);
                }
            }

            // Find boundary of polygonal hole
            List<Edge> polygon = new List<Edge>();
            foreach (var triangle in badTriangles)
            {
                Edge e1 = new Edge(triangle.v0, triangle.v1);
                Edge e2 = new Edge(triangle.v1, triangle.v2);
                Edge e3 = new Edge(triangle.v2, triangle.v0);

                AddEdgeIfUnique(polygon, e1, badTriangles);
                AddEdgeIfUnique(polygon, e2, badTriangles);
                AddEdgeIfUnique(polygon, e3, badTriangles);
            }

            // Remove bad triangles
            foreach (var triangle in badTriangles)
            {
                triangles.Remove(triangle);
            }

            // Re-triangulate the polygonal hole
            foreach (var edge in polygon)
            {
                triangles.Add(new Triangle(edge.v0, edge.v1, i));
            }
        }

        // Remove triangles containing super triangle vertices
        triangles.RemoveAll(t => t.Contains(super0) || t.Contains(super1) || t.Contains(super2));

        return triangles;
    }

    /// <summary>
    /// Get all unique edges from triangles.
    /// </summary>
    public HashSet<Edge> GetEdges(List<Triangle> triangles)
    {
        HashSet<Edge> edges = new HashSet<Edge>();

        foreach (var triangle in triangles)
        {
            edges.Add(new Edge(triangle.v0, triangle.v1));
            edges.Add(new Edge(triangle.v1, triangle.v2));
            edges.Add(new Edge(triangle.v2, triangle.v0));
        }

        return edges;
    }

    /// <summary>
    /// Get triangles (for debugging/visualization).
    /// </summary>
    public List<Triangle> GetTriangles()
    {
        return new List<Triangle>();
    }

    // Helper methods

    private Rect GetBounds(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
            return new Rect();

        float minX = points[0].x;
        float maxX = points[0].x;
        float minY = points[0].y;
        float maxY = points[0].y;

        foreach (var point in points)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private bool IsPointInCircumcircle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        // Calculate circumcircle center and radius
        float ax = a.x, ay = a.y;
        float bx = b.x, by = b.y;
        float cx = c.x, cy = c.y;
        float px = p.x, py = p.y;

        float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Mathf.Abs(d) < 0.0001f)
            return false; // Points are collinear

        float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

        Vector2 center = new Vector2(ux, uy);
        float radius = Vector2.Distance(center, a);
        float distance = Vector2.Distance(center, p);

        return distance < radius;
    }

    private void AddEdgeIfUnique(List<Edge> edges, Edge edge, List<Triangle> badTriangles)
    {
        // Count how many times this edge appears in bad triangles
        int count = 0;
        foreach (var triangle in badTriangles)
        {
            if ((triangle.v0 == edge.v0 && triangle.v1 == edge.v1) ||
                (triangle.v1 == edge.v0 && triangle.v2 == edge.v1) ||
                (triangle.v2 == edge.v0 && triangle.v0 == edge.v1) ||
                (triangle.v0 == edge.v1 && triangle.v1 == edge.v0) ||
                (triangle.v1 == edge.v1 && triangle.v2 == edge.v0) ||
                (triangle.v2 == edge.v1 && triangle.v0 == edge.v0))
            {
                count++;
            }
        }

        // If edge appears only once, it's on the boundary
        if (count == 1)
        {
            edges.Add(edge);
        }
    }
}
