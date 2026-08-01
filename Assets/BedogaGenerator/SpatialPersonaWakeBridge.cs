using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Forwards SpatialGenerator placements into PersonaDayManager venue retinues.
/// Lives in BedogaGenerator (refs Locomotion.Runtime).
/// </summary>
[AddComponentMenu("Bedoga/Spatial Persona Wake Bridge")]
public sealed class SpatialPersonaWakeBridge : MonoBehaviour
{
    public SpatialGenerator spatialGenerator;
    public PersonaDayManager dayManager;
    public string venueStableId;
    public CivilSystemKind venueKind = CivilSystemKind.Generic;
    public bool autoRegisterVenue = true;

    void OnEnable()
    {
        if (spatialGenerator == null) spatialGenerator = GetComponent<SpatialGenerator>();
        SpatialGenerator.PlacedInstancesChanged += OnPlaced;
    }

    void OnDisable()
    {
        SpatialGenerator.PlacedInstancesChanged -= OnPlaced;
    }

    void OnPlaced(SpatialGenerator gen)
    {
        if (gen == null || (spatialGenerator != null && gen != spatialGenerator)) return;
        var pdm = dayManager != null ? dayManager : PersonaDayManager.Instance;
        if (pdm == null) return;

        string id = !string.IsNullOrEmpty(venueStableId) ? venueStableId : gen.gameObject.name;
        var venue = pdm.lattice.Get(id);
        if (venue == null && autoRegisterVenue)
        {
            venue = new CivilVenueNode
            {
                stableId = id,
                kind = venueKind,
                contextOwner = gen.gameObject,
                buildingTypeId = venueKind.ToString().ToLowerInvariant()
            };
            if (venueKind != CivilSystemKind.Kitchen)
                venue.venueBio = gen.GetComponent<CivilVenueBioRhythmService>()
                                 ?? gen.gameObject.AddComponent<CivilVenueBioRhythmService>();
            pdm.RegisterVenue(venue);
        }
        if (venue == null) return;

        var placed = gen.CollectPlacedGameObjects();
        pdm.wakeSource?.IngestPlaced(venue, placed);
    }
}
