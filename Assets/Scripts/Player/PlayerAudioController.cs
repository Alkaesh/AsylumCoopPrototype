using AsylumHorror.Monster;
using AsylumHorror.Audio;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(NetworkPlayerController))]
    public class PlayerAudioController : NetworkBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource footstepsSource;
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource breathingSource;
        [SerializeField] private AudioSource chaseMusicSource;

        [Header("Clips")]
        [SerializeField] private AudioClip[] walkFootsteps;
        [SerializeField] private AudioClip[] runFootsteps;
        [SerializeField] private AudioClip[] crouchFootsteps;

        [Header("Footsteps")]
        [SerializeField] private float walkStepInterval = 0.55f;
        [SerializeField] private float runStepInterval = 0.35f;
        [SerializeField] private float crouchStepInterval = 0.8f;
        [SerializeField] private float walkStepVolume = 0.42f;
        [SerializeField] private float runStepVolume = 0.55f;
        [SerializeField] private float crouchStepVolume = 0.14f;

        [Header("Fear Audio")]
        [SerializeField] private float heartbeatMaxDistance = 20f;
        [SerializeField] private float chaseMusicDistance = 16f;
        [SerializeField] private AudioClip traumaFumbleClip;

        private NetworkPlayerController playerController;
        private NetworkPlayerStatus playerStatus;
        private PlayerStressController stressController;
        private float nextStepTime;

        private void Awake()
        {
            playerController = GetComponent<NetworkPlayerController>();
            playerStatus = GetComponent<NetworkPlayerStatus>();
            stressController = GetComponent<PlayerStressController>();
            EnsureFallbackAudio();
        }

        private void OnEnable()
        {
            if (stressController == null)
            {
                stressController = GetComponent<PlayerStressController>();
            }

            if (stressController != null)
            {
                stressController.TraumaFumbled += OnTraumaFumbled;
            }
        }

        private void OnDisable()
        {
            if (stressController != null)
            {
                stressController.TraumaFumbled -= OnTraumaFumbled;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            UpdateFootsteps();
            UpdateFearAudio();
        }

        private void UpdateFootsteps()
        {
            if (playerStatus == null || !playerStatus.CanControlCharacter)
            {
                return;
            }

            if (Time.time < nextStepTime)
            {
                return;
            }

            PlayerLocomotionState state = playerController.LocomotionState;
            if (state == PlayerLocomotionState.Idle || playerController.LocalMoveMagnitude <= 0.1f)
            {
                return;
            }

            AudioClip[] pool;
            float interval;
            float volume;

            switch (state)
            {
                case PlayerLocomotionState.Run:
                    pool = runFootsteps;
                    interval = runStepInterval;
                    volume = runStepVolume;
                    break;
                case PlayerLocomotionState.Crouch:
                    pool = crouchFootsteps;
                    interval = crouchStepInterval;
                    volume = crouchStepVolume;
                    break;
                default:
                    pool = walkFootsteps;
                    interval = walkStepInterval;
                    volume = walkStepVolume;
                    break;
            }

            nextStepTime = Time.time + interval;
            if (footstepsSource == null || pool == null || pool.Length == 0)
            {
                return;
            }

            AudioClip selected = pool[Random.Range(0, pool.Length)];
            if (selected != null)
            {
                float move01 = Mathf.Clamp01(playerController.LocalMoveMagnitude);
                float dynamicVolume = Mathf.Lerp(volume * 0.74f, volume, move01);
                footstepsSource.PlayOneShot(selected, dynamicVolume);
            }
        }

        private void UpdateFearAudio()
        {
            MonsterAI monster = MonsterAI.Instance;
            float stress01 = stressController != null ? stressController.CurrentStress01 : 0f;
            float breathing01 = stressController != null ? stressController.BreathingStress01 : stress01;
            float proximity01 = 0f;
            float chase01 = 0f;

            if (monster != null)
            {
                float distance = Vector3.Distance(transform.position, monster.transform.position);
                proximity01 = Mathf.Clamp01(1f - distance / Mathf.Max(1f, heartbeatMaxDistance));
                chase01 = Mathf.Clamp01(1f - distance / Mathf.Max(1f, chaseMusicDistance));
            }

            float heartbeatVolume = Mathf.Clamp01(Mathf.Max(proximity01, stress01 * 0.92f));
            float breathingVolume = Mathf.Clamp01(Mathf.Max(proximity01 * 0.55f, breathing01) *
                                                  (playerStatus != null ? playerStatus.BreathingLoudnessMultiplier : 1f));
            float chaseVolume = monster != null &&
                                (monster.CurrentState == MonsterState.Chase || monster.CurrentState == MonsterState.Attack)
                ? Mathf.Clamp01(Mathf.Max(chase01 * 0.16f, stress01 * 0.08f))
                : 0f;

            SetFearVolumes(heartbeatVolume, breathingVolume, chaseVolume);
        }

        private void SetFearVolumes(float heartbeatVolume, float breathingVolume, float chaseVolume)
        {
            if (heartbeatSource != null)
            {
                heartbeatSource.volume = heartbeatVolume;
                if (heartbeatVolume > 0.02f && !heartbeatSource.isPlaying)
                {
                    heartbeatSource.Play();
                }
                else if (heartbeatVolume <= 0.02f && heartbeatSource.isPlaying)
                {
                    heartbeatSource.Stop();
                }
            }

            if (breathingSource != null)
            {
                breathingSource.volume = breathingVolume;
                breathingSource.pitch = Mathf.Lerp(0.95f, 1.18f, breathingVolume);
                if (breathingVolume > 0.02f && !breathingSource.isPlaying)
                {
                    breathingSource.Play();
                }
                else if (breathingVolume <= 0.02f && breathingSource.isPlaying)
                {
                    breathingSource.Stop();
                }
            }

            if (chaseMusicSource != null)
            {
                chaseMusicSource.volume = chaseVolume;
                chaseMusicSource.pitch = Mathf.Lerp(0.94f, 1.08f, chaseVolume);
                if (chaseVolume > 0.02f && !chaseMusicSource.isPlaying)
                {
                    chaseMusicSource.Play();
                }
                else if (chaseVolume <= 0.02f && chaseMusicSource.isPlaying)
                {
                    chaseMusicSource.Stop();
                }
            }
        }

        private void EnsureFallbackAudio()
        {
            if (walkFootsteps == null || walkFootsteps.Length == 0)
            {
                walkFootsteps = new[]
                {
                    ProceduralAudioFactory.GetFootstepClip("walk_a", 125f, 0.45f),
                    ProceduralAudioFactory.GetFootstepClip("walk_b", 110f, 0.45f)
                };
            }

            if (runFootsteps == null || runFootsteps.Length == 0)
            {
                runFootsteps = new[]
                {
                    ProceduralAudioFactory.GetFootstepClip("run_a", 145f, 0.56f),
                    ProceduralAudioFactory.GetFootstepClip("run_b", 160f, 0.56f)
                };
            }

            if (crouchFootsteps == null || crouchFootsteps.Length == 0)
            {
                crouchFootsteps = new[]
                {
                    ProceduralAudioFactory.GetFootstepClip("crouch_a", 88f, 0.28f),
                    ProceduralAudioFactory.GetFootstepClip("crouch_b", 96f, 0.28f)
                };
            }

            if (heartbeatSource != null && heartbeatSource.clip == null)
            {
                heartbeatSource.clip = ProceduralAudioFactory.GetHeartbeatLoop();
                heartbeatSource.loop = true;
            }

            if (breathingSource != null && breathingSource.clip == null)
            {
                breathingSource.clip = ProceduralAudioFactory.GetBreathingLoop();
                breathingSource.loop = true;
            }

            if (chaseMusicSource != null && chaseMusicSource.clip == null)
            {
                chaseMusicSource.clip = ProceduralAudioFactory.GetChaseLoop();
                chaseMusicSource.loop = true;
            }

            if (traumaFumbleClip == null)
            {
                traumaFumbleClip = ProceduralAudioFactory.GetDoorOpenClip();
            }
        }

        private void OnTraumaFumbled()
        {
            if (!isLocalPlayer || footstepsSource == null || traumaFumbleClip == null)
            {
                return;
            }

            footstepsSource.PlayOneShot(traumaFumbleClip, 0.42f);
        }
    }
}
