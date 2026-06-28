using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace DestructibleEnvironment
{
    public static class DestructibleMaterialStrength
    {
        public static float ResolveStrength(
            Vector3 worldPoint,
            PhysicsMaterial pm,
            WeatherPhysicsManifold manifold,
            DestructibleMaterialProfile profile)
        {
            float threshold = profile != null ? profile.baseBreakThresholdN : 500f;
            float minScale = profile != null ? profile.minMatScale : 0.5f;
            float maxScale = profile != null ? profile.maxMatScale : 2f;
            float tensionScale = profile != null ? profile.tensionScale : 0.25f;
            float porosityWeakening = profile != null ? profile.porosityWeakening : 0.35f;

            if (pm != null)
            {
                float friction = Mathf.Clamp01((pm.dynamicFriction + pm.staticFriction) * 0.5f);
                float bouncy = Mathf.Clamp01(pm.bounciness);
                float absorb = Mathf.Clamp01(friction * 0.7f + (1f - bouncy) * 0.3f);
                threshold *= Mathf.Lerp(minScale, maxScale, absorb);
            }

            if (manifold != null)
            {
                ManifoldCellData cell = manifold.GetDataAtPosition(worldPoint);
                threshold *= 1f + cell.surfaceTensionCoeff * tensionScale - cell.surfacePorosity * porosityWeakening;
            }

            return Mathf.Max(threshold, 1f);
        }
    }

    public static class DestructibleBreakEvaluator
    {
        public static HashSet<int> EvaluateDetachedPieces(
            DestructibleBakeAsset bake,
            DestructibleImpactContext impact,
            DestructibleMaterialProfile profile,
            WeatherPhysicsManifold manifold,
            AnimationCurve pieceRetentionCurve,
            float gravityBias,
            float impactFalloffM,
            Matrix4x4 localToWorld)
        {
            var detached = new HashSet<int>();
            if (bake == null || bake.pieces == null || bake.pieces.Count == 0)
                return detached;

            Vector3 gravityDir = impact.gravityDir.sqrMagnitude > 1e-6f ? impact.gravityDir.normalized : Vector3.down;
            Vector3 biasDir = (impact.impulseDir + gravityDir * gravityBias).normalized;
            float falloff = Mathf.Max(impactFalloffM, 0.01f);

            for (int i = 0; i < bake.pieces.Count; i++)
            {
                DestructiblePieceRecord piece = bake.pieces[i];
                Vector3 worldCentroid = localToWorld.MultiplyPoint3x4(piece.localCentroid);
                float distance = Vector3.Distance(worldCentroid, impact.worldPoint);
                float falloffFactor = Mathf.Exp(-distance / falloff);

                float alignment;
                if (distance < 1e-4f)
                    alignment = 1f;
                else
                {
                    Vector3 toPiece = (worldCentroid - impact.worldPoint).normalized;
                    alignment = Mathf.Max(0f, Vector3.Dot(biasDir, toPiece));
                }

                float stress = impact.impulseN * falloffFactor * alignment;

                float strength = DestructibleMaterialStrength.ResolveStrength(
                    worldCentroid,
                    impact.colliderMaterial,
                    manifold,
                    profile);

                float retention = pieceRetentionCurve != null
                    ? Mathf.Max(0.01f, pieceRetentionCurve.Evaluate(piece.normalizedVolume))
                    : Mathf.Lerp(0.5f, 3f, piece.normalizedVolume);

                if (stress > strength * retention)
                    detached.Add(piece.pieceId);
            }

            return detached;
        }
    }
}
