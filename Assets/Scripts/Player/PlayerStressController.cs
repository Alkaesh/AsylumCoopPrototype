using System;
using AsylumHorror.Monster;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(NetworkPlayerStatus))]
    [RequireComponent(typeof(NetworkPlayerController))]
    public class PlayerStressController : NetworkBehaviour
    {
        [Header("Stress")]
        [SerializeField] private float monsterSenseDistance = 26f;
        [SerializeField] private float chasePressureDistance = 18f;
        [SerializeField] private float stressBuildSpeed = 1.55f;
        [SerializeField] private float stressRecoverySpeed = 0.85f;
        [SerializeField] private float hiddenStressRelief = 0.18f;

        [Header("Reveal Flash")]
        [SerializeField] private float revealDistance = 17f;
        [SerializeField] private float revealDuration = 1.05f;
        [SerializeField] private Vector2 revealCooldownRange = new Vector2(11f, 18f);
        [SerializeField] private float revealMinStress = 0.42f;

        [Header("Trauma Fumble")]
        [SerializeField] private float fumbleCheckInterval = 1.8f;
        [SerializeField] private float fumbleNoiseRadius = 6.5f;

        private NetworkPlayerStatus playerStatus;
        private NetworkPlayerController playerController;
        private float currentStress;
        private float revealTimer;
        private float revealFlash01;
        private float nextRevealAt;
        private float nextFumbleCheckAt;

        public event Action TraumaFumbled;

        public float CurrentStress01 => currentStress;
        public float RevealFlash01 => revealFlash01;
        public float FlashlightInstability01 =>
            Mathf.Clamp01(currentStress * 0.7f + (playerStatus != null ? playerStatus.RescueTraumaStrength * 0.4f : 0f));
        public float BreathingStress01 =>
            Mathf.Clamp01(currentStress * 0.72f + (playerStatus != null ? playerStatus.RescueTraumaStrength * 0.34f : 0f));

        private void Awake()
        {
            playerStatus = GetComponent<NetworkPlayerStatus>();
            playerController = GetComponent<NetworkPlayerController>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            ScheduleNextReveal();
            nextFumbleCheckAt = Time.time + UnityEngine.Random.Range(0.8f, 1.4f);
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            MonsterAI monster = MonsterAI.Instance;
            float targetStress = EvaluateTargetStress(monster);
            float blendSpeed = targetStress > currentStress ? stressBuildSpeed : stressRecoverySpeed;
            currentStress = Mathf.MoveTowards(currentStress, targetStress, blendSpeed * Time.deltaTime);

            UpdateReveal(monster);
            UpdateTraumaFumble(monster);
        }

        private float EvaluateTargetStress(MonsterAI monster)
        {
            if (playerStatus == null)
            {
                return 0f;
            }

            float stress = 0f;
            if (playerStatus.Condition == PlayerCondition.Injured)
            {
                stress += 0.08f;
            }

            if (playerStatus.HasRescueTrauma)
            {
                stress += 0.16f * playerStatus.StressIntensityMultiplier;
            }

            if (monster != null)
            {
                float distance = Vector3.Distance(transform.position, monster.transform.position);
                float proximity01 = Mathf.Clamp01(1f - distance / Mathf.Max(1f, monsterSenseDistance));
                stress += proximity01 * 0.58f;

                switch (monster.CurrentState)
                {
                    case MonsterState.Chase:
                        stress += Mathf.Lerp(0.2f, 0.48f, Mathf.Clamp01(1f - distance / Mathf.Max(1f, chasePressureDistance)));
                        break;
                    case MonsterState.Attack:
                    case MonsterState.Carry:
                        stress += 0.42f;
                        break;
                    case MonsterState.Search:
                    case MonsterState.InvestigateSound:
                        stress += proximity01 * 0.18f;
                        break;
                }
            }

            stress *= playerStatus.StressIntensityMultiplier;

            if (playerStatus.IsHidden)
            {
                stress = Mathf.Max(0.07f, stress - hiddenStressRelief);
            }

            if (playerController != null && playerController.LocomotionState == PlayerLocomotionState.Crouch)
            {
                stress *= 0.94f;
            }

            return Mathf.Clamp01(stress);
        }

        private void UpdateReveal(MonsterAI monster)
        {
            if (monster == null)
            {
                revealTimer = 0f;
                revealFlash01 = Mathf.MoveTowards(revealFlash01, 0f, Time.deltaTime * 5f);
                return;
            }

            bool canTrigger = !playerStatus.IsHidden &&
                              currentStress >= revealMinStress &&
                              Vector3.Distance(transform.position, monster.transform.position) <= revealDistance;

            if (revealTimer <= 0f && canTrigger && Time.time >= nextRevealAt)
            {
                revealTimer = revealDuration;
                ScheduleNextReveal();
            }

            if (revealTimer > 0f)
            {
                revealTimer -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(revealTimer / Mathf.Max(0.01f, revealDuration));
                revealFlash01 = progress < 0.24f
                    ? Mathf.Lerp(0f, 1f, progress / 0.24f)
                    : Mathf.Lerp(1f, 0f, (progress - 0.24f) / 0.76f);
            }
            else
            {
                revealFlash01 = Mathf.MoveTowards(revealFlash01, 0f, Time.deltaTime * 6f);
            }

            monster.SetClientRevealBoost(revealFlash01);
        }

        private void UpdateTraumaFumble(MonsterAI monster)
        {
            if (playerStatus == null ||
                !playerStatus.HasRescueTrauma ||
                playerStatus.IsHidden ||
                Time.time < nextFumbleCheckAt)
            {
                return;
            }

            nextFumbleCheckAt = Time.time + fumbleCheckInterval * UnityEngine.Random.Range(0.85f, 1.25f);

            float moveFactor = playerController != null ? playerController.LocalMoveMagnitude : 0f;
            if (moveFactor <= 0.15f && currentStress < 0.58f)
            {
                return;
            }

            float chance = playerStatus.LooseItemFumbleChance *
                           (0.6f + currentStress * 0.72f + moveFactor * 0.4f);

            if (monster != null && Vector3.Distance(transform.position, monster.transform.position) <= 10f)
            {
                chance += 0.08f;
            }

            if (UnityEngine.Random.value > chance)
            {
                return;
            }

            currentStress = Mathf.Clamp01(currentStress + 0.1f);
            TraumaFumbled?.Invoke();
            CmdEmitTraumaFumbleNoise(fumbleNoiseRadius * Mathf.Lerp(0.95f, 1.25f, currentStress));
        }

        [Command]
        private void CmdEmitTraumaFumbleNoise(float radius)
        {
            NoiseSystem.Emit(transform.position, radius, 1.35f, NoiseCategory.PlayerMovement);
        }

        private void ScheduleNextReveal()
        {
            float min = Mathf.Min(revealCooldownRange.x, revealCooldownRange.y);
            float max = Mathf.Max(revealCooldownRange.x, revealCooldownRange.y);
            nextRevealAt = Time.time + UnityEngine.Random.Range(min, max);
        }
    }
}
