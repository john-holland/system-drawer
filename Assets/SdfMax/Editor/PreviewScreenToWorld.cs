#if UNITY_EDITOR
using UnityEngine;

namespace SdfMax.Editor
{
    public static class PreviewScreenToWorld
    {
        public static bool TryGetImageRect(Rect previewRect, Vector2 textureSize, out Rect imageRect)
        {
            imageRect = previewRect;
            if (textureSize.x < 1f || textureSize.y < 1f)
                return false;

            float textureAspect = textureSize.x / textureSize.y;
            float rectAspect = previewRect.width / previewRect.height;
            if (rectAspect > textureAspect)
            {
                float h = previewRect.height;
                float w = h * textureAspect;
                imageRect = new Rect(
                    previewRect.x + (previewRect.width - w) * 0.5f,
                    previewRect.y,
                    w,
                    h);
            }
            else
            {
                float w = previewRect.width;
                float h = w / textureAspect;
                imageRect = new Rect(
                    previewRect.x,
                    previewRect.y + (previewRect.height - h) * 0.5f,
                    w,
                    h);
            }
            return true;
        }

        public static bool TryRay(
            Rect previewRect,
            Vector2 mouseGuiPos,
            Vector2 textureSize,
            Camera camera,
            out Ray ray)
        {
            ray = default;
            if (camera == null || !TryGetImageRect(previewRect, textureSize, out Rect imageRect))
                return false;
            if (!imageRect.Contains(mouseGuiPos))
                return false;

            Vector2 uv = new Vector2(
                (mouseGuiPos.x - imageRect.xMin) / imageRect.width,
                1f - (mouseGuiPos.y - imageRect.yMin) / imageRect.height);
            ray = camera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            return true;
        }

        public static bool TryHitPlane(
            Rect previewRect,
            Vector2 mouseGuiPos,
            Vector2 textureSize,
            Camera camera,
            Vector3 planePoint,
            Vector3 planeNormal,
            out Vector3 hit)
        {
            hit = default;
            if (!TryRay(previewRect, mouseGuiPos, textureSize, camera, out Ray ray))
                return false;
            var plane = new Plane(planeNormal, planePoint);
            if (!plane.Raycast(ray, out float enter))
                return false;
            hit = ray.GetPoint(enter);
            return true;
        }
    }
}
#endif
