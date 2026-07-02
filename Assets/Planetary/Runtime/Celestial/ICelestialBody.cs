using Planetary;
using UnityEngine;

namespace Planetary.Celestial
{
    public interface ICelestialBody
    {
        string BodyId { get; }
        GalacticBodyKind Kind { get; }
        float Mass { get; }
        float Radius { get; }
        Vector3 GalacticPosition { get; }
        PhysicalManifold Manifold { get; }
        bool Immovable { get; }
        Transform BodyTransform { get; }
    }
}
