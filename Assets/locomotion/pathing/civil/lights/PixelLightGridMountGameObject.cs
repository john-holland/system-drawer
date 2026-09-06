using UnityEngine;

/// <summary>Snaps PixelLight rigs to a local grid for bake / fine positioning on models.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Lights/Pixel Light Grid Mount")]
public sealed class PixelLightGridMountGameObject : MonoBehaviour
{
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 0.25f;
    public Vector3 localPlaneNormal = Vector3.up;
    public bool snapToBake = true;
    public Vector3 fineOffset;
    public bool onlyActivateLightSource;
    public Material[] emissionMaterials;
    public PixelLightRig rig;
    public PixelLightPatternAsset pattern;
    public int mountCellX;
    public int mountCellY;
    public GameObject attachToModelPiece;
    public int minigridW = 1;
    public int minigridH = 1;
    public int nestedMinigridW = 1;
    public int nestedMinigridH = 1;
    public bool recursiveBlock;
    public int centroidCellX;
    public int centroidCellY;
    public RadialSide radialSide = RadialSide.Center;
    public RadialBuildSpec radialBuild = new RadialBuildSpec();
    public CustomRadialSideAsset customRadialSide;
    public int previewConfigIndex = -1;
    public RadialBuildHost radialHost;

    public RadialBuildHost ResolvedRadialHost() =>
        radialHost != null ? radialHost : GetComponent<RadialBuildHost>();

    public Vector3 CellLocalPosition(int x, int y) =>
        CellLocalPosition(gridWidth, gridHeight, cellSize, localPlaneNormal, fineOffset, x, y);

    public static Vector3 CellLocalPosition(
        int gridWidth, int gridHeight, float cellSize, Vector3 localPlaneNormal, Vector3 fineOffset, int x, int y)
    {
        float ox = (x - gridWidth * 0.5f + 0.5f) * cellSize;
        float oy = (y - gridHeight * 0.5f + 0.5f) * cellSize;
        Vector3 n = localPlaneNormal.sqrMagnitude > 1e-6f ? localPlaneNormal.normalized : Vector3.up;
        Vector3 t = Vector3.Cross(n, Vector3.right);
        if (t.sqrMagnitude < 1e-6f) t = Vector3.Cross(n, Vector3.forward);
        t.Normalize();
        Vector3 b = Vector3.Cross(n, t);
        return t * ox + b * oy + fineOffset;
    }

    public Bounds CellBounds(int x, int y) =>
        new Bounds(CellLocalPosition(x, y), Vector3.one * Mathf.Max(0.01f, cellSize));

    public System.Collections.Generic.List<PixelLightRadialStampCell> EnumerateRadialStamp()
    {
        var spec = radialBuild ?? new RadialBuildSpec();
        spec.side = radialSide;
        spec.minigridW = minigridW;
        spec.minigridH = minigridH;
        spec.centroidCellX = centroidCellX;
        spec.centroidCellY = centroidCellY;
        if (customRadialSide != null)
        {
            spec.useCustomSide = true;
            spec.customSide = customRadialSide.ToPose();
        }
        var host = ResolvedRadialHost();
        if (host != null && host.spec != null)
            spec = host.spec;
        return PixelLightRadialStamp.Enumerate(
            gridWidth, gridHeight, cellSize, localPlaneNormal, fineOffset,
            centroidCellX, centroidCellY, minigridW, minigridH, radialSide, spec,
            customRadialSide != null ? customRadialSide.ToPose() : default,
            customRadialSide != null, recursiveBlock, nestedMinigridW, nestedMinigridH);
    }

    public PixelLightRig EnsureRig()
    {
        if (rig != null) return rig;
        Transform parent = attachToModelPiece != null ? attachToModelPiece.transform : transform;
        Vector3 local = CellLocalPosition(mountCellX, mountCellY);
        if (snapToBake)
        {
            var host = new GameObject("PixelLightMount_" + mountCellX + "_" + mountCellY);
            host.transform.SetParent(parent, false);
            host.transform.localPosition = local;
            var go = PixelLightPrefabFactory.CreateDefaultRuntime(host.transform);
            rig = go != null ? go.GetComponent<PixelLightRig>() : null;
        }
        else
        {
            rig = GetComponentInChildren<PixelLightRig>();
            if (rig == null)
            {
                var go = PixelLightPrefabFactory.CreateDefaultRuntime(parent);
                rig = go != null ? go.GetComponent<PixelLightRig>() : null;
            }
            if (rig != null)
                rig.transform.localPosition = local;
        }

        if (pattern != null && rig != null)
            rig.SetPattern(pattern);
        return rig;
    }

    public static PixelLightGridMountGameObject PickClosest(
        PixelLightGridMountGameObject[] mounts, Ray ray, int columns = 8, float maxDist = 50f)
    {
        if (mounts == null || mounts.Length == 0) return null;
        PixelLightGridMountGameObject best = null;
        float bestDist = float.MaxValue;
        for (int c = 0; c < columns; c++)
        {
            float u = columns <= 1 ? 0f : (c / (float)(columns - 1) - 0.5f) * 0.2f;
            Ray col = new Ray(ray.origin + ray.direction * 0.01f + Vector3.Cross(ray.direction, Vector3.up) * u, ray.direction);
            for (int i = 0; i < mounts.Length; i++)
            {
                var m = mounts[i];
                if (m == null) continue;
                float d = Vector3.Cross(col.direction, m.transform.position - col.origin).magnitude;
                float along = Vector3.Dot(m.transform.position - col.origin, col.direction);
                if (along <= 0f || along >= maxDist) continue;
                float score = d * 1000f + along;
                if (score < bestDist)
                {
                    bestDist = score;
                    best = m;
                }
            }
        }
        return best;
    }
}
