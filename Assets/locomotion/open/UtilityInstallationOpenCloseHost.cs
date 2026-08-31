using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Receives <see cref="UtilityRoomBootstrap.RequestInstallOpenCloseBt"/> without a Runtime→Open assembly reference.</summary>
    [AddComponentMenu("Locomotion/Open/Utility Installation Open Close Host")]
    public sealed class UtilityInstallationOpenCloseHost : MonoBehaviour
    {
        public void BakeUtilityInstallationOpenClose(UtilityRoomBootstrap room)
        {
            if (room == null) return;
            UtilityInstallationOpenCloseBt.Bake(room, transform, room.transform);
        }
    }
}
