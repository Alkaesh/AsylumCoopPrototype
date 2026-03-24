using System.Collections;
using System.Collections.Generic;
using AsylumHorror.Core;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace AsylumHorror.World
{
    public class FalsePresenceDirector : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 eventIntervalRange = new Vector2(16f, 28f);
        [SerializeField] private Vector2 openingIntervalRange = new Vector2(28f, 42f);
        [SerializeField] private float openingQuietDuration = 55f;
        [SerializeField] private float dangerRecoveryDuration = 10f;
        [SerializeField] private float minMonsterDistance = 18f;
        [SerializeField] private float minPlayerDistance = 5f;
        [SerializeField] private float maxPlayerDistance = 24f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] distantFootstepClips;
        [SerializeField] private AudioClip[] metallicPresenceClips;
        [SerializeField] private float falseStepVolume = 0.82f;
        [SerializeField] private float hideoutNoiseVolume = 0.7f;

        [Header("Visual")]
        [SerializeField] private float silhouetteDuration = 0.85f;
        [SerializeField] private float shadowCrossDuration = 0.72f;
        [SerializeField] private float flashIntensityMultiplier = 2.1f;

        private readonly List<FalsePresenceAnchor> anchors = new List<FalsePresenceAnchor>();
        private NetworkPlayerController localPlayer;
        private MonsterAI monster;
        private float nextEventAt;
        private float lastDangerAt = -999f;
        private float lastVisualEventAt = -999f;
        private FalsePresenceAnchor lastAnchor;
        private Camera localCamera;

        private void Start()
        {
            CacheAnchors();
            ScheduleNextEvent();
        }

        private void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsRoundOver)
            {
                return;
            }

            if (localPlayer == null || !localPlayer.isLocalPlayer)
            {
                localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    localCamera = localPlayer.GetComponentInChildren<Camera>(true);
                }
            }

            if (monster == null)
            {
                monster = MonsterAI.Instance;
            }

            if (localPlayer == null || monster == null || Time.time < nextEventAt)
            {
                return;
            }

            if (monster.CurrentState == MonsterState.Chase ||
                monster.CurrentState == MonsterState.Attack ||
                monster.CurrentState == MonsterState.Carry)
            {
                lastDangerAt = Time.time;
            }

            NetworkPlayerStatus status = localPlayer.GetComponent<NetworkPlayerStatus>();
            if (status == null ||
                !status.CanLookAround ||
                status.Condition == PlayerCondition.Hooked ||
                status.Condition == PlayerCondition.Carried ||
                status.Condition == PlayerCondition.Knocked ||
                status.Condition == PlayerCondition.Dead ||
                status.Condition == PlayerCondition.Escaped)
            {
                ScheduleNextEvent();
                return;
            }

            if (monster.CurrentState == MonsterState.Chase || monster.CurrentState == MonsterState.Attack)
            {
                ScheduleNextEvent(1.35f);
                return;
            }

            if (Time.time - lastDangerAt < dangerRecoveryDuration)
            {
                ScheduleNextEvent(1.15f);
                return;
            }

            if (Vector3.Distance(localPlayer.transform.position, monster.transform.position) < minMonsterDistance)
            {
                ScheduleNextEvent(0.6f);
                return;
            }

            FalsePresenceAnchor anchor = PickAnchor();
            if (anchor == null)
            {
                ScheduleNextEvent(0.5f);
                return;
            }

            lastAnchor = anchor;
            TriggerAnchor(anchor);
            ScheduleNextEvent();
        }

        private void CacheAnchors()
        {
            anchors.Clear();
            anchors.AddRange(FindObjectsByType<FalsePresenceAnchor>());
        }

        private FalsePresenceAnchor PickAnchor()
        {
            if (localPlayer == null)
            {
                return null;
            }

            GameStateManager gameState = GameStateManager.Instance;
            RoundObjectivePhase phase = gameState != null
                ? gameState.CurrentObjectivePhase
                : RoundObjectivePhase.RestoreAuxiliaryPower;
            RoundRouteKind routeKind = gameState != null
                ? gameState.ActiveRouteKind
                : RoundRouteKind.CrossCurrent;
            float roundElapsed = gameState != null ? gameState.RoundElapsedSeconds : 0f;
            List<FalsePresenceAnchor> candidates = new List<FalsePresenceAnchor>();
            foreach (FalsePresenceAnchor anchor in anchors)
            {
                if (anchor == null)
                {
                    continue;
                }

                float playerDistance = Vector3.Distance(localPlayer.transform.position, anchor.transform.position);
                if (playerDistance < minPlayerDistance || playerDistance > maxPlayerDistance)
                {
                    continue;
                }

                if (monster != null && Vector3.Distance(monster.transform.position, anchor.transform.position) < minMonsterDistance)
                {
                    continue;
                }

                if (!IsAnchorEventAllowed(anchor.EventType, phase, roundElapsed))
                {
                    continue;
                }

                if (IsVisualEvent(anchor.EventType) && Time.time - lastVisualEventAt < 34f)
                {
                    continue;
                }

                if (!HasPresentationLine(anchor))
                {
                    continue;
                }

                if (lastAnchor != null && anchor == lastAnchor && Random.value < 0.85f)
                {
                    continue;
                }

                candidates.Add(anchor);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort((a, b) =>
            {
                float scoreA = ScoreAnchor(a, routeKind, phase);
                float scoreB = ScoreAnchor(b, routeKind, phase);
                return scoreA.CompareTo(scoreB);
            });

            int pickIndex = Mathf.Min(candidates.Count - 1, Random.Range(0, Mathf.Min(3, candidates.Count)));
            return candidates[pickIndex];
        }

        private void TriggerAnchor(FalsePresenceAnchor anchor)
        {
            if (IsVisualEvent(anchor.EventType))
            {
                lastVisualEventAt = Time.time;
            }

            switch (anchor.EventType)
            {
                case FalsePresenceEventType.DistantFootsteps:
                    StartCoroutine(PlayDistantFootsteps(anchor.transform.position));
                    break;
                case FalsePresenceEventType.FlashSilhouette:
                    StartCoroutine(PlayFlashSilhouette(anchor.transform, silhouetteDuration, anchor.LinkedLight));
                    break;
                case FalsePresenceEventType.DoorwayShadow:
                    StartCoroutine(PlayDoorwayShadow(anchor.transform, anchor.SecondaryPoint));
                    break;
                case FalsePresenceEventType.HideoutNoise:
                    PlayOneShotAt(anchor.transform.position, metallicPresenceClips, hideoutNoiseVolume);
                    break;
            }
        }

        private IEnumerator PlayDistantFootsteps(Vector3 origin)
        {
            int steps = Random.Range(2, 5);
            for (int index = 0; index < steps; index++)
            {
                PlayOneShotAt(origin + Random.insideUnitSphere * 0.35f, distantFootstepClips, falseStepVolume);
                yield return new WaitForSeconds(Random.Range(0.32f, 0.56f));
            }
        }

        private IEnumerator PlayFlashSilhouette(Transform anchor, float duration, Light linkedLight)
        {
            yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
            PlayOneShotAt(anchor.position + anchor.forward * 0.4f, metallicPresenceClips, hideoutNoiseVolume * 0.42f);
            yield return new WaitForSeconds(Random.Range(0.08f, 0.16f));

            GameObject silhouette = BossApparitionFactory.Create(anchor.position, anchor.rotation, BossApparitionStyle.FlashReveal);
            if (silhouette == null)
            {
                yield return PulseLight(linkedLight, duration * 0.8f, flashIntensityMultiplier);
                yield break;
            }

            BossApparitionProxy proxy = silhouette.GetComponent<BossApparitionProxy>();
            proxy?.SetOpacity(0.74f);
            yield return StartCoroutine(PulseLight(linkedLight, duration * 0.8f, flashIntensityMultiplier));
            yield return FadeAndDestroyApparition(silhouette, proxy, duration);
        }

        private IEnumerator PlayDoorwayShadow(Transform anchor, Transform target)
        {
            PlayOneShotAt(anchor.position, distantFootstepClips, falseStepVolume * 0.45f);
            yield return new WaitForSeconds(Random.Range(0.12f, 0.24f));

            GameObject silhouette = BossApparitionFactory.Create(anchor.position, anchor.rotation, BossApparitionStyle.DoorwayShadow);
            if (silhouette == null)
            {
                yield break;
            }

            BossApparitionProxy proxy = silhouette.GetComponent<BossApparitionProxy>();
            Vector3 start = anchor.position;
            Vector3 end = target != null ? target.position : anchor.position + anchor.right * 2f;
            Quaternion endRotation = target != null ? target.rotation : anchor.rotation;
            float endTime = Time.time + shadowCrossDuration;

            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / shadowCrossDuration);
                silhouette.transform.SetPositionAndRotation(
                    Vector3.Lerp(start, end, t),
                    Quaternion.Slerp(anchor.rotation, endRotation, t));
                proxy?.SetOpacity(Mathf.Lerp(0.65f, 0.12f, t));
                yield return null;
            }

            Object.Destroy(silhouette);
        }

        private IEnumerator FadeAndDestroyApparition(GameObject silhouette, BossApparitionProxy proxy, float duration)
        {
            if (silhouette == null)
            {
                yield break;
            }

            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                proxy?.SetOpacity(Mathf.Lerp(0.72f, 0f, t));
                yield return null;
            }

            Object.Destroy(silhouette);
        }

        private IEnumerator PulseLight(Light linkedLight, float duration, float intensityMultiplier)
        {
            if (linkedLight == null)
            {
                yield break;
            }

            float baseIntensity = linkedLight.intensity;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                linkedLight.intensity = Mathf.Lerp(baseIntensity, baseIntensity * intensityMultiplier, Mathf.PingPong(t * 1.8f, 1f));
                yield return null;
            }

            linkedLight.intensity = baseIntensity;
        }

        private void PlayOneShotAt(Vector3 position, AudioClip[] pool, float volume)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            AudioClip clip = pool[Random.Range(0, pool.Length)];
            if (clip == null)
            {
                return;
            }

            GameObject audioObject = new GameObject("FalsePresenceAudio");
            audioObject.transform.position = position;
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.minDistance = 3f;
            source.maxDistance = 26f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.playOnAwake = false;
            source.volume = volume;
            source.clip = clip;
            source.Play();
            Destroy(audioObject, clip.length + 0.2f);
        }

        private bool HasPresentationLine(FalsePresenceAnchor anchor)
        {
            if (anchor == null || localPlayer == null || localCamera == null)
            {
                return true;
            }

            if (anchor.EventType != FalsePresenceEventType.FlashSilhouette &&
                anchor.EventType != FalsePresenceEventType.DoorwayShadow)
            {
                return true;
            }

            Vector3 cameraPosition = localCamera.transform.position;
            Vector3 lookDirection = (anchor.transform.position - cameraPosition).normalized;
            if (Vector3.Dot(localCamera.transform.forward, lookDirection) < -0.15f)
            {
                return false;
            }

            Vector3 targetPoint = anchor.transform.position + Vector3.up * 1.45f;
            if (Physics.Linecast(cameraPosition, targetPoint, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(anchor.transform) &&
                    !hit.transform.IsChildOf(localPlayer.transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void ScheduleNextEvent(float multiplier = 1f)
        {
            GameStateManager gameState = GameStateManager.Instance;
            bool inQuietOpening = gameState == null || gameState.RoundElapsedSeconds < openingQuietDuration;
            Vector2 intervalRange = inQuietOpening ? openingIntervalRange : eventIntervalRange;
            float min = Mathf.Min(intervalRange.x, intervalRange.y);
            float max = Mathf.Max(intervalRange.x, intervalRange.y);

            if (gameState != null && gameState.CurrentObjectivePhase == RoundObjectivePhase.Escape)
            {
                min *= 0.82f;
                max *= 0.82f;
            }

            nextEventAt = Time.time + Random.Range(min, max) * Mathf.Max(0.15f, multiplier);
        }

        private float ScoreAnchor(FalsePresenceAnchor anchor, RoundRouteKind routeKind, RoundObjectivePhase phase)
        {
            float distanceScore = Mathf.Abs(
                Vector3.Distance(localPlayer.transform.position, anchor.transform.position) -
                anchor.PreferredPlayerDistance);
            float score = distanceScore + Random.Range(0f, 0.6f);
            score -= ResolveRouteAffinity(anchor.name, routeKind);
            score -= ResolvePhaseAffinity(anchor.name, phase);
            score += ResolveEventPenalty(anchor.EventType, phase);
            return score;
        }

        private static bool IsAnchorEventAllowed(FalsePresenceEventType eventType, RoundObjectivePhase phase, float roundElapsed)
        {
            switch (phase)
            {
                case RoundObjectivePhase.RestoreAuxiliaryPower:
                    if (roundElapsed < 80f)
                    {
                        return eventType == FalsePresenceEventType.DistantFootsteps ||
                               eventType == FalsePresenceEventType.HideoutNoise;
                    }

                    return eventType != FalsePresenceEventType.FlashSilhouette;
                case RoundObjectivePhase.FindAccessKey:
                case RoundObjectivePhase.RestoreMainPower:
                    return true;
                case RoundObjectivePhase.Escape:
                    return true;
                default:
                    return eventType != FalsePresenceEventType.FlashSilhouette;
            }
        }

        private static float ResolveEventPenalty(FalsePresenceEventType eventType, RoundObjectivePhase phase)
        {
            return phase switch
            {
                RoundObjectivePhase.RestoreAuxiliaryPower when eventType == FalsePresenceEventType.FlashSilhouette => 2.4f,
                RoundObjectivePhase.RestoreAuxiliaryPower when eventType == FalsePresenceEventType.DoorwayShadow => 0.9f,
                RoundObjectivePhase.FindAccessKey when eventType == FalsePresenceEventType.DistantFootsteps => -0.2f,
                RoundObjectivePhase.RestoreMainPower when eventType == FalsePresenceEventType.DoorwayShadow => -0.3f,
                RoundObjectivePhase.Escape when eventType == FalsePresenceEventType.FlashSilhouette => -0.45f,
                _ => 0f
            };
        }

        private static float ResolveRouteAffinity(string anchorName, RoundRouteKind routeKind)
        {
            switch (routeKind)
            {
                case RoundRouteKind.WestDescent:
                    if (ContainsAny(anchorName, "Security", "Archive", "Service"))
                    {
                        return 1.6f;
                    }

                    break;
                case RoundRouteKind.EastDescent:
                    if (ContainsAny(anchorName, "Security", "Operation", "Server"))
                    {
                        return 1.6f;
                    }

                    break;
                case RoundRouteKind.CrossCurrent:
                    if (ContainsAny(anchorName, "Security", "Cross", "Morgue"))
                    {
                        return 1.6f;
                    }

                    break;
            }

            return 0f;
        }

        private static float ResolvePhaseAffinity(string anchorName, RoundObjectivePhase phase)
        {
            switch (phase)
            {
                case RoundObjectivePhase.RestoreAuxiliaryPower:
                    return ContainsAny(anchorName, "Security", "Archive", "Operation") ? 0.7f : 0f;
                case RoundObjectivePhase.FindAccessKey:
                    return ContainsAny(anchorName, "Archive", "Operation", "Cross") ? 0.55f : 0f;
                case RoundObjectivePhase.RestoreMainPower:
                    return ContainsAny(anchorName, "Service", "Server", "Cross") ? 0.85f : 0f;
                case RoundObjectivePhase.Escape:
                    return ContainsAny(anchorName, "Cross", "Morgue", "Security") ? 0.9f : 0f;
                default:
                    return 0f;
            }
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value) || tokens == null)
            {
                return false;
            }

            foreach (string token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token) &&
                    value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsVisualEvent(FalsePresenceEventType eventType)
        {
            return eventType == FalsePresenceEventType.FlashSilhouette ||
                   eventType == FalsePresenceEventType.DoorwayShadow;
        }

        private static NetworkPlayerController FindLocalPlayer()
        {
            foreach (NetworkPlayerController controller in FindObjectsByType<NetworkPlayerController>())
            {
                if (controller != null && controller.isLocalPlayer)
                {
                    return controller;
                }
            }

            return null;
        }
    }
}
