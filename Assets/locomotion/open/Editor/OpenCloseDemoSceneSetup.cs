#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative.Music;
using Locomotion.Open;
using Locomotion.Open.Topology;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Locomotion.Open.Editor
{
    public static class OpenCloseDemoSceneSetup
    {
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = SceneDir + "/dresser_music_box_demo.unity";
        const string TopologyPath = "Assets/locomotion/open/DresserMusicBoxTopology.asset";

        [MenuItem("GameObject/Locomotion/Dresser Music Box Demo", false, 20)]
        public static void CreateDemo()
        {
            EnsureFolder(SceneDir);
            EnsureFolder("Assets/locomotion/open");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var dresser = new GameObject("Dresser");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(dresser.transform);
            body.transform.localScale = new Vector3(1.2f, 1f, 0.5f);

            var guard = new GameObject("TopGuard");
            guard.transform.SetParent(dresser.transform);
            guard.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var latch = guard.AddComponent<OpenableLatch>();

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(dresser.transform);
            door.transform.localPosition = new Vector3(0f, 0f, -0.26f);
            door.transform.localScale = new Vector3(1f, 0.9f, 0.05f);
            var doorRb = door.AddComponent<Rigidbody>();
            doorRb.isKinematic = true;
            var doorDriver = door.AddComponent<OpenableJointDriver>();
            doorDriver.targetOpenAngle = 75f;

            var musicBox = new GameObject("MusicBox");
            musicBox.transform.SetParent(door.transform);
            musicBox.transform.localPosition = new Vector3(0f, -0.1f, -0.15f);
            musicBox.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            var boxMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxMesh.transform.SetParent(musicBox.transform);
            boxMesh.transform.localPosition = Vector3.zero;

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            lid.transform.SetParent(musicBox.transform);
            lid.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            lid.transform.localScale = new Vector3(1f, 0.2f, 1f);
            var lidDriver = lid.AddComponent<OpenableJointDriver>();
            lidDriver.targetOpenAngle = 110f;
            lidDriver.usePhysicsMotor = false;

            var actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = "Actor";
            actor.transform.position = new Vector3(0f, 1f, 1.5f);
            actor.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

            var camRig = new GameObject("CameraPathingRig");
            camRig.AddComponent<Locomotion.Camera.CameraPathingRig>();
            var camSeq = camRig.AddComponent<OpenCloseCameraSequence>();

            var btHost = new GameObject("OpenCloseBT");
            var plan = btHost.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            plan.actor = actor.transform;
            plan.cameraSequence = camSeq;
            plan.persistBakedSteps = true;
            var seq = btHost.AddComponent<OpenCloseSequenceNode>();
            seq.actor = actor.transform;
            seq.cameraSequence = camSeq;

            var topology = AssetDatabase.LoadAssetAtPath<OpenCloseTopologyAsset>(TopologyPath);
            if (topology == null)
            {
                topology = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
                topology.ClearTopology();
                topology.Root = new OpenCloseTopologyNode { nodeId = "dresser" };
                ConcaveExposeScanner.ScanHierarchy(dresser.transform, topology, topology.Root, AutoCloseBtMode.OnStopExit);
                var flat = new System.Collections.Generic.List<OpenCloseTopologyNode>();
                foreach (var n in topology.EnumerateDepthFirst()) flat.Add(n);
                if (flat.Count > 0) { flat[0].arrivalBlendCoefficient = 0.35f; flat[0].autoCloseBt = AutoCloseBtMode.OnSequenceEnd; }
                if (flat.Count > 1) { flat[1].arrivalBlendCoefficient = 0f; flat[1].autoCloseBt = AutoCloseBtMode.AfterChildren; }
                if (flat.Count > 2) { flat[2].arrivalBlendCoefficient = 0f; flat[2].autoCloseBt = AutoCloseBtMode.None; flat[2].beatProfile = CreateMusicBoxProfile(); }
                AssetDatabase.CreateAsset(topology, TopologyPath);
            }

            plan.topology = topology;
            plan.BakeFromTopology();
            seq.topology = topology;
            seq.RebuildFromTopology();

            var musicBridge = new GameObject("CausalityMusicBridge").AddComponent<CausalityMusicBridge>();
            var rootChildren = new List<OpenCloseTopologyNode>(topology.GetChildren(topology.Root));
            musicBridge.compositionPlan = rootChildren.Count > 0
                ? rootChildren[0].beatProfile?.musicPlan
                : null;
            var router = btHost.AddComponent<OpenCloseBeatMessageRouter>();
            router.musicBridge = musicBridge;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Saved dresser music box demo to {ScenePath}");
        }

        static OpenCloseBeatProfile CreateMusicBoxProfile()
        {
            var p = ScriptableObject.CreateInstance<OpenCloseBeatProfile>();
            p.playMusicOnOpen = true;
            p.name = "MusicBoxBeat";
            AssetDatabase.CreateAsset(p, "Assets/locomotion/open/MusicBoxBeatProfile.asset");
            return p;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                string cur = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = cur + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }
        }
    }
}
#endif
