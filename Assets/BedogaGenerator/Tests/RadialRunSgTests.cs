using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RadialRunSgTests
{
    [Test]
    public void ConfigureRepeat_SetsPerParentLimitsAndRadialMode()
    {
        var go = new GameObject("radial");
        var run = go.AddComponent<RadialRunNode>();
        var pieceGo = new GameObject("piece");
        pieceGo.transform.SetParent(go.transform, false);
        var piece = pieceGo.AddComponent<HouseShellNode>();
        run.ConfigureRepeat(6);
        Assert.AreEqual(1, run.placementLimit);
        Assert.AreEqual(SGBehaviorTreeNode.PlaceSearchMode.Radial, run.placeSearchMode);
        Assert.AreEqual(SGBehaviorTreeNode.PlacementMode.Around, run.placementMode);
        Assert.AreEqual(6, run.radialBuild.count);
        Assert.IsTrue(piece.perParentPlacementLimits);
        Assert.AreEqual(6, piece.placementLimit);
        Assert.AreEqual(SGBehaviorTreeNode.PlaceSearchMode.Radial, piece.placeSearchMode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void UsesRadial_AndResolvedSpecFromHost()
    {
        var go = new GameObject("node");
        var node = go.AddComponent<SGBehaviorTreeNode>();
        node.placeSearchMode = SGBehaviorTreeNode.PlaceSearchMode.Radial;
        Assert.IsTrue(node.UsesRadialPlacement());
        var hostGo = new GameObject("host");
        var host = hostGo.AddComponent<RadialBuildHost>();
        host.spec = new RadialBuildSpec { count = 8, radius = 2.5f };
        node.radialHost = host;
        Assert.AreEqual(8, node.ResolvedRadialSpec().count);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(hostGo);
    }

    [Test]
    public void Generate_PlacesChildrenAroundCenterPost()
    {
        var root = new GameObject("RadialGenRoot");
        try
        {
            var genGo = new GameObject("SpatialGenerator");
            genGo.transform.SetParent(root.transform);
            var gen = genGo.AddComponent<SpatialGenerator>();
            gen.mode = SpatialGenerator.GenerationMode.ThreeDimensional;
            gen.seed = 7;
            gen.autoGenerateOnStart = false;
            gen.placementStrategy = SpatialGenerator.PlacementStrategy.UniformQueue;
            gen.generationSize = new Vector3(40f, 10f, 40f);
            var sceneParent = new GameObject("SceneTree").transform;
            sceneParent.SetParent(root.transform);
            gen.sceneTreeParent = sceneParent;

            var post = new GameObject("CenterPost");
            post.transform.SetParent(root.transform, false);
            var host = root.AddComponent<RadialBuildHost>();
            host.centerPost = post;
            host.spec = new RadialBuildSpec
            {
                count = 4,
                radius = 1f,
                startAngleDeg = 0f,
                wrapAngleDeg = 360f,
                yawToCenter = true,
                centerPostPosition = post.transform.position
            };
            host.CreateAnchorObjects();

            var treeObj = new GameObject("BehaviorTree");
            treeObj.transform.SetParent(genGo.transform);
            var container = treeObj.AddComponent<SGTreeNodeContainer>();
            var parentObj = new GameObject("ring");
            parentObj.transform.SetParent(treeObj.transform);
            var parent = parentObj.AddComponent<SGBehaviorTreeNode>();
            parent.placementLimit = 4;
            parent.placeSearchMode = SGBehaviorTreeNode.PlaceSearchMode.Radial;
            parent.placementMode = SGBehaviorTreeNode.PlacementMode.Around;
            parent.radialHost = host;
            parent.radialBuild = host.spec;
            parent.minSpace = parent.maxSpace = parent.optimalSpace = Vector3.one;
            parent.gameObjectPrefabs = new List<GameObject> { Cube(1f) };
            container.rootNode = parent;
            gen.behaviorTreeParent = treeObj.transform;

            gen.Generate();

            var placed = new List<Transform>();
            CollectCubes(sceneParent, placed);
            Assert.GreaterOrEqual(placed.Count, 4);
            Vector3 center = post.transform.position;
            int around = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                float d = Vector3.Distance(placed[i].position, center);
                if (d > 0.4f)
                    around++;
            }
            Assert.GreaterOrEqual(around, 3);
            Vector3 start = host.StartPostAnchor.position;
            bool slot0OnStart = false;
            for (int i = 0; i < placed.Count; i++)
            {
                if (Vector3.Distance(placed[i].position, start) < 0.35f)
                    slot0OnStart = true;
            }
            Assert.IsTrue(slot0OnStart);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GlueSocket_RecordsJointId()
    {
        var go = new GameObject("piece");
        var spec = new RadialBuildSpec { joinKind = RadialJoinKind.Glue, jointId = "glue_a" };
        var sock = RadialJoinSocket.Apply(go, spec);
        Assert.IsNotNull(sock);
        Assert.AreEqual(RadialJoinKind.Glue, sock.joinKind);
        Assert.AreEqual("glue_a", sock.jointId);
        Object.DestroyImmediate(go);
    }

    static GameObject Cube(float s)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.localScale = Vector3.one * s;
        c.SetActive(false);
        return c;
    }

    static void CollectCubes(Transform t, List<Transform> dst)
    {
        if (t.GetComponent<Renderer>() != null)
            dst.Add(t);
        for (int i = 0; i < t.childCount; i++)
            CollectCubes(t.GetChild(i), dst);
    }
}
