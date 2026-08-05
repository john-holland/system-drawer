using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CityPlaceableChunkTests
{
    static CityPixelGrid NewGrid()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 16;
        grid.height = 16;
        grid.frameCount = 1;
        grid.cellWorldSize = 1f;
        grid.EnsureLayersAndFrames();
        return grid;
    }

    static CityPlaceableCatalog NewCatalog(params CityPlaceableCandidate[] candidates)
    {
        var cat = ScriptableObject.CreateInstance<CityPlaceableCatalog>();
        cat.candidates = new List<CityPlaceableCandidate>(candidates);
        return cat;
    }

    [Test]
    public void Separator_SplitsSameTypeIntoTwoChunks()
    {
        var grid = NewGrid();
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 0, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 0, kind = CityPixelBrushKind.BuildingTypeSeparator
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 2, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });

        var result = CityPlaceableChunker.ChunkFrame(grid, 0, null);
        Assert.AreEqual(2, result.chunks.Count);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void IncompleteSeparator_WarnsAndKeepsOneChunk()
    {
        var grid = NewGrid();
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 0, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 2, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });
        // Notch separator — touches group but does not wall-split the run.
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 1, kind = CityPixelBrushKind.BuildingTypeSeparator
        });

        var result = CityPlaceableChunker.ChunkFrame(grid, 0, null);
        Assert.AreEqual(1, result.chunks.Count);
        Assert.IsTrue(result.incompleteSeparation);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void SharedShell_MergesHotelAndRetailUnderSkyscraper()
    {
        var catalog = NewCatalog(new CityPlaceableCandidate
        {
            id = "sky",
            placeableKind = CityPlaceableKind.Building,
            typeKey = "shared_building",
            sharedBuildingCompatible = true,
            sg3dSharedBuildingCompatible = true,
            footprintCellsX = 2,
            footprintCellsZ = 1,
            footprintCellsY = 20
        });
        var grid = NewGrid();
        grid.catalog = catalog;
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 0, cellY = 0, kind = CityPixelBrushKind.Building,
            typeKey = "Hotel", heightCells = 10
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 0, kind = CityPixelBrushKind.Building,
            typeKey = "Retail", heightCells = 4
        });

        var result = CityPlaceableChunker.ChunkFrame(grid, 0, catalog);
        Assert.AreEqual(1, result.chunks.Count);
        Assert.IsTrue(result.chunks[0].isSharedShell);
        CollectionAssert.Contains(result.chunks[0].tenantTypeKeys, "Hotel");
        CollectionAssert.Contains(result.chunks[0].tenantTypeKeys, "Retail");
        Assert.AreEqual(10, result.chunks[0].heightCells);

        Object.DestroyImmediate(grid);
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void SharedShell_IncompatibleSg3d_DoesNotMerge()
    {
        var dummyPrompt = ScriptableObject.CreateInstance<FloorPlanIndexMap>();
        var catalog = NewCatalog(new CityPlaceableCandidate
        {
            id = "sky_bad",
            placeableKind = CityPlaceableKind.Building,
            typeKey = "shared_building",
            sharedBuildingCompatible = true,
            sg3dSharedBuildingCompatible = false,
            sg3dPromptComposition = dummyPrompt,
            footprintCellsX = 2,
            footprintCellsZ = 1,
            footprintCellsY = 20
        });
        var grid = NewGrid();
        grid.catalog = catalog;
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 0, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Hotel"
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 0, kind = CityPixelBrushKind.Building, typeKey = "Retail"
        });

        var result = CityPlaceableChunker.ChunkFrame(grid, 0, catalog);
        Assert.AreEqual(2, result.chunks.Count);
        Assert.IsFalse(result.chunks[0].isSharedShell && result.chunks[1].isSharedShell);

        Object.DestroyImmediate(grid);
        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(dummyPrompt);
    }

    [Test]
    public void FloorPlanIndexMap_AutoFromFloorIndex_AndAttendantDialog()
    {
        var stamps = new List<CityPixelBrushStamp>
        {
            new CityPixelBrushStamp
            {
                cellX = 2, cellY = 3, kind = CityPixelBrushKind.Building,
                typeKey = "Hotel", floorIndex = 0, zoneId = "lobby"
            },
            new CityPixelBrushStamp
            {
                cellX = 3, cellY = 3, kind = CityPixelBrushKind.Building,
                typeKey = "Retail", floorIndex = 1, zoneId = "shop"
            }
        };
        var map = FloorPlanIndexMap.BuildFromStamps(stamps, 2, 3, "b1");
        Assert.AreEqual(2, map.floorCount);
        Assert.IsTrue(map.TryGetAttendantDialog(0, "lobby", out string dialog0));
        Assert.AreEqual("attendant_floor_0", dialog0);
        Assert.IsTrue(map.TryGetAttendantDialog(1, "shop", out string dialog1));
        Assert.AreEqual("attendant_floor_1", dialog1);
        Assert.GreaterOrEqual(map.GetDirectory().Count, 2);
        Object.DestroyImmediate(map);
    }

    [Test]
    public void BestFit_HeightPrefersTallerShell()
    {
        var catalog = NewCatalog(
            new CityPlaceableCandidate
            {
                id = "short",
                placeableKind = CityPlaceableKind.Building,
                typeKey = "Hotel",
                footprintCellsX = 1,
                footprintCellsZ = 1,
                footprintCellsY = 1
            },
            new CityPlaceableCandidate
            {
                id = "tall",
                placeableKind = CityPlaceableKind.Building,
                typeKey = "Hotel",
                footprintCellsX = 1,
                footprintCellsZ = 1,
                footprintCellsY = 8
            });

        var chunk = new CityPlaceableChunk
        {
            placeableKind = CityPlaceableKind.Building,
            typeKey = "Hotel",
            minX = 0,
            minY = 0,
            maxX = 0,
            maxY = 0,
            heightCells = 5,
            cells = new List<CityPlaceableChunkCell>
            {
                new CityPlaceableChunkCell { cellX = 0, cellY = 0, typeKey = "Hotel" }
            }
        };

        var pick = CityPlaceableBestFit.Pick(chunk, catalog);
        Assert.IsNotNull(pick);
        Assert.AreEqual("tall", pick.id);

        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void Materialize_SpawnsOneShellPerChunk_WithFloorPlanHost()
    {
        var catalog = NewCatalog(new CityPlaceableCandidate
        {
            id = "shop",
            placeableKind = CityPlaceableKind.Building,
            typeKey = "Retail",
            footprintCellsX = 2,
            footprintCellsZ = 1,
            footprintCellsY = 2
        });
        var grid = NewGrid();
        grid.catalog = catalog;
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 0, cellY = 0, kind = CityPixelBrushKind.Building,
            typeKey = "Retail", heightCells = 2, buildingKind = CivilSystemKind.Mall
        });
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0, cellX = 1, cellY = 0, kind = CityPixelBrushKind.Building,
            typeKey = "Retail", heightCells = 1, buildingKind = CivilSystemKind.Mall
        });

        var go = new GameObject("runtime");
        var runtime = go.AddComponent<CityPixelGridRuntime>();
        runtime.grid = grid;
        runtime.materializePrefabs = true;
        runtime.materializeRoot = go.transform;
        runtime.MaterializeStampsForFrame(0);

        var hosts = go.GetComponentsInChildren<FloorPlanIndexMapHost>();
        Assert.AreEqual(1, hosts.Length);
        Assert.IsNotNull(hosts[0].map);
        Assert.IsTrue(hosts[0].TryGetAttendantDialog(0, "Retail", out _));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(grid);
        Object.DestroyImmediate(catalog);
    }
}
