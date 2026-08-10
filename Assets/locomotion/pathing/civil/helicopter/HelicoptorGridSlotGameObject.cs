using UnityEngine;

/// <summary>Grid slot for placing magnetos, PixelLights, or cockpit telecom/GPS webtop on a helicopter.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Helicoptor Grid Slot")]
public sealed class HelicoptorGridSlotGameObject : MonoBehaviour
{
    public enum SlotContents
    {
        Empty = 0,
        Magneto = 1,
        PixelLight = 2,
        TelecomGpsWebtop = 3
    }

    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 0.5f;
    public int cellX;
    public int cellY;
    public Vector3 fineOffset;
    public SlotContents contents;
    public GameObject attachToModelPiece;
    public MagnetoLiftParams placedMagnetoParams;
    public Transform magnetoHost;
    public PixelLightGridMountGameObject lightMount;
    public Transform telecomGpsHost;
    public HelicopterVehicleRagdoll helicopter;

    public Vector3 CellLocalPosition(int x, int y)
    {
        float ox = (x - gridWidth * 0.5f + 0.5f) * cellSize;
        float oy = (y - gridHeight * 0.5f + 0.5f) * cellSize;
        return new Vector3(ox, 0f, oy) + fineOffset;
    }

    public Transform EnsureHost(string name)
    {
        Transform parent = attachToModelPiece != null ? attachToModelPiece.transform : transform;
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = CellLocalPosition(cellX, cellY);
        return go.transform;
    }

    public Transform PlaceMagneto(MagnetoLiftParams parms = null)
    {
        magnetoHost = EnsureHost("Magneto_" + cellX + "_" + cellY);
        placedMagnetoParams = parms ?? placedMagnetoParams ?? new MagnetoLiftParams { magnetoId = "m_" + cellX + "_" + cellY };
        contents = SlotContents.Magneto;
        if (helicopter == null)
            helicopter = GetComponentInParent<HelicopterVehicleRagdoll>();
        if (helicopter != null)
        {
            if (!helicopter.magnetoAnchors.Contains(magnetoHost))
                helicopter.magnetoAnchors.Add(magnetoHost);
            if (helicopter.magnetos == null)
                helicopter.magnetos = new System.Collections.Generic.List<MagnetoLiftParams>();
            bool found = false;
            for (int i = 0; i < helicopter.magnetos.Count; i++)
            {
                if (helicopter.magnetos[i] != null && helicopter.magnetos[i].magnetoId == placedMagnetoParams.magnetoId)
                {
                    helicopter.magnetos[i] = placedMagnetoParams;
                    found = true;
                    break;
                }
            }
            if (!found)
                helicopter.magnetos.Add(placedMagnetoParams);
            if (!helicopter.gridSlots.Contains(this))
                helicopter.gridSlots.Add(this);
        }
        return magnetoHost;
    }

    public PixelLightGridMountGameObject PlacePixelLight()
    {
        var host = EnsureHost("PixelLightSlot_" + cellX + "_" + cellY);
        lightMount = host.GetComponent<PixelLightGridMountGameObject>()
                     ?? host.gameObject.AddComponent<PixelLightGridMountGameObject>();
        lightMount.mountCellX = cellX;
        lightMount.mountCellY = cellY;
        lightMount.EnsureRig();
        contents = SlotContents.PixelLight;
        if (helicopter == null)
            helicopter = GetComponentInParent<HelicopterVehicleRagdoll>();
        if (helicopter != null && !helicopter.lightMounts.Contains(lightMount))
            helicopter.lightMounts.Add(lightMount);
        return lightMount;
    }

    public Transform PlaceTelecomGpsWebtop()
    {
        telecomGpsHost = EnsureHost("TelecomGpsWebtop_" + cellX + "_" + cellY);
        contents = SlotContents.TelecomGpsWebtop;
        if (helicopter == null)
            helicopter = GetComponentInParent<HelicopterVehicleRagdoll>();
        if (helicopter != null)
        {
            helicopter.gpsWebtopMount = telecomGpsHost;
            helicopter.EnsureSystems();
            if (helicopter.gpsHud != null)
                helicopter.gpsHud.mount = telecomGpsHost;
            if (helicopter.renderPortal != null)
            {
                helicopter.renderPortal.overlayParent = telecomGpsHost;
                helicopter.renderPortal.EnsureOverlayQuad();
            }
            if (!helicopter.gridSlots.Contains(this))
                helicopter.gridSlots.Add(this);
        }
        return telecomGpsHost;
    }
}
