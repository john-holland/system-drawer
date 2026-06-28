using System;
using System.Collections.Generic;
using UnityEngine;

namespace Continuum.Society
{
    [Serializable]
    public class SpatialBoundsDto
    {
        public float centerX;
        public float centerZ;
        public float widthM;
        public float depthM;
    }

    [Serializable]
    public class CityScapeProfileDto
    {
        public SpatialBoundsDto spatialBounds;
        public int sliceCount = 32;
        public int gridResX = 16;
        public int gridResY = 16;
        public int gridResZ = 16;
        public int gridResT = 32;
        public List<PlannedBuildingDto> plannedBuildings = new();
    }

    [Serializable]
    public class PlannedBuildingDto
    {
        public string zoneId;
        public string buildingTypeId;
        public int count;
    }

    /// <summary>API city-scape profile applied to SpatialGenerator4D on bake.</summary>
    [CreateAssetMenu(fileName = "CityScapeProfile", menuName = "Continuum/Society/City Scape Profile")]
    public class CityScapeProfile : ScriptableObject
    {
        public string cityId;
        public int profileVersion;
        public SpatialBoundsDto spatialBounds = new() { widthM = 1000, depthM = 1000 };
        public int sliceCount = 32;
        public int gridResX = 16;
        public int gridResY = 16;
        public int gridResZ = 16;
        public int gridResT = 32;

        public void ApplyTo(SpatialGenerator4D generator)
        {
            if (generator == null || spatialBounds == null) return;
            var size = new Vector3(spatialBounds.widthM, spatialBounds.depthM * 0.1f, spatialBounds.depthM);
            generator.spatialBounds = new Bounds(
                new Vector3(spatialBounds.centerX, 0, spatialBounds.centerZ),
                size);
            generator.sliceCount = sliceCount;
            generator.gridResX = gridResX;
            generator.gridResY = gridResY;
            generator.gridResZ = gridResZ;
            generator.gridResT = gridResT;
        }

        public void ImportFromDto(CityScapeProfileDto dto, string city, int version)
        {
            cityId = city;
            profileVersion = version;
            if (dto == null) return;
            spatialBounds = dto.spatialBounds ?? spatialBounds;
            sliceCount = dto.sliceCount;
            gridResX = dto.gridResX;
            gridResY = dto.gridResY;
            gridResZ = dto.gridResZ;
            gridResT = dto.gridResT;
        }
    }
}
