#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class PlanarSplinePathLocomotionTests
{
    static PlanarSplinePathLocomotion MakePath(PlanarSplineGranularityMode mode)
    {
        var go = new GameObject("psp");
        var path = go.AddComponent<PlanarSplinePathLocomotion>();
        path.controlPoints = new System.Collections.Generic.List<Vector3>
        {
            Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 20f)
        };
        path.granularity = mode;
        path.divisionStopCount = 4;
        path.perLengthMeters = 5f;
        path.defaultWidth = 2f;
        path.Rebuild();
        return path;
    }

    [Test]
    public void Division_BuildsExpectedPlaneCount()
    {
        var path = MakePath(PlanarSplineGranularityMode.Division);
        try
        {
            Assert.AreEqual(4, path.planes.Count);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }

    [Test]
    public void PerLength_BuildsPlanesByMeterSpacing()
    {
        var path = MakePath(PlanarSplineGranularityMode.PerLength);
        try
        {
            Assert.GreaterOrEqual(path.planes.Count, 4);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }

    [Test]
    public void CustomSection_OverridesAutoAtMidpoint()
    {
        var path = MakePath(PlanarSplineGranularityMode.Division);
        try
        {
            path.customSections.Add(new PlanarSplineCustomSection
            {
                startT01 = 0.2f,
                endT01 = 0.3f,
                width = 4f,
                hierarchicalPlaneId = "wide_aisle"
            });
            path.Rebuild();
            Assert.IsTrue(path.planes.Exists(p => p.hierarchicalPlaneId == "wide_aisle"));
            Assert.AreEqual(2f, path.planes.Find(p => p.hierarchicalPlaneId == "wide_aisle").halfWidth, 0.01f);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }

    [Test]
    public void LedgeWalls_SpawnWhenEnabledWithHeight()
    {
        var path = MakePath(PlanarSplineGranularityMode.Division);
        try
        {
            path.blockFallUnlessJump = true;
            path.jumpWallHeight = 1.2f;
            path.Rebuild();
            Assert.Greater(path.GetComponentsInChildren<BoxCollider>().Length, 0);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }

    [Test]
    public void GizmoSaveRevert_RoundTripsLocalTRS()
    {
        var path = MakePath(PlanarSplineGranularityMode.Division);
        try
        {
            path.customSections.Add(new PlanarSplineCustomSection
            {
                startT01 = 0f,
                endT01 = 0.2f,
                width = 1f,
                gizmoLocalPosition = Vector3.zero,
                gizmoLocalEuler = Vector3.zero,
                gizmoLocalScale = Vector3.one
            });
            var go = new GameObject("gizmo");
            go.transform.SetParent(path.transform, false);
            path.customSections[0].gizmoTransform = go.transform;
            go.transform.localPosition = new Vector3(1f, 0f, 0f);
            go.transform.localEulerAngles = new Vector3(0f, 15f, 0f);
            go.transform.localScale = new Vector3(2f, 1f, 1f);
            path.ApplyGizmoSave(0);
            Assert.AreEqual(1f, path.customSections[0].gizmoLocalPosition.x, 0.01f);
            path.ApplyGizmoRevert(0, Vector3.zero, Vector3.zero, Vector3.one);
            Assert.AreEqual(0f, path.customSections[0].gizmoLocalPosition.x, 0.01f);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }

    [Test]
    public void ClampToPath_ProjectsOntoRibbon()
    {
        var path = MakePath(PlanarSplineGranularityMode.Division);
        try
        {
            Vector3 far = new Vector3(50f, 0f, 5f);
            Vector3 clamped = path.ClampToPath(far);
            Assert.Less(Mathf.Abs(clamped.x), path.defaultWidth);
        }
        finally { Object.DestroyImmediate(path.gameObject); }
    }
}
#endif
