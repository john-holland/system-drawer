using Planetary;
using Planetary.Composition;
using Planetary.TimeTravel;
using UnityEngine;
using Weather;

namespace Roads
{
    /// <summary>Merges road height stamps into weather/terrain prebake pipelines.</summary>
    [AddComponentMenu("Roads/Road Weather Integration")]
    public class RoadWeatherIntegration : MonoBehaviour
    {
        public RoadMeshBaker baker;
        public MeshTerrainSampler meshTerrainSampler;
        public PlanetBody planetBody;
        public PlanetaryWeatherTimeTravelSystem timeTravelSystem;

        public void ApplyHeightStampToScene()
        {
            if (baker?.lastBakeData?.heightStamp == null)
                return;
            // Height stamp available via lastBakeData.heightStamp.ToTexture() for terrain shaders
            _ = baker.lastBakeData.heightStamp.ToTexture();
        }

        public void RebakePlanetaryComposition()
        {
            Debug.Log("RebakePlanetaryComposition called for " + this.gameObject.name);
            if (planetBody == null)
                planetBody = FindAnyObjectByType<PlanetBody>();
            if (planetBody != null)
                planetBody.RebakeComposition();
        }

        public void OnRoadBakeComplete()
        {
            ApplyHeightStampToScene();
            RebakePlanetaryComposition();
        }

        public void RestoreWearFromTimeTravelFrame(WeatherTimeTravelFrame frame)
        {
            if (frame?.roadWearSnapshot == null)
                return;
            var wear = frame.roadWearSnapshot;
            var erosion = GetComponent<RoadErosionSystem>();
            if (erosion == null || wear.flowArcLengths == null)
                return;

            var cells = new RoadFlowCell[wear.flowArcLengths.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new RoadFlowCell
                {
                    arcLength = wear.flowArcLengths[i],
                    lateralPos = wear.flowLateral != null && i < wear.flowLateral.Length ? wear.flowLateral[i] : 0f,
                    intensity = wear.flowIntensities != null && i < wear.flowIntensities.Length ? wear.flowIntensities[i] : 0f
                };
            }
            erosion.cachedFlowCells = cells;
            erosion.debrisCached = wear.debrisCached;
        }

        public RoadWearSnapshotDto CaptureWearSnapshot()
        {
            var erosion = GetComponent<RoadErosionSystem>();
            if (erosion?.cachedFlowCells == null)
                return null;
            var snap = new RoadWearSnapshotDto
            {
                debrisCached = erosion.debrisCached,
                roadSegmentId = baker != null ? baker.lastBakeData?.roadSegmentId : null
            };
            int n = erosion.cachedFlowCells.Length;
            snap.flowArcLengths = new float[n];
            snap.flowIntensities = new float[n];
            snap.flowLateral = new float[n];
            for (int i = 0; i < n; i++)
            {
                snap.flowArcLengths[i] = erosion.cachedFlowCells[i].arcLength;
                snap.flowIntensities[i] = erosion.cachedFlowCells[i].intensity;
                snap.flowLateral[i] = erosion.cachedFlowCells[i].lateralPos;
            }
            return snap;
        }
    }
}
