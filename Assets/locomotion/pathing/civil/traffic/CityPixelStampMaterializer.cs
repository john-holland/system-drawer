using UnityEngine;

public static class CityPixelStampMaterializer
{
    public static GameObject Materialize(
        CityPixelBrushStamp s,
        Vector3 world,
        Quaternion rot,
        Transform parent,
        TrafficWarden warden)
    {
        if (s == null) return null;
        GameObject go = null;
        switch (s.kind)
        {
            case CityPixelBrushKind.StreetLight:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : StreetLightPrefabFactory.CreateStreetLightPhonePole(parent);
                go.transform.SetPositionAndRotation(world, rot);
                break;
            case CityPixelBrushKind.TrafficSignal:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : StreetLightPrefabFactory.CreateTrafficSignalPhonePole(parent);
                go.transform.SetPositionAndRotation(world, rot);
                warden?.RefreshLights();
                break;
            case CityPixelBrushKind.PhonePole:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : StreetLightPrefabFactory.CreatePhonePole(parent);
                go.transform.SetPositionAndRotation(world, rot);
                break;
            case CityPixelBrushKind.PedCallButton:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : StreetLightPrefabFactory.CreateStandaloneButton(parent);
                go.transform.SetPositionAndRotation(world, rot);
                var act = go.GetComponent<RoadComponentMeshActivator>();
                if (act != null)
                    act.target = Object.FindFirstObjectByType<TrafficLightController>();
                break;
            case CityPixelBrushKind.Crosswalk:
                go = new GameObject("Crosswalk");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var decal = go.AddComponent<CrosswalkDecal>();
                decal.barCount = s.barCount;
                decal.barWidthM = s.barWidthM;
                decal.acrossLanes = s.acrossLanes;
                decal.Apply();
                go.AddComponent<RoadLaneLemmaResolver>().placeholderName = RoadLaneLemmaPropertyKeys.Crosswalk;
                break;
            case CityPixelBrushKind.Sidewalk:
                go = new GameObject("Sidewalk");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var ribbon = go.AddComponent<SidewalkRibbon>();
                ribbon.widthM = s.laneConfig != null ? s.laneConfig.sidewalkWidthM : 1.8f;
                ribbon.paddingM = s.laneConfig != null ? s.laneConfig.sidewalkPaddingM : 0.2f;
                ribbon.mattingWidth01 = s.laneConfig != null ? s.laneConfig.mattingWidth01 : 0f;
                go.AddComponent<RoadLaneLemmaResolver>().placeholderName = RoadLaneLemmaPropertyKeys.Sidewalk;
                break;
            case CityPixelBrushKind.GrassStrip:
                go = new GameObject("GrassStrip");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var grass = go.AddComponent<LotGrassGrowthController>();
                grass.stripWidthM = s.stripWidthM > 0.01f ? s.stripWidthM : 0.8f;
                go.AddComponent<RoadLaneLemmaResolver>().placeholderName = RoadLaneLemmaPropertyKeys.GrassStrip;
                break;
            case CityPixelBrushKind.JerseyBarrier:
            case CityPixelBrushKind.GuardRail:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : new GameObject(s.kind.ToString());
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var bend = go.GetComponent<RoadSplineLengthBend>() ?? go.AddComponent<RoadSplineLengthBend>();
                bend.bendWithRoad = s.bendWithRoad;
                bend.laneDisabled = s.kind == CityPixelBrushKind.JerseyBarrier && s.laneDisabled;
                go.AddComponent<RoadLaneLemmaResolver>().placeholderName =
                    s.kind == CityPixelBrushKind.JerseyBarrier
                        ? RoadLaneLemmaPropertyKeys.JerseyBarrier
                        : RoadLaneLemmaPropertyKeys.GuardRail;
                break;
            case CityPixelBrushKind.WireEnd:
                go = new GameObject("WireEnd");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var end = go.AddComponent<StreetWireEnd>();
                end.poleId = s.poleId;
                end.wireId = s.wireId;
                end.t01 = s.wireT01;
                end.kind = s.wireEndKind;
                end.Resolve();
                break;
            case CityPixelBrushKind.Debris:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : new GameObject("Debris");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                break;
            case CityPixelBrushKind.StopSign:
            case CityPixelBrushKind.Sign:
            case CityPixelBrushKind.Detour:
                go = s.signPrefab != null
                    ? Object.Instantiate(s.signPrefab, world, rot, parent)
                    : new GameObject(s.kind.ToString());
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(world, rot);
                var pot = go.GetComponent<SignStopPotential>() ?? go.AddComponent<SignStopPotential>();
                pot.stopPotential01 = s.stopPotential01;
                if (s.kind == CityPixelBrushKind.StopSign && pot.stopPotential01 <= 0f)
                    pot.stopPotential01 = 1f;
                if (s.kind == CityPixelBrushKind.Sign && Mathf.Approximately(s.stopPotential01, 1f))
                    pot.stopPotential01 = 0f;
                if (s.kind == CityPixelBrushKind.Detour && Mathf.Approximately(s.stopPotential01, 1f))
                    pot.stopPotential01 = 0.85f;
                go.AddComponent<RoadLaneLemmaResolver>().placeholderName = RoadLaneLemmaPropertyKeys.RoadSign;
                break;
            case CityPixelBrushKind.Bridge:
            case CityPixelBrushKind.BridgeAndUnderpass:
            case CityPixelBrushKind.Overpass:
            case CityPixelBrushKind.RoadLanes:
                if (s.laneConfig != null)
                    CityPixelRecipeApplier.Apply(null, s.laneConfig, s.frameIndex, s.cellX, s.cellY);
                break;
        }
        return go;
    }
}
