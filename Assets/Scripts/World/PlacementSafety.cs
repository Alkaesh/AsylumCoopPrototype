using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.World
{
    public static class PlacementSafety
    {
        private static readonly Vector2[] SearchPattern =
        {
            Vector2.zero,
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(0.85f, 0.85f),
            new Vector2(-0.85f, 0.85f),
            new Vector2(0.85f, -0.85f),
            new Vector2(-0.85f, -0.85f),
            new Vector2(1.8f, 0f),
            new Vector2(-1.8f, 0f),
            new Vector2(0f, 1.8f),
            new Vector2(0f, -1.8f)
        };

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
            for (int index = 0; index < SearchPattern.Length; index++)
            {
                Vector2 offset = SearchPattern[index];
                Vector3 candidate = desiredPosition + new Vector3(offset.x, 0f, offset.y);
                if (!TryResolveCandidate(
                        candidate,
                        desiredRotation,
                        clearanceExtents,
                        requireNavMesh,
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
            if (target == null)
            {
                return false;
            }

            if (!TryResolvePlacement(
                    desiredPosition,
                    desiredRotation,
                    clearanceExtents,
                    requireNavMesh,
                    out Pose resolvedPose,
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
            Vector3 clearanceExtents,
            bool requireNavMesh,
            float groundProbeHeight,
            float groundProbeDistance,
            float navMeshSampleDistance,
            int collisionMask,
            out Pose resolvedPose)
        {
            Vector3 probeOrigin = desiredPosition + Vector3.up * Mathf.Max(groundProbeHeight, clearanceExtents.y + 1.8f);
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

            Vector3 resolvedPosition = new Vector3(desiredPosition.x, groundHit.point.y, desiredPosition.z);
            if (requireNavMesh)
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

            Vector3 overlapCenter = resolvedPosition + Vector3.up * (clearanceExtents.y + 0.06f);
            Collider[] overlaps = Physics.OverlapBox(
                overlapCenter,
                clearanceExtents,
                desiredRotation,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.isTrigger)
                {
                    continue;
                }

                if (overlap.bounds.max.y <= resolvedPosition.y + 0.08f)
                {
                    continue;
                }

                resolvedPose = default;
                return false;
            }

            resolvedPose = new Pose(resolvedPosition, desiredRotation);
            return true;
        }
    }
}
