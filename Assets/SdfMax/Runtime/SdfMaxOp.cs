namespace SdfMax
{
    public enum SdfMaxOp
    {
        PrimitiveLeaf = 0,
        Constant = 1,
        Max = 2,
        Min = 3,
        Subtract = 4,
        Add = 5,
        Multiply = 6,
        Divide = 7,
        SmoothMax = 8
    }

    public enum SdfPrimitiveType
    {
        Sphere = 0,
        Box = 1,
        Capsule = 2,
        Plane = 3,
        MeshBounds = 4,
        FractalNoise = 5,
        MandelbrotDisplacement = 6,
        DisplacedSphere = 7,
        PlanarStamp = 8,
        LatLonShell = 9,
        Torus = 10,
        SplineExtrusion = 11,
        DisplacedTorus = 12
    }
}
