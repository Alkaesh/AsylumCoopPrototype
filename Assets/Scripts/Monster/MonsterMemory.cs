using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsylumHorror.Monster
{
    public sealed class MonsterMemory
    {
        private readonly List<MonsterSearchProbe> searchPlan = new List<MonsterSearchProbe>();
        private int searchIndex;

        public Vector3 LastSeenPoint { get; private set; }
        public Vector3 LastSeenDirection { get; private set; }
        public double LastSeenAt { get; private set; } = double.NegativeInfinity;
        public Vector3 LastNoisePoint { get; private set; }
        public double LastNoiseAt { get; private set; } = double.NegativeInfinity;
        public float LastNoisePriority { get; private set; }
        public NoiseCategory LastNoiseCategory { get; private set; }

        public bool HasSeen => !double.IsNegativeInfinity(LastSeenAt);
        public bool HasNoise => !double.IsNegativeInfinity(LastNoiseAt);
        public int RemainingProbeCount => Mathf.Max(0, searchPlan.Count - searchIndex);

        public void Reset(Vector3 origin)
        {
            LastSeenPoint = origin;
            LastSeenDirection = Vector3.forward;
            LastSeenAt = double.NegativeInfinity;
            LastNoisePoint = origin;
            LastNoiseAt = double.NegativeInfinity;
            LastNoisePriority = 0f;
            LastNoiseCategory = NoiseCategory.PlayerMovement;
            ClearSearchPlan();
        }

        public void RememberSight(Vector3 observerPosition, Vector3 targetPosition, Vector3 targetVelocity, double time)
        {
            LastSeenPoint = targetPosition;
            LastSeenAt = time;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(targetVelocity, Vector3.up);
            if (planarVelocity.sqrMagnitude > 0.09f)
            {
                LastSeenDirection = planarVelocity.normalized;
                return;
            }

            Vector3 directHint = Vector3.ProjectOnPlane(targetPosition - observerPosition, Vector3.up);
            if (directHint.sqrMagnitude > 0.01f)
            {
                LastSeenDirection = directHint.normalized;
            }
        }

        public void RememberNoise(NoiseEvent noiseEvent)
        {
            LastNoisePoint = noiseEvent.Position;
            LastNoiseAt = noiseEvent.Timestamp;
            LastNoisePriority = noiseEvent.Priority;
            LastNoiseCategory = noiseEvent.Category;
        }

        public bool HasRecentSight(double now, double maxAgeSeconds)
        {
            return HasSeen && now - LastSeenAt <= maxAgeSeconds;
        }

        public bool HasRecentNoise(double now, double maxAgeSeconds)
        {
            return HasNoise && now - LastNoiseAt <= maxAgeSeconds;
        }

        public Vector3 ResolveAnchor(Vector3 fallback, double now, double seenAgeSeconds, double noiseAgeSeconds)
        {
            if (HasRecentSight(now, seenAgeSeconds))
            {
                return LastSeenPoint;
            }

            if (HasRecentNoise(now, noiseAgeSeconds))
            {
                return LastNoisePoint;
            }

            return fallback;
        }

        public Vector3 ResolveDirection(Vector3 fallbackForward)
        {
            Vector3 planarDirection = Vector3.ProjectOnPlane(LastSeenDirection, Vector3.up);
            if (planarDirection.sqrMagnitude > 0.01f)
            {
                return planarDirection.normalized;
            }

            Vector3 planarFallback = Vector3.ProjectOnPlane(fallbackForward, Vector3.up);
            return planarFallback.sqrMagnitude > 0.01f ? planarFallback.normalized : Vector3.forward;
        }

        public Vector3 PredictEscapePoint(float distanceAhead)
        {
            return LastSeenPoint + ResolveDirection(Vector3.forward) * Mathf.Max(0.5f, distanceAhead);
        }

        public void SetSearchPlan(IReadOnlyList<MonsterSearchProbe> probes)
        {
            searchPlan.Clear();
            searchIndex = 0;

            if (probes == null)
            {
                return;
            }

            for (int index = 0; index < probes.Count; index++)
            {
                searchPlan.Add(probes[index]);
            }
        }

        public void ClearSearchPlan()
        {
            searchPlan.Clear();
            searchIndex = 0;
        }

        public bool TryGetCurrentProbe(out MonsterSearchProbe probe)
        {
            if (searchIndex < 0 || searchIndex >= searchPlan.Count)
            {
                probe = default;
                return false;
            }

            probe = searchPlan[searchIndex];
            return true;
        }

        public void AdvanceProbe()
        {
            if (searchIndex < searchPlan.Count)
            {
                searchIndex++;
            }
        }
    }
}
