using System.Collections;
using System.Collections.Generic;
using AsylumHorror.Core;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using UnityEngine;

namespace AsylumHorror.World
{
    public class FalsePresenceDirector : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private Vector2 eventIntervalRange = new Vector2(16f, 28f);
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
        private FalsePresenceAnchor lastAnchor;
        private Material silhouetteMaterial;

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
            }

            if (monster == null)
            {
                monster = MonsterAI.Instance;
            }

            if (localPlayer == null || monster == null || Time.time < nextEventAt)
            {
                return;
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
                ScheduleNextEvent(0.5f);
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
            anchors.AddRange(FindObjectsByType<FalsePresenceAnchor>(FindObjectsSortMode.None));
        }

        private FalsePresenceAnchor PickAnchor()
        {
            if (localPlayer == null)
            {
                return null;
            }

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
                float scoreA = Mathf.Abs(Vector3.Distance(localPlayer.transform.position, a.transform.position) - a.PreferredPlayerDistance);
                float scoreB = Mathf.Abs(Vector3.Distance(localPlayer.transform.position, b.transform.position) - b.PreferredPlayerDistance);
                return scoreA.CompareTo(scoreB);
            });

            int pickIndex = Mathf.Min(candidates.Count - 1, Random.Range(0, Mathf.Min(3, candidates.Count)));
            return candidates[pickIndex];
        }

        private void TriggerAnchor(FalsePresenceAnchor anchor)
        {
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
            GameObject silhouette = CreateSilhouette(anchor.position, anchor.rotation);
            silhouette.SetActive(true);
            yield return StartCoroutine(PulseLight(linkedLight, duration * 0.8f, flashIntensityMultiplier));
            yield return FadeAndDestroySilhouette(silhouette, duration);
        }

        private IEnumerator PlayDoorwayShadow(Transform anchor, Transform target)
        {
            GameObject silhouette = CreateSilhouette(anchor.position, anchor.rotation);
            Vector3 start = anchor.position;
            Vector3 end = target != null ? target.position : anchor.position + anchor.right * 2f;
            Quaternion endRotation = target != null ? target.rotation : anchor.rotation;
            float endTime = Time.time + shadowCrossDuration;
            Renderer[] renderers = silhouette.GetComponentsInChildren<Renderer>();

            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / shadowCrossDuration);
                silhouette.transform.SetPositionAndRotation(
                    Vector3.Lerp(start, end, t),
                    Quaternion.Slerp(anchor.rotation, endRotation, t));
                SetSilhouetteAlpha(renderers, Mathf.Lerp(0.65f, 0.15f, t));
                yield return null;
            }

            Object.Destroy(silhouette);
        }

        private IEnumerator FadeAndDestroySilhouette(GameObject silhouette, float duration)
        {
            if (silhouette == null)
            {
                yield break;
            }

            Renderer[] renderers = silhouette.GetComponentsInChildren<Renderer>();
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                SetSilhouetteAlpha(renderers, Mathf.Lerp(0.72f, 0f, t));
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

        private GameObject CreateSilhouette(Vector3 position, Quaternion rotation)
        {
            if (silhouetteMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                silhouetteMaterial = new Material(shader);
                silhouetteMaterial.color = new Color(0.02f, 0.02f, 0.025f, 0.72f);
            }

            GameObject root = new GameObject("FalsePresenceSilhouette");
            root.transform.SetPositionAndRotation(position, rotation);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            torso.transform.localScale = new Vector3(0.72f, 1.15f, 0.72f);
            Object.Destroy(torso.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.12f, 0.04f);
            head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            Object.Destroy(head.GetComponent<Collider>());

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = silhouetteMaterial;
                ShadowCastingModeOff(renderer);
            }

            return root;
        }

        private void SetSilhouetteAlpha(Renderer[] renderers, float alpha)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.material == null)
                {
                    continue;
                }

                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }

        private static void ShadowCastingModeOff(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void ScheduleNextEvent(float multiplier = 1f)
        {
            float min = Mathf.Min(eventIntervalRange.x, eventIntervalRange.y);
            float max = Mathf.Max(eventIntervalRange.x, eventIntervalRange.y);
            nextEventAt = Time.time + Random.Range(min, max) * Mathf.Max(0.15f, multiplier);
        }

        private static NetworkPlayerController FindLocalPlayer()
        {
            foreach (NetworkPlayerController controller in FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None))
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
