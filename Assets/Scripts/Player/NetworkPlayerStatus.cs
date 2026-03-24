using System;
using AsylumHorror.Core;
using AsylumHorror.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.Player
{
    public class NetworkPlayerStatus : NetworkBehaviour
    {
        [Header("Injury")]
        [SerializeField] private float injuredMoveMultiplier = 0.85f;
        [SerializeField] private float rescuedMoveMultiplier = 0.65f;
        [SerializeField] private float rescuedSlowDuration = 8f;

        [Header("Rescue Trauma")]
        [SerializeField] private float rescueTraumaDuration = 30f;
        [SerializeField] private float traumaNoiseBonus = 0.32f;
        [SerializeField] private float traumaInteractionBonus = 0.75f;
        [SerializeField] private float traumaFlashlightBonus = 0.7f;
        [SerializeField] private float traumaStressBonus = 0.55f;
        [SerializeField] private float traumaBreathingBonus = 0.65f;
        [SerializeField] private float traumaFumbleChance = 0.2f;

        [Header("Focus Ability")]
        [SerializeField] private float focusAbilityDuration = 5.5f;
        [SerializeField] private float focusAbilityCooldown = 24f;
        [SerializeField] private float focusNoiseMultiplier = 0.52f;
        [SerializeField] private float focusInteractionMultiplier = 0.82f;
        [SerializeField] private float focusFlashlightMultiplier = 0.55f;
        [SerializeField] private float focusStressMultiplier = 0.58f;
        [SerializeField] private float focusBreathingMultiplier = 0.7f;

        [Header("Hook")]
        [SerializeField] private float hookDeathDuration = 45f;
        [SerializeField] [Range(0f, 1f)] private float hookSelfEscapeChance = 0.25f;
        [SerializeField] private float hookSelfEscapeResolveDelay = 1.15f;

        [SyncVar(hook = nameof(OnConditionSyncChanged))] private PlayerCondition condition = PlayerCondition.Healthy;
        [SyncVar] private int hitsTaken;
        [SyncVar] private int timesHooked;
        [SyncVar] private int timesSavedAllies;
        [SyncVar] private float hookRemainingTime;
        [SyncVar] private uint carrierNetId;
        [SyncVar] private uint hookPointNetId;
        [SyncVar] private double rescuedSlowEndNetworkTime;
        [SyncVar] private double rescueTraumaEndNetworkTime;
        [SyncVar] private int rescueTraumaStacks;
        [SyncVar] private Quaternion carriedFacingRotation = Quaternion.identity;
        [SyncVar] private bool hidden;
        [SyncVar] private double focusAbilityEndsNetworkTime;
        [SyncVar] private double focusAbilityReadyNetworkTime;
        [SyncVar] private bool hookSelfEscapeAvailable;
        [SyncVar] private double hookSelfEscapeResolveNetworkTime;
        [SyncVar(hook = nameof(OnHookSelfEscapeOutcomeChanged))] private HookSelfEscapeOutcome hookSelfEscapeOutcome;

        public event Action<PlayerCondition> ConditionChanged;
        public event Action<HookSelfEscapeOutcome> HookSelfEscapeOutcomeChanged;

        public PlayerCondition Condition => condition;
        public float HookRemainingTime => hookRemainingTime;
        public uint CarrierNetId => carrierNetId;
        public uint HookPointNetId => hookPointNetId;
        public bool IsHidden => hidden;
        public bool HasRescueTrauma => rescueTraumaStacks > 0 && NetworkTime.time < rescueTraumaEndNetworkTime;
        public int RescueTraumaStacks => rescueTraumaStacks;
        public int TimesHooked => timesHooked;
        public int TimesSavedAllies => timesSavedAllies;
        public bool FocusAbilityActive => NetworkTime.time < focusAbilityEndsNetworkTime;
        public float FocusAbilityCooldownRemaining => Mathf.Max(0f, (float)(focusAbilityReadyNetworkTime - NetworkTime.time));
        public bool CanActivateFocusAbility =>
            (condition == PlayerCondition.Healthy || condition == PlayerCondition.Injured) &&
            NetworkTime.time >= focusAbilityReadyNetworkTime;
        public bool HookSelfEscapeAvailable => hookSelfEscapeAvailable;
        public bool HookSelfEscapeRolling =>
            condition == PlayerCondition.Hooked &&
            hookSelfEscapeOutcome == HookSelfEscapeOutcome.Pending &&
            NetworkTime.time < hookSelfEscapeResolveNetworkTime;
        public HookSelfEscapeOutcome CurrentHookSelfEscapeOutcome => hookSelfEscapeOutcome;
        public float HookSelfEscapeResolveRemaining =>
            HookSelfEscapeRolling ? Mathf.Max(0f, (float)(hookSelfEscapeResolveNetworkTime - NetworkTime.time)) : 0f;

        public bool CanUseInteraction =>
            hidden ||
            condition == PlayerCondition.Healthy ||
            condition == PlayerCondition.Injured;

        public bool CanControlCharacter =>
            !hidden &&
            (condition == PlayerCondition.Healthy || condition == PlayerCondition.Injured);
        public bool CanLookAround =>
            condition != PlayerCondition.Dead &&
            condition != PlayerCondition.Escaped;

        public bool IsMonsterTargetable =>
            !hidden &&
            (condition == PlayerCondition.Healthy ||
             condition == PlayerCondition.Injured);

        public float CurrentMovementMultiplier
        {
            get
            {
                if (condition == PlayerCondition.Healthy)
                {
                    return 1f;
                }

                if (condition == PlayerCondition.Injured)
                {
                    return NetworkTime.time < rescuedSlowEndNetworkTime
                        ? rescuedMoveMultiplier
                        : injuredMoveMultiplier;
                }

                return 0f;
            }
        }

        public float RescueTrauma01
        {
            get
            {
                if (!HasRescueTrauma || rescueTraumaDuration <= 0.01f)
                {
                    return 0f;
                }

                double remaining = rescueTraumaEndNetworkTime - NetworkTime.time;
                return Mathf.Clamp01((float)(remaining / rescueTraumaDuration));
            }
        }

        public float RescueTraumaStrength
        {
            get
            {
                if (!HasRescueTrauma)
                {
                    return 0f;
                }

                float timeWeighted = Mathf.Lerp(0.45f, 1f, RescueTrauma01);
                float stackBonus = 1f + Mathf.Max(0, rescueTraumaStacks - 1) * 0.32f;
                return timeWeighted * stackBonus;
            }
        }

        public float MovementNoiseMultiplier
        {
            get
            {
                float multiplier = 1f + traumaNoiseBonus * RescueTraumaStrength;
                if (FocusAbilityActive)
                {
                    multiplier *= focusNoiseMultiplier;
                }

                return multiplier;
            }
        }

        public float InteractionDurationMultiplier
        {
            get
            {
                float multiplier = 1f + traumaInteractionBonus * RescueTraumaStrength;
                if (FocusAbilityActive)
                {
                    multiplier *= focusInteractionMultiplier;
                }

                return multiplier;
            }
        }

        public float FlashlightInstabilityMultiplier
        {
            get
            {
                float multiplier = 1f + traumaFlashlightBonus * RescueTraumaStrength;
                if (FocusAbilityActive)
                {
                    multiplier *= focusFlashlightMultiplier;
                }

                return multiplier;
            }
        }

        public float StressIntensityMultiplier
        {
            get
            {
                float multiplier = 1f + traumaStressBonus * RescueTraumaStrength;
                if (FocusAbilityActive)
                {
                    multiplier *= focusStressMultiplier;
                }

                return multiplier;
            }
        }

        public float BreathingLoudnessMultiplier
        {
            get
            {
                float multiplier = 1f + traumaBreathingBonus * RescueTraumaStrength;
                if (FocusAbilityActive)
                {
                    multiplier *= focusBreathingMultiplier;
                }

                return multiplier;
            }
        }

        public float LooseItemFumbleChance => traumaFumbleChance * RescueTraumaStrength;

        [ServerCallback]
        private void Update()
        {
            if (rescueTraumaStacks > 0 && NetworkTime.time >= rescueTraumaEndNetworkTime)
            {
                rescueTraumaStacks = 0;
                rescueTraumaEndNetworkTime = 0;
            }

            if (condition == PlayerCondition.Hooked)
            {
                hookRemainingTime -= Time.deltaTime;
                if (hookRemainingTime <= 0f)
                {
                    ServerSetDead();
                }
            }

            if (condition == PlayerCondition.Hooked &&
                hookSelfEscapeOutcome == HookSelfEscapeOutcome.Pending &&
                NetworkTime.time >= hookSelfEscapeResolveNetworkTime)
            {
                ResolveHookSelfEscape();
            }

            if (condition == PlayerCondition.Carried && carrierNetId != 0 && !NetworkServer.spawned.ContainsKey(carrierNetId))
            {
                carrierNetId = 0;
                condition = PlayerCondition.Knocked;
            }
        }

        [Server]
        public void ServerResetForRound()
        {
            condition = PlayerCondition.Healthy;
            hitsTaken = 0;
            timesHooked = 0;
            timesSavedAllies = 0;
            hookRemainingTime = 0f;
            carrierNetId = 0;
            hookPointNetId = 0;
            rescuedSlowEndNetworkTime = 0;
            rescueTraumaEndNetworkTime = 0;
            rescueTraumaStacks = 0;
            carriedFacingRotation = transform.rotation;
            hidden = false;
            focusAbilityEndsNetworkTime = 0;
            focusAbilityReadyNetworkTime = 0;
            hookSelfEscapeAvailable = false;
            hookSelfEscapeResolveNetworkTime = 0;
            hookSelfEscapeOutcome = HookSelfEscapeOutcome.None;
        }

        [Server]
        public bool ServerApplyMonsterHit()
        {
            if (condition != PlayerCondition.Healthy && condition != PlayerCondition.Injured)
            {
                return false;
            }

            if (hitsTaken == 0)
            {
                hitsTaken = 1;
                condition = PlayerCondition.Injured;
                return false;
            }

            condition = PlayerCondition.Knocked;
            return true;
        }

        [Server]
        public bool ServerSetCarried(NetworkIdentity monsterIdentity)
        {
            if (condition != PlayerCondition.Knocked || monsterIdentity == null)
            {
                return false;
            }

            carrierNetId = monsterIdentity.netId;
            hookPointNetId = 0;
            carriedFacingRotation = transform.rotation;
            hidden = false;
            condition = PlayerCondition.Carried;
            return true;
        }

        [Server]
        public void ServerSetCarriedPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (condition != PlayerCondition.Carried)
            {
                return;
            }

            carriedFacingRotation = worldRotation;
            transform.SetPositionAndRotation(worldPosition, carriedFacingRotation);
            TargetForcePosition(connectionToClient, worldPosition, carriedFacingRotation);
        }

        [Server]
        public void ServerSetHooked(HookPoint hookPoint, Vector3 worldPosition)
        {
            if (hookPoint == null)
            {
                return;
            }

            carrierNetId = 0;
            hookPointNetId = hookPoint.netId;
            hidden = false;
            condition = PlayerCondition.Hooked;
            timesHooked++;
            hookRemainingTime = hookDeathDuration;
            hookSelfEscapeAvailable = true;
            hookSelfEscapeResolveNetworkTime = 0;
            hookSelfEscapeOutcome = HookSelfEscapeOutcome.None;
            transform.SetPositionAndRotation(worldPosition, hookPoint.transform.rotation);
            TargetForcePosition(connectionToClient, worldPosition, hookPoint.transform.rotation);
        }

        [Server]
        public void ServerRescueFromHook(HookPoint hookPoint)
        {
            if (condition != PlayerCondition.Hooked || hookPoint == null)
            {
                return;
            }

            hookPointNetId = 0;
            hookRemainingTime = 0f;
            hitsTaken = 1;
            hidden = false;
            rescuedSlowEndNetworkTime = NetworkTime.time + rescuedSlowDuration;
            ApplyRescueTrauma();
            condition = PlayerCondition.Injured;

            Vector3 rescueOffset = hookPoint.transform.right * 1.5f;
            Vector3 rescuePosition = hookPoint.transform.position + rescueOffset;
            if (NavMesh.SamplePosition(rescuePosition, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                rescuePosition = hit.position;
            }

            transform.SetPositionAndRotation(rescuePosition, hookPoint.transform.rotation);
            TargetForcePosition(connectionToClient, rescuePosition, hookPoint.transform.rotation);
            GameStateManager.Instance?.ServerEvaluateRoundState();
            ClearHookSelfEscapeState();
        }

        [Server]
        public void ServerDropFromCarry(Vector3 dropPosition)
        {
            if (condition != PlayerCondition.Carried)
            {
                return;
            }

            carrierNetId = 0;
            hidden = false;
            condition = PlayerCondition.Knocked;
            transform.position = dropPosition;
            TargetForcePosition(connectionToClient, transform.position, transform.rotation);
            ClearHookSelfEscapeState();
        }

        [Server]
        public bool ServerReviveFromKnocked(Vector3 revivePosition)
        {
            if (condition != PlayerCondition.Knocked)
            {
                return false;
            }

            hitsTaken = 1;
            rescuedSlowEndNetworkTime = NetworkTime.time + rescuedSlowDuration;
            ApplyRescueTrauma();
            hidden = false;
            condition = PlayerCondition.Injured;
            transform.position = revivePosition;
            TargetForcePosition(connectionToClient, transform.position, transform.rotation);
            ClearHookSelfEscapeState();
            return true;
        }

        [Server]
        public void ServerSetHidden(bool hiddenState, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (condition != PlayerCondition.Healthy && condition != PlayerCondition.Injured)
            {
                return;
            }

            hidden = hiddenState;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            TargetForcePosition(connectionToClient, worldPosition, worldRotation);
        }

        [Server]
        public void ServerSetEscaped()
        {
            if (condition == PlayerCondition.Dead)
            {
                return;
            }

            condition = PlayerCondition.Escaped;
            hookRemainingTime = 0f;
            carrierNetId = 0;
            hookPointNetId = 0;
            rescueTraumaEndNetworkTime = 0;
            rescueTraumaStacks = 0;
            hidden = false;
            ClearHookSelfEscapeState();
        }

        [Server]
        public void ServerSetDead()
        {
            if (condition == PlayerCondition.Dead)
            {
                return;
            }

            if (hookPointNetId != 0 &&
                NetworkServer.spawned.TryGetValue(hookPointNetId, out NetworkIdentity hookIdentity) &&
                hookIdentity.TryGetComponent(out HookPoint hookPoint))
            {
                hookPoint.ServerClearIfOccupant(this);
            }

            condition = PlayerCondition.Dead;
            hookRemainingTime = 0f;
            carrierNetId = 0;
            hookPointNetId = 0;
            rescueTraumaEndNetworkTime = 0;
            rescueTraumaStacks = 0;
            hidden = false;
            ClearHookSelfEscapeState();
            GameStateManager.Instance?.ServerEvaluateRoundState();
        }

        [Server]
        public void ServerRegisterSave()
        {
            timesSavedAllies++;
        }

        [Server]
        private void ApplyRescueTrauma()
        {
            rescueTraumaStacks = Mathf.Clamp(rescueTraumaStacks + 1, 0, 2);
            rescueTraumaEndNetworkTime = NetworkTime.time + rescueTraumaDuration;
        }

        [Server]
        public bool ServerActivateFocusAbility()
        {
            if (!CanActivateFocusAbility)
            {
                return false;
            }

            focusAbilityEndsNetworkTime = NetworkTime.time + focusAbilityDuration;
            focusAbilityReadyNetworkTime = NetworkTime.time + focusAbilityCooldown;
            return true;
        }

        [Command]
        public void CmdAttemptHookSelfEscape()
        {
            if (condition != PlayerCondition.Hooked ||
                !hookSelfEscapeAvailable ||
                hookSelfEscapeOutcome == HookSelfEscapeOutcome.Pending)
            {
                return;
            }

            hookSelfEscapeAvailable = false;
            hookSelfEscapeOutcome = HookSelfEscapeOutcome.Pending;
            hookSelfEscapeResolveNetworkTime = NetworkTime.time + hookSelfEscapeResolveDelay;
        }

        [Server]
        private void ResolveHookSelfEscape()
        {
            if (condition != PlayerCondition.Hooked ||
                hookSelfEscapeOutcome != HookSelfEscapeOutcome.Pending)
            {
                return;
            }

            hookSelfEscapeResolveNetworkTime = 0;
            bool escaped = UnityEngine.Random.value <= hookSelfEscapeChance;
            if (escaped &&
                hookPointNetId != 0 &&
                NetworkServer.spawned.TryGetValue(hookPointNetId, out NetworkIdentity hookIdentity) &&
                hookIdentity.TryGetComponent(out HookPoint hookPoint))
            {
                hookSelfEscapeOutcome = HookSelfEscapeOutcome.Success;
                hookPoint.ServerClearIfOccupant(this);
                ServerRescueFromHook(hookPoint);
                return;
            }

            hookSelfEscapeOutcome = HookSelfEscapeOutcome.Failed;
        }

        [Server]
        private void ClearHookSelfEscapeState()
        {
            hookSelfEscapeAvailable = false;
            hookSelfEscapeResolveNetworkTime = 0;
            hookSelfEscapeOutcome = HookSelfEscapeOutcome.None;
        }

        [TargetRpc]
        private void TargetForcePosition(NetworkConnection _, Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        private void OnConditionSyncChanged(PlayerCondition _, PlayerCondition nextCondition)
        {
            ConditionChanged?.Invoke(nextCondition);
        }

        private void OnHookSelfEscapeOutcomeChanged(HookSelfEscapeOutcome _, HookSelfEscapeOutcome nextOutcome)
        {
            HookSelfEscapeOutcomeChanged?.Invoke(nextOutcome);
        }
    }
}
