#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weather.CloudBake;

namespace Weather.Tests
{
    public sealed class CloudBakeTests
    {
        [Test]
        public void Raycaster_EmitsSamples_ForGradientTarget()
        {
            var viewer = new CloudViewerSpec
            {
                kind = CloudViewerKind.WorldPoint,
                worldPoint = Vector3.zero
            };
            var target = new CloudPerspectiveTarget
            {
                rayWidth = 32,
                rayHeight = 32,
                sampleStride = 8,
                gradientBands = CloudGradientBands.Parse("top=#112233 mid=#445566 bottom=#778899")
            };
            var cols = CloudPerspectiveRaycaster.SampleColumns(viewer, target, 100f, 200f, 256);
            Assert.IsNotNull(cols);
        }

        [Test]
        public void HalfShell_SphereCount_IncreasesWithColumns()
        {
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = new Vector3(0, 150, 0), targetOpacity = 0.8f },
                new CloudColumnSample { worldHit = new Vector3(10, 150, 0), targetOpacity = 0.7f }
            };
            var stack = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 3);
            Assert.GreaterOrEqual(stack.spheres.Count, 3);
        }

        [Test]
        public void FresnelSchedule_Sigma_DecreasesMonotonically()
        {
            float prev = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                float s = FresnelNoiseSchedule.Sigma(i, 16, 1f, 2f);
                Assert.LessOrEqual(s, prev);
                prev = s;
            }
        }

        [Test]
        public void AnchorReset_RestoresSphereCenters()
        {
            var stack = new CloudHalfShellStack();
            stack.spheres.Add(new CloudSpherePrimitive { center = new Vector3(1, 2, 3) });
            var anchor = CloudBakeAnchor.Capture(stack);
            stack.spheres[0] = new CloudSpherePrimitive { center = new Vector3(9, 9, 9) };
            anchor.Restore(stack);
            Assert.AreEqual(new Vector3(1, 2, 3), stack.spheres[0].center);
        }

        [Test]
        public void AllowFloatAway_False_KeepsAnchoredCentersDuringBake()
        {
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = Vector3.zero };
            var target = new CloudPerspectiveTarget
            {
                rayWidth = 16,
                rayHeight = 16,
                sampleStride = 8,
                gradientBands = new CloudGradientBands()
            };
            var columns = CloudPerspectiveRaycaster.SampleColumns(viewer, target, 100f, 200f);
            if (columns.Count == 0)
            {
                columns.Add(new CloudColumnSample { worldHit = new Vector3(0, 150, 0), targetOpacity = 0.8f, columnIndex = 0 });
            }

            var config = new CloudPerspectiveBakeConfig { maxIterations = 4, allowFloatAway = false, spheresPerColumn = 2 };
            var solver = new CloudPerspectiveBakeSolver();
            var result = solver.Bake(viewer, target, columns, null, null, null, null, config);
            var anchor = result.anchor;
            for (int i = 0; i < result.stack.spheres.Count; i++)
            {
                Assert.AreEqual(anchor.sphereCentersWorld[i], result.stack.spheres[i].center);
            }
        }

        [Test]
        public void AllowFloatAway_True_MovesSphereCentersWithWind()
        {
            var stack = new CloudHalfShellStack();
            stack.spheres.Add(new CloudSpherePrimitive { center = Vector3.zero, radius = 5f });
            stack.shellBounds = new Bounds(Vector3.zero, Vector3.one * 20f);

            var desc = new CloudAdvectionDescriptor
            {
                mode = CloudAdvectionMode.WeatherSolverAdvection,
                advectionVectorWorld = new Vector3(10f, 0f, 0f)
            };
            var before = stack.spheres[0].center;
            CloudBakeSession.Begin(true);
            try
            {
                CloudAdvectionDescriptorBuilder.ApplyAdvectionStep(desc, stack, null, null, null, 1f);
            }
            finally
            {
                CloudBakeSession.End();
            }
            Assert.AreNotEqual(before, stack.spheres[0].center);
        }

        [Test]
        public void VideoFrames_AnchorReset_KeepsZeroDriftFromAnchor()
        {
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = Vector3.zero };
            var target = new CloudPerspectiveTarget { gradientBands = new CloudGradientBands() };
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = new Vector3(5, 150, 5), targetOpacity = 0.8f, columnIndex = 0 }
            };
            var config = new CloudPerspectiveBakeConfig
            {
                maxIterations = 3,
                allowFloatAway = false,
                spheresPerColumn = 2,
                warmStartScalarsOnly = true
            };
            var solver = new CloudPerspectiveBakeSolver();
            var frame0 = solver.Bake(viewer, target, columns, null, null, null, null, config, null, null, 0);
            var frame1 = solver.Bake(viewer, target, columns, null, null, null, null, config, frame0.stack, null, 1);
            Assert.AreEqual(0f, frame1.anchor.MaxAnchorDistance(frame1.stack), 0.0001f);
        }

        [Test]
        public void AnchorHash_IsStableForSamePositions()
        {
            var stack = new CloudHalfShellStack();
            stack.spheres.Add(new CloudSpherePrimitive { center = new Vector3(1, 2, 3) });
            var a = CloudBakeAnchor.Capture(stack, null, 0);
            var b = CloudBakeAnchor.Capture(stack, null, 0);
            Assert.AreEqual(a.ComputeHash(), b.ComputeHash());
        }

        [Test]
        public void Convexion_SizeZero_NoDisplacement()
        {
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = new Vector3(1, 150, 2), targetOpacity = 0.9f, columnIndex = 0 }
            };
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = Vector3.zero };
            var baseline = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 3, viewer, null);
            var zeroSize = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 3, viewer,
                new CloudHalfShellConvexion { bias = 1f, size = 0f });
            Assert.AreEqual(baseline.spheres[0].center, zeroSize.spheres[0].center);
        }

        [Test]
        public void Convexion_BiasBack_MovesDeeperThanBiasForward()
        {
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = Vector3.zero, targetOpacity = 0.9f, columnIndex = 0 }
            };
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = new Vector3(0, 0, -100) };
            var back = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 3, viewer,
                new CloudHalfShellConvexion { bias = 1f, size = 0.8f });
            var forward = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 3, viewer,
                new CloudHalfShellConvexion { bias = -1f, size = 0.8f });
            Vector3 viewDir = viewer.ResolveForward().normalized;
            float backDepth = Vector3.Dot(back.spheres[0].center, viewDir);
            float forwardDepth = Vector3.Dot(forward.spheres[0].center, viewDir);
            Assert.Greater(backDepth, forwardDepth);
        }

        [Test]
        public void Convexion_CenterColumnMovesMoreThanEdge()
        {
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = Vector3.zero, targetOpacity = 0.9f, columnIndex = 0 },
                new CloudColumnSample { worldHit = new Vector3(80, 0, 0), targetOpacity = 0.9f, columnIndex = 1 }
            };
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = new Vector3(0, 0, -200) };
            const int spheresPerColumn = 3;
            var before = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, spheresPerColumn, viewer, null);
            int centerIdx = spheresPerColumn - 1;
            int edgeIdx = 2 * spheresPerColumn - 1;
            var centerBefore = before.spheres[centerIdx].center;
            var edgeBefore = before.spheres[edgeIdx].center;

            var after = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, spheresPerColumn, viewer,
                new CloudHalfShellConvexion { bias = 1f, size = 0.8f });
            float centerMove = Vector3.Distance(centerBefore, after.spheres[centerIdx].center);
            float edgeMove = Vector3.Distance(edgeBefore, after.spheres[edgeIdx].center);
            Assert.Greater(centerMove, edgeMove);
        }

        [Test]
        public void Convexion_AnchorCapturesShiftedCenters()
        {
            var columns = new List<CloudColumnSample>
            {
                new CloudColumnSample { worldHit = Vector3.zero, targetOpacity = 0.9f, columnIndex = 0 }
            };
            var viewer = new CloudViewerSpec { kind = CloudViewerKind.WorldPoint, worldPoint = new Vector3(0, 0, -50) };
            var stack = CloudHalfShellBuilder.Build(columns, null, 100f, 200f, 2, viewer,
                new CloudHalfShellConvexion { bias = 0.75f, size = 0.6f });
            var anchor = CloudBakeAnchor.Capture(stack);
            Assert.AreEqual(stack.spheres[0].center, anchor.sphereCentersWorld[0]);
        }
    }
}
#endif
