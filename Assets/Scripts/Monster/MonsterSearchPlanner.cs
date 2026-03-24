using System.Collections.Generic;
using UnityEngine;

namespace AsylumHorror.Monster
{
    public static class MonsterSearchPlanner
    {
        public static List<MonsterSearchProbe> BuildPlan(
            Vector3 origin,
            Vector3 anchor,
            Vector3 preferredDirection,
            IReadOnlyList<Vector3> doorwayPoints,
            IReadOnlyList<Vector3> patrolPoints,
            float searchRadius)
        {
            List<MonsterSearchProbe> plan = new List<MonsterSearchProbe>(12);

            Vector3 forward = Vector3.ProjectOnPlane(preferredDirection, Vector3.up);
            if (forward.sqrMagnitude <= 0.01f)
            {
                forward = Vector3.ProjectOnPlane(anchor - origin, Vector3.up);
            }

            if (forward.sqrMagnitude <= 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            AddProbe(plan, anchor + forward * 1.8f, anchor + forward * 4f, MonsterSearchProbeType.Intercept, 0.42f);
            AddProbe(plan, anchor + forward * 3.8f, anchor + forward * 5.8f, MonsterSearchProbeType.Intercept, 0.32f);
            AddProbe(plan, anchor + forward * 1.1f + right * 1.8f, anchor + right * 3.2f, MonsterSearchProbeType.Corner, 0.66f);
            AddProbe(plan, anchor + forward * 1.1f - right * 1.8f, anchor - right * 3.2f, MonsterSearchProbeType.Corner, 0.66f);
            AddProbe(plan, anchor - right * 2.2f, anchor - right * 3.8f, MonsterSearchProbeType.Corner, 0.56f);
            AddProbe(plan, anchor + right * 2.2f, anchor + right * 3.8f, MonsterSearchProbeType.Corner, 0.56f);

            if (doorwayPoints != null)
            {
                for (int index = 0; index < doorwayPoints.Count && index < 4; index++)
                {
                    Vector3 doorway = doorwayPoints[index];
                    Vector3 toDoor = Vector3.ProjectOnPlane(doorway - anchor, Vector3.up);
                    Vector3 doorForward = toDoor.sqrMagnitude > 0.01f ? toDoor.normalized : forward;
                    Vector3 doorRight = Vector3.Cross(Vector3.up, doorForward).normalized;

                    AddProbe(plan, doorway, doorway + doorForward * 1.8f, MonsterSearchProbeType.Doorway, 0.82f);
                    AddProbe(plan, doorway + doorRight * 1.1f, doorway + doorForward * 1.6f, MonsterSearchProbeType.Corner, 0.62f);
                    AddProbe(plan, doorway - doorRight * 1.1f, doorway + doorForward * 1.6f, MonsterSearchProbeType.Corner, 0.62f);
                }
            }

            if (patrolPoints != null)
            {
                for (int index = 0; index < patrolPoints.Count && index < 3; index++)
                {
                    Vector3 patrolPoint = patrolPoints[index];
                    AddProbe(plan, patrolPoint, patrolPoint + forward * 1.2f, MonsterSearchProbeType.Room, 0.52f);
                }
            }

            AddProbe(plan, anchor - forward * Mathf.Min(2.6f, searchRadius * 0.55f), anchor, MonsterSearchProbeType.Fallback, 0.36f);
            AddProbe(plan, anchor + right * Mathf.Min(3f, searchRadius * 0.64f), anchor + right * 4f, MonsterSearchProbeType.Fallback, 0.32f);
            AddProbe(plan, anchor - right * Mathf.Min(3f, searchRadius * 0.64f), anchor - right * 4f, MonsterSearchProbeType.Fallback, 0.32f);

            return plan;
        }

        private static void AddProbe(
            List<MonsterSearchProbe> plan,
            Vector3 position,
            Vector3 focusPoint,
            MonsterSearchProbeType type,
            float dwellSeconds)
        {
            if (plan == null)
            {
                return;
            }

            Vector3 planarPosition = new Vector3(position.x, 0f, position.z);
            for (int index = 0; index < plan.Count; index++)
            {
                Vector3 existing = plan[index].Position;
                Vector3 planarExisting = new Vector3(existing.x, 0f, existing.z);
                if (Vector3.Distance(planarExisting, planarPosition) <= 1.05f)
                {
                    return;
                }
            }

            plan.Add(new MonsterSearchProbe(position, focusPoint, type, dwellSeconds));
        }
    }
}
