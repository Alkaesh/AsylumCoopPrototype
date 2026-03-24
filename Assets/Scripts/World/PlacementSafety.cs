using System;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.World
{
    public enum PlacementCategory
    {
        Structural = 0,
        LargeBlockingProp = 1,
        MediumClutter = 2,
        SmallDecor = 3,
        WallMounted = 4,
        InteractionObjective = 5,
        Monster = 6
    }

    public readonly struct PlacementRules
    {
        public PlacementRules(
            Vector3 clearanceExtents,
            PlacementCategory category,
            bool requireNavMesh,
            float protectedPadding = 0.1f,
            float groundClearance = 0.02f,
            float minSurfaceGap = 0.06f)
        {
            ClearanceExtents = clearanceExtents;
            Category = category;
            RequireNavMesh = requireNavMesh;
            ProtectedPadding = protectedPadding;
            GroundClearance = groundClearance;
            MinSurfaceGap = minSurfaceGap;
        }

        public Vector3 ClearanceExtents { get; }
        public PlacementCategory Category { get; }
        public bool RequireNavMesh { get; }
        public float ProtectedPadding { get; }
        public float GroundClearance { get; }
        public float MinSurfaceGap { get; }
    }

    public static class PlacementSafety
    {
        private static readonly Vector2[] SearchPattern =
        {
            Vector2.zero,
            new Vector2(0.8f, 0f),
            new Vector2(-0.8f, 0f),
            new Vector2(0f, 0.8f),
            new Vector2(0f, -0.8f),
            new Vector2(0.8f, 0.8f),
            new Vector2(-0.8f, 0.8f),
            new Vector2(0.8f, -0.8f),
            new Vector2(-0.8f, -0.8f),
            new Vector2(1.6f, 0f),
            new Vector2(-1.6f, 0f),
            new Vector2(0f, 1.6f),
            new Vector2(0f, -1.6f),
            new Vector2(1.6f, 1.6f),
            new Vector2(-1.6f, 1.6f),
            new Vector2(1.6f, -1.6f),
            new Vector2(-1.6f, -1.6f),
            new Vector2(2.4f, 0f),
            new Vector2(-2.4f, 0f),
            new Vector2(0f, 2.4f),
            new Vector2(0f, -2.4f)
        };

        public static PlacementRules ResolveRules(ObjectiveSpawnType spawnType)
        {
            return spawnType switch
            {
                ObjectiveSpawnType.Generator => new PlacementRules(
                    new Vector3(1.3f, 1.25f, 1.05f),
                    PlacementCategory.LargeBlockingProp,
                    false,
                    protectedPadding: 0.32f),
                ObjectiveSpawnType.PowerConsole => new PlacementRules(
                    new Vector3(0.88f, 0.95f, 0.42f),
                    PlacementCategory.InteractionObjective,
                    false,
                    protectedPadding: 0.22f),
                ObjectiveSpawnType.Hook => new PlacementRules(
                    new Vector3(0.65f, 1.3f, 0.65f),
                    PlacementCategory.InteractionObjective,
                    false,
                    protectedPadding: 0.24f),
                ObjectiveSpawnType.Monster => new PlacementRules(
                    new Vector3(0.6f, 1.1f, 0.6f),
                    PlacementCategory.Monster,
                    true,
                    protectedPadding: 0.4f),
                ObjectiveSpawnType.Battery => new PlacementRules(
                    new Vector3(0.18f, 0.28f, 0.18f),
                    PlacementCategory.SmallDecor,
                    false,
                    protectedPadding: 0.1f),
                ObjectiveSpawnType.Keycard => new PlacementRules(
                    new Vector3(0.22f, 0.22f, 0.22f),
                    PlacementCategory.SmallDecor,
                    false,
                    protectedPadding: 0.1f),
                _ => new PlacementRules(
                    new Vector3(0.45f, 0.9f, 0.45f),
                    PlacementCategory.MediumClutter,
                    false)
            };
        }

        public static bool TryResolvePlacement(
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            Vector3 clearanceExtents,
            bool requireNavMesh,
            out Pose resolvedPose,
            float groundProbeHeight = 3.5f,
            float groundProbeDistance = 8f,
            float navMeshSampleDistance = 1.35f,
            int collisionMask = Physics.DefaultRaycastLayers)
        {
            PlacementRules rules = new PlacementRules(
                clearanceExtents,
                PlacementCategory.LargeBlockingProp,
                requireNavMesh);

            return TryResolvePlacement(
                desiredPosition,
                desiredRotation,
                rules,
                out resolvedPose,
                null,
                groundProbeHeight,
                groundProbeDistance,
                navMeshSampleDistance,
                collisionMask);
        }

        public static bool TryResolvePlacement(
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            PlacementRules rules,
            out Pose resolvedPose,
            Transform ignoreRoot = null,
            float groundProbeHeight = 3.5f,
            float groundProbeDistance = 8f,
            float navMeshSampleDistance = 1.35f,
            int collisionMask = Physics.DefaultRaycastLayers)
        {
            for (int index = 0; index < SearchPattern.Length; index++)
            {
                Vector2 offset = SearchPattern[index];
                Vector3 candidate = desiredPosition + new Vector3(offset.x, 0f, offset.y);
                if (!TryResolveCandidate(
                        candidate,
                        desiredRotation,
                        rules,
                        ignoreRoot,
                        groundProbeHeight,
                        groundProbeDistance,
                        navMeshSampleDistance,
                        collisionMask,
                        out resolvedPose))
                {
                    continue;
                }

                return true;
            }

            resolvedPose = new Pose(desiredPosition, desiredRotation);
            return false;
        }

        public static bool TryMoveTransformToSafePlacement(
            Transform target,
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            Vector3 clearanceExtents,
            bool requireNavMesh,
            float groundProbeHeight = 3.5f,
            float groundProbeDistance = 8f,
            float navMeshSampleDistance = 1.35f,
            int collisionMask = Physics.DefaultRaycastLayers)
        {
            PlacementRules rules = new PlacementRules(
                clearanceExtents,
                PlacementCategory.LargeBlockingProp,
                requireNavMesh);

            return TryMoveTransformToSafePlacement(
                target,
                desiredPosition,
                desiredRotation,
                rules,
                groundProbeHeight,
                groundProbeDistance,
                navMeshSampleDistance,
                collisionMask);
        }

        public static bool TryMoveTransformToSafePlacement(
            Transform target,
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            PlacementRules rules,
            float groundProbeHeight = 3.5f,
            float groundProbeDistance = 8f,
            float navMeshSampleDistance = 1.35f,
            int collisionMask = Physics.DefaultRaycastLayers)
        {
            if (target == null)
            {
                return false;
            }

            if (!TryResolvePlacement(
                    desiredPosition,
                    desiredRotation,
                    rules,
                    out Pose resolvedPose,
                    target,
                    groundProbeHeight,
                    groundProbeDistance,
                    navMeshSampleDistance,
                    collisionMask))
            {
                return false;
            }

            target.SetPositionAndRotation(resolvedPose.position, resolvedPose.rotation);
            return true;
        }

        private static bool TryResolveCandidate(
            Vector3 desiredPosition,
            Quaternion desiredRotation,
            PlacementRules rules,
            Transform ignoreRoot,
            float groundProbeHeight,
            float groundProbeDistance,
            float navMeshSampleDistance,
            int collisionMask,
            out Pose resolvedPose)
        {
            Vector3 probeOrigin = desiredPosition + Vector3.up * Mathf.Max(groundProbeHeight, rules.ClearanceExtents.y + 1.8f);
            if (!Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    groundProbeDistance + probeOrigin.y,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                resolvedPose = default;
                return false;
            }

            if (groundHit.normal.y < 0.82f)
            {
                resolvedPose = default;
                return false;
            }

            Vector3 resolvedPosition = new Vector3(desiredPosition.x, groundHit.point.y, desiredPosition.z);
            if (rules.RequireNavMesh)
            {
                if (!NavMesh.SamplePosition(resolvedPosition, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    resolvedPose = default;
                    return false;
                }

                Vector2 navOffset = new Vector2(navHit.position.x - resolvedPosition.x, navHit.position.z - resolvedPosition.z);
                if (navOffset.sqrMagnitude > navMeshSampleDistance * navMeshSampleDistance)
                {
                    resolvedPose = default;
                    return false;
                }

                resolvedPosition = navHit.position;
            }

            Bounds candidateBounds = BuildCandidateBounds(resolvedPosition, desiredRotation, rules.ClearanceExtents);
            if (IsOverlappingBlockingGeometry(candidateBounds, desiredRotation, rules, ignoreRoot, collisionMask))
            {
                resolvedPose = default;
                return false;
            }

            if (ViolatesProtectedSpaces(candidateBounds))
            {
                resolvedPose = default;
                return false;
            }

            if (!HasGroundSupport(candidateBounds, groundHit.point.y, rules.GroundClearance))
            {
                resolvedPose = default;
                return false;
            }

            resolvedPose = new Pose(resolvedPosition, desiredRotation);
            return true;
        }

        private static Bounds BuildCandidateBounds(Vector3 resolvedPosition, Quaternion desiredRotation, Vector3 clearanceExtents)
        {
            Vector3 center = resolvedPosition + Vector3.up * (clearanceExtents.y + 0.06f);
            Vector3 worldSize = clearanceExtents * 2f;
            if (desiredRotation == Quaternion.identity)
            {
                return new Bounds(center, worldSize);
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, desiredRotation, Vector3.one);
            Vector3 right = AbsVector(matrix.MultiplyVector(Vector3.right)) * clearanceExtents.x;
            Vector3 up = AbsVector(matrix.MultiplyVector(Vector3.up)) * clearanceExtents.y;
            Vector3 forward = AbsVector(matrix.MultiplyVector(Vector3.forward)) * clearanceExtents.z;
            Vector3 extents = right + up + forward;
            return new Bounds(center, extents * 2f);
        }

        private static bool IsOverlappingBlockingGeometry(
            Bounds candidateBounds,
            Quaternion desiredRotation,
            PlacementRules rules,
            Transform ignoreRoot,
            int collisionMask)
        {
            Collider[] overlaps = Physics.OverlapBox(
                candidateBounds.center,
                rules.ClearanceExtents,
                desiredRotation,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.isTrigger || ShouldIgnoreCollider(overlap, ignoreRoot))
                {
                    continue;
                }

                if (overlap.bounds.max.y <= candidateBounds.min.y + 0.08f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ViolatesProtectedSpaces(Bounds candidateBounds)
        {
            PlacementProtectionVolume[] volumes = UnityEngine.Object.FindObjectsByType<PlacementProtectionVolume>(FindObjectsSortMode.None);
            foreach (PlacementProtectionVolume volume in volumes)
            {
                if (volume == null || !volume.isActiveAndEnabled)
                {
                    continue;
                }

                Bounds expanded = volume.WorldBounds;
                expanded.Expand(volume.Padding * 2f);
                if (expanded.Intersects(candidateBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGroundSupport(Bounds candidateBounds, float groundY, float tolerance)
        {
            float heightAboveGround = Mathf.Abs(candidateBounds.min.y - groundY);
            return heightAboveGround <= Mathf.Max(0.02f, tolerance);
        }

        private static bool ShouldIgnoreCollider(Collider collider, Transform ignoreRoot)
        {
            if (collider == null || ignoreRoot == null)
            {
                return false;
            }

            Transform current = collider.transform;
            return current == ignoreRoot || current.IsChildOf(ignoreRoot);
        }

        private static Vector3 AbsVector(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }
    }
}
