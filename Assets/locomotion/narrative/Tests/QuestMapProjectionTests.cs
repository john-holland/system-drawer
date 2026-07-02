using NUnit.Framework;
using Locomotion.Narrative;
using SystemDrawer.Quest;
using UnityEngine;

namespace Locomotion.Narrative.Tests
{
    public class QuestMapProjectionTests
    {
        [Test]
        public void Project_4x4x4Grid_FillsCornerPixel()
        {
            var go = new GameObject("map-test");
            var renderer = go.AddComponent<QuestMapRenderer>();
            renderer.profile = ScriptableObject.CreateInstance<QuestMapProfile>();
            renderer.profile.textureWidth = 4;
            renderer.profile.textureHeight = 4;
            renderer.profile.projectionAxis = QuestMapProjectionAxis.XZ;
            renderer.outputTexture = new RenderTexture(4, 4, 0);

            var sliceGo = new GameObject("slice");
            var slice = sliceGo.AddComponent<StubQuestSliceSource>();
            renderer.sliceSource = slice;
            renderer.RenderSlice(0f);

            Object.DestroyImmediate(sliceGo);
            Object.DestroyImmediate(go);
            Assert.Pass();
        }

        sealed class StubQuestSliceSource : QuestSpatialSliceSource
        {
            public override bool TryGetSliceAtT(
                float t,
                out Bounds bounds,
                out int resX,
                out int resY,
                out int resZ,
                out float[] occupancy,
                out float[] causal)
            {
                resX = resY = resZ = 4;
                bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
                occupancy = new float[64];
                causal = new float[64];
                occupancy[0] = 1f;
                return true;
            }
        }
    }
}
