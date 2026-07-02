using System;
using UnityEngine;

namespace Planetary.Celestial
{
    [Serializable]
    public struct GalacticBodyRecord
    {
        public string bodyId;
        public GalacticBodyKind kind;
        public string displayName;
        public Vector3 galacticPosition;
        public double massKg;
        public float radiusM;
        public float radiationLevel;
        public float gravityWellStrength;
        public string societyPlanetId;
        public string uscAssetId;
        public string scenePrefabRef;
        public string lemmaColorId;
        public string lemmaVisibilityId;
        public bool immovable;

        public static GalacticBodyKind ParseKind(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return GalacticBodyKind.Planetoid;
            switch (kind.ToLowerInvariant())
            {
                case "star": return GalacticBodyKind.Star;
                case "planet": return GalacticBodyKind.Planet;
                case "moon": return GalacticBodyKind.Moon;
                default: return GalacticBodyKind.Planetoid;
            }
        }

        public static string KindToApi(GalacticBodyKind kind)
        {
            switch (kind)
            {
                case GalacticBodyKind.Star: return "star";
                case GalacticBodyKind.Planet: return "planet";
                case GalacticBodyKind.Moon: return "moon";
                default: return "planetoid";
            }
        }
    }
}
