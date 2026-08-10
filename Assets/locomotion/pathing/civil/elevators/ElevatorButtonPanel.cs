using UnityEngine;

/// <summary>PixelLight grid floor buttons — press/hit channel → CallFloor.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Elevators/Elevator Button Panel")]
public sealed class ElevatorButtonPanel : MonoBehaviour
{
    public ElevatorVehicleRagdoll elevator;
    public PixelLightGridMountGameObject mount;
    public int gridColumns = 3;
    public Material buttonMaterial;
    public string sdfNumberInsertId = "elevator_btn_digit";

    public bool TryPressCell(int cellX, int cellY)
    {
        if (elevator == null) return false;
        int floor = cellY * Mathf.Max(1, gridColumns) + cellX + elevator.minFloor;
        bool ok = elevator.CallFloor(floor);
        if (ok && mount != null)
        {
            var rig = mount.EnsureRig();
            rig?.SetSolidChannel(Color.green, true);
        }
        return ok;
    }

    public void EnsureMount()
    {
        if (mount == null)
            mount = GetComponent<PixelLightGridMountGameObject>()
                    ?? gameObject.AddComponent<PixelLightGridMountGameObject>();
        mount.gridWidth = gridColumns;
        mount.gridHeight = Mathf.Max(1, elevator != null
            ? Mathf.CeilToInt((elevator.maxFloor - elevator.minFloor + 1) / (float)gridColumns)
            : 4);
        mount.EnsureRig();
    }
}
