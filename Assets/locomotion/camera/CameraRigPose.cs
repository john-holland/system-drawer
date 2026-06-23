using System;
using UnityEngine;

namespace Locomotion.Camera
{
    [Serializable]
    public struct CameraRigPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
        public CameraFocusMode focusMode;

        public static CameraRigPose FromCamera(UnityEngine.Camera cam, CameraFocusMode mode)
        {
            if (cam == null)
                return default;
            return new CameraRigPose
            {
                position = cam.transform.position,
                rotation = cam.transform.rotation,
                fieldOfView = cam.fieldOfView,
                focusMode = mode,
            };
        }

        public void ApplyTo(UnityEngine.Camera cam)
        {
            if (cam == null) return;
            cam.transform.SetPositionAndRotation(position, rotation);
            cam.fieldOfView = fieldOfView;
        }
    }
}
