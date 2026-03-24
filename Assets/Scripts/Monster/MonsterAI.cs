using System;
using System.Collections.Generic;
using System.Linq;
using AsylumHorror.Audio;
using AsylumHorror.Player;
using AsylumHorror.Tasks;
using AsylumHorror.World;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.Monster
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI : NetworkBehaviour
    {
        public static MonsterAI Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private Transform roundStartPoint;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource stepAudioSource;
        [SerializeField] private AudioClip chaseClip;
        [SerializeField] private AudioClip patrolClip;
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Perception")]
        [SerializeField] private float viewDistance = 15.5f;
        [SerializeField] private float viewAngle = 92f;
        [SerializeField] private float closeSightDistance = 4.1f;
        [SerializeField] private float closeSightAngle = 168f;
        [SerializeField] private LayerMask sightMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float lostTargetAfterSeconds = 5.4f;

        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 2.3f;
        [SerializeField] private float investigateSpeed = 2.9f;
        [SerializeField] private float chaseSpeed = 5.1f;
        [SerializeField] private float searchSpeed = 3.3f;
        [SerializeField] private float carrySpeed = 3.1f;
        [SerializeField] private float patrolStoppingDistance = 0.9f;

        [Header("State Timers")]
        [SerializeField] private float investigateTimeout = 7f;
        [SerializeField] private float suspiciousDuration = 3.2f;
        [SerializeField] private float searchDuration = 8f;
        [SerializeField] private float cooldownDuration = 2.1f;
        [SerializeField] private float searchPointInterval = 1.35f;
        [SerializeField] private float searchRadius = 6.2f;
        [SerializeField] private float noiseMemorySeconds = 10f;
        [SerializeField] private float maxChaseDuration = 16f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackWindup = 0.55f;
        [SerializeField] private float attackCooldown = 1.8f;
        [SerializeField] private float hookReachDistance = 1.4f;
        [SerializeField] private float monsterDoorOpenRange = 2.2f;
        [SerializeField] private float monsterDoorOpenCooldown = 1.1f;
        [SerializeField] private float footstepIntervalPatrol = 0.7f;
        [SerializeField] private float footstepIntervalChase = 0.42f;
        [SerializeField] private float footstepIntervalCarry = 0.56f;

        [Header("Carry")]
        [SerializeField] private float maxCarryDuration = 24f;
        [SerializeField] private float carryRepathInterval = 0.45f;
        [SerializeField] private float carryStuckTimeout = 2.2f;
        [SerializeField] private float hookFailureCooldown = 5f;
        [SerializeField] private float carryNoHookDropDelay = 2.8f;
        [SerializeField] private float postHookDisperseDistance = 12f;
        [SerializeField] private float postHookDisperseJitter = 5f;
        [SerializeField] private float postHookInvestigateDuration = 6f;

        [Header("Recovery")]
        [SerializeField] private float pathRefreshInterval = 1.1f;
        [SerializeField] private float stalledRepathDelay = 1.25f;
        [SerializeField] private float minimumProgressDistance = 0.16f;
        [SerializeField] private float idleSweepTurnSpeed = 42f;

        [Header("Abilities")]
        [SerializeField] private float huntingSenseCooldown = 12f;
        [SerializeField] private float huntingSenseRadius = 14f;
        [SerializeField] private float crouchSenseRadius = 3f;
        [SerializeField] private float surgeCooldown = 18f;
        [SerializeField] private float surgeDuration = 2.5f;
        [SerializeField] private float surgeSpeedMultiplier = 1.24f;
        [SerializeField] private float surgeMinDistance = 3.5f;
        [SerializeField] private float surgeMaxDistance = 10.5f;

        [SyncVar(hook = nameof(OnStateSyncChanged))] private MonsterState currentState = MonsterState.Patrol;
        [SyncVar] private uint chaseTargetNetId;
        [SyncVar] private uint carriedTargetNetId;
        [SyncVar] private uint attackTargetNetId;
        [SyncVar] private double stateEndsAt;
        [SyncVar] private Vector3 interestPoint;

        private PatrolPoint[] patrolPoints = Array.Empty<PatrolPoint>();
        private int patrolIndex;
        private NoiseEvent? bufferedNoise;
        private double lastTimeSawTarget;
        private Vector3 lastKnownTargetPosition;
        private float nextAttackTime;
        private double nextSearchPointAt;
        private Vector3 currentSearchPoint;
        private double attackResolveAt;
        private double nextFootstepAt;
        private HookPoint targetHookPoint;
        private double carryStartedAt;
        private double nextCarryRepathAt;
        private double carryLastProgressAt;
        private double carryNoHookSinceAt;
        private double chaseStartedAt;
        private double nextDoorOpenAt;
        private double nextPathRefreshAt;
        private double lastTravelProgressAt;
        private double nextSensePulseAt;
        private double surgeEndsAt;
        private double nextSurgeAt;
        private Vector3 carryLastPosition;
        private Vector3 lastTravelProgressPosition;
        private readonly Dictionary<uint, double> blockedHookUntil = new Dictionary<uint, double>();
        private Light revealLight;
        private float revealLightBaseIntensity = -1f;
        private float revealLightBaseRange = -1f;

        public MonsterState CurrentState => currentState;

        private void Awake()
        {
            Instance = this;
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NoiseSystem.ServerNoiseEmitted += OnServerNoiseEmitted;
            CachePatrolPoints();
            SetState(MonsterState.Patrol);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            EnsureFallbackAudioClips();
            OnStateSyncChanged(currentState, currentState);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            NoiseSystem.ServerNoiseEmitted -= OnServerNoiseEmitted;
        }

        [Server]
        public void ServerResetForRound()
        {
            chaseTargetNetId = 0;
            carriedTargetNetId = 0;
            attackTargetNetId = 0;
            bufferedNoise = null;
            targetHookPoint = null;
            patrolIndex = 0;
            carryStartedAt = 0;
            nextCarryRepathAt = 0;
            carryLastProgressAt = 0;
            carryNoHookSinceAt = 0;
            chaseStartedAt = 0;
            nextDoorOpenAt = 0;
            nextPathRefreshAt = 0;
            nextSensePulseAt = NetworkTime.time + UnityEngine.Random.Range(4f, 7f);
            surgeEndsAt = 0;
            nextSurgeAt = 0;
            carryLastPosition = transform.position;
            lastTravelProgressPosition = transform.position;
            lastTravelProgressAt = NetworkTime.time;
            blockedHookUntil.Clear();
            stateEndsAt = 0;
            interestPoint = transform.position;
            lastKnownTargetPosition = transform.position;
            lastTimeSawTarget = 0;
            nextSearchPointAt = 0;
            attackResolveAt = 0;

            if (roundStartPoint != null)
            {
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.Warp(roundStartPoint.position);
                }
                else
                {
                    transform.SetPositionAndRotation(roundStartPoint.position, roundStartPoint.rotation);
                }
            }

            EnsureAgentReady();

            CachePatrolPoints();
            SetState(MonsterState.Patrol);
        }

        [ServerCallback]
        private void Update()
        {
            EnsureAgentReady();

            switch (currentState)
            {
                case MonsterState.Patrol:
                    TickPatrol();
                    break;
                case MonsterState.InvestigateSound:
                    TickInvestigate();
                    break;
                case MonsterState.Suspicious:
                    TickSuspicious();
                    break;
                case MonsterState.Chase:
                    TickChase();
                    break;
                case MonsterState.Search:
                    TickSearch();
                    break;
                case MonsterState.Attack:
                    TickAttack();
                    break;
                case MonsterState.Cooldown:
                    TickCooldown();
                    break;
                case MonsterState.Carry:
                    TickCarry();
                    break;
            }

            TryHandleNearbyDoor(ResolveDoorFocusPoint());
            TickAbilities();
            MaybeEmitFootstep();
        }

        [Server]
        private void TickPatrol()
        {
            ConfigureAgent(patrolSpeed, false);
            RefreshTravelProgress();

            if (TryFindVisiblePlayer(out NetworkPlayerStatus visiblePlayer))
            {
                BeginChase(visiblePlayer);
                return;
            }

            if (TryConsumeRecentNoise(out Vector3 noisePosition))
            {
                BeginInvestigate(noisePosition);
                return;
            }

            if (patrolPoints.Length == 0)
            {
                if (NetworkTime.time >= nextPathRefreshAt)
                {
                    currentSearchPoint = FindSearchPointNear(transform.position);
                    interestPoint = currentSearchPoint;
                    TrySetDestination(currentSearchPoint);
                    nextPathRefreshAt = NetworkTime.time + pathRefreshInterval;
                }

                SweepIdleLook();
                return;
            }

            bool invalidPath = navMeshAgent == null ||
                               !navMeshAgent.pathPending &&
                               (!navMeshAgent.hasPath || navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete);
            bool reached = navMeshAgent != null &&
                           navMeshAgent.hasPath &&
                           navMeshAgent.remainingDistance <= patrolStoppingDistance;
            bool stalled = IsMovementStalled(patrolStoppingDistance + 1.15f);
            bool timedRefresh = NetworkTime.time >= nextPathRefreshAt;
            bool needsNextPoint = invalidPath || reached || stalled || timedRefresh;
            if (needsNextPoint)
            {
                PatrolPoint nextPoint = patrolPoints[patrolIndex % patrolPoints.Length];
                if (stalled)
                {
                    PatrolPoint nearestPoint = ResolveNearestPatrolPoint();
                    if (nearestPoint != null)
                    {
                        int nearestIndex = Array.IndexOf(patrolPoints, nearestPoint);
                        if (nearestIndex >= 0)
                        {
                            patrolIndex = nearestIndex;
                            nextPoint = patrolPoints[patrolIndex % patrolPoints.Length];
                        }
                    }
                }

                patrolIndex++;
                interestPoint = nextPoint.transform.position;
                TrySetDestination(nextPoint.transform.position);
                nextPathRefreshAt = NetworkTime.time + pathRefreshInterval;
            }

            LookToward(interestPoint, idleSweepTurnSpeed);
        }

        [Server]
        private void TickInvestigate()
        {
            ConfigureAgent(investigateSpeed, false);
            RefreshTravelProgress();

            if (TryFindVisiblePlayer(out NetworkPlayerStatus visiblePlayer))
            {
                BeginChase(visiblePlayer);
                return;
            }

            if (TryConsumeRecentNoise(out Vector3 strongerNoise))
            {
                BeginInvestigate(strongerNoise);
                return;
            }

            bool needsRefresh = navMeshAgent == null ||
                                !navMeshAgent.hasPath ||
                                IsMovementStalled(patrolStoppingDistance + 0.95f) ||
                                NetworkTime.time >= nextPathRefreshAt;
            if (needsRefresh)
            {
                TrySetDestination(interestPoint);
                nextPathRefreshAt = NetworkTime.time + pathRefreshInterval;
            }

            bool reached = navMeshAgent.remainingDistance <= patrolStoppingDistance + 0.25f;
            bool timedOut = NetworkTime.time >= stateEndsAt;
            if (reached || timedOut)
            {
                BeginSuspicious(interestPoint);
            }
        }

        [Server]
        private void TickSuspicious()
        {
            ConfigureAgent(investigateSpeed * 0.75f, true);
            transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);

            if (TryFindVisiblePlayer(out NetworkPlayerStatus visiblePlayer))
            {
                BeginChase(visiblePlayer);
                return;
            }

            if (TryConsumeRecentNoise(out Vector3 noisePosition))
            {
                BeginInvestigate(noisePosition);
                return;
            }

            if (NetworkTime.time >= stateEndsAt)
            {
                BeginSearch(interestPoint);
            }
        }

        [Server]
        private void TickSearch()
        {
            ConfigureAgent(searchSpeed, false);
            RefreshTravelProgress();

            if (TryFindVisiblePlayer(out NetworkPlayerStatus visiblePlayer))
            {
                BeginChase(visiblePlayer);
                return;
            }

            if (TryConsumeRecentNoise(out Vector3 noisePosition))
            {
                BeginInvestigate(noisePosition);
                return;
            }

            if (NetworkTime.time >= stateEndsAt)
            {
                BeginCooldown();
                return;
            }

            bool shouldRefreshPoint = !navMeshAgent.hasPath ||
                                      IsMovementStalled(patrolStoppingDistance + 0.9f) ||
                                      navMeshAgent.remainingDistance <= patrolStoppingDistance + 0.1f ||
                                      NetworkTime.time >= nextSearchPointAt;
            if (!shouldRefreshPoint)
            {
                return;
            }

            currentSearchPoint = FindSearchPointNear(interestPoint);
            nextSearchPointAt = NetworkTime.time + searchPointInterval;
            TrySetDestination(currentSearchPoint);
        }

        [Server]
        private void TickChase()
        {
            ConfigureAgent(chaseSpeed, false);
            RefreshTravelProgress();

            NetworkPlayerStatus target = ResolveChaseTarget();
            if (target == null || !target.IsMonsterTargetable)
            {
                BeginSearch(lastKnownTargetPosition == Vector3.zero ? transform.position : lastKnownTargetPosition);
                return;
            }

            if (!navMeshAgent.hasPath ||
                IsMovementStalled(attackRange + 1.4f) ||
                NetworkTime.time >= nextPathRefreshAt)
            {
                TrySetDestination(target.transform.position);
                nextPathRefreshAt = NetworkTime.time + pathRefreshInterval;
            }

            bool visibleNow = CanSeePlayer(target);
            if (visibleNow)
            {
                lastTimeSawTarget = NetworkTime.time;
                lastKnownTargetPosition = target.transform.position;
                interestPoint = lastKnownTargetPosition;
            }
            else if (NetworkTime.time - lastTimeSawTarget > lostTargetAfterSeconds)
            {
                BeginSearch(lastKnownTargetPosition);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (NetworkTime.time - chaseStartedAt >= maxChaseDuration && (!visibleNow || distance >= 10f))
            {
                BeginSearch(lastKnownTargetPosition == Vector3.zero ? transform.position : lastKnownTargetPosition);
                return;
            }

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                BeginAttack(target);
            }
        }

        [Server]
        private void TickAttack()
        {
            ConfigureAgent(0f, true);

            NetworkPlayerStatus target = ResolveAttackTarget();
            if (target != null)
            {
                Vector3 direction = target.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
                }
            }

            if (NetworkTime.time < attackResolveAt)
            {
                return;
            }

            attackTargetNetId = 0;
            if (target == null ||
                !target.IsMonsterTargetable)
            {
                BeginCooldown();
                return;
            }

            if (!CanPerformMeleeAttack(target))
            {
                BeginChase(target);
                return;
            }

            ServerAttackTarget(target);
            if (currentState != MonsterState.Carry)
            {
                BeginCooldown();
            }
        }

        [Server]
        private void TickCooldown()
        {
            ConfigureAgent(patrolSpeed * 0.85f, false);

            if (TryFindVisiblePlayer(out NetworkPlayerStatus visiblePlayer))
            {
                BeginChase(visiblePlayer);
                return;
            }

            if (TryConsumeRecentNoise(out Vector3 noisePosition))
            {
                BeginInvestigate(noisePosition);
                return;
            }

            if (NetworkTime.time >= stateEndsAt)
            {
                SetState(MonsterState.Patrol);
            }
        }

        [Server]
        private void TickCarry()
        {
            ConfigureAgent(carrySpeed, false);
            RefreshTravelProgress();

            NetworkPlayerStatus carriedTarget = ResolveCarriedTarget();
            if (carriedTarget == null)
            {
                carriedTargetNetId = 0;
                SetState(MonsterState.Patrol);
                return;
            }

            if (carryAnchor != null)
            {
                carriedTarget.ServerSetCarriedPose(carryAnchor.position, carryAnchor.rotation);
            }
            else
            {
                carriedTarget.ServerSetCarriedPose(transform.position + Vector3.up * 1.4f, transform.rotation);
            }

            if (NetworkTime.time - carryStartedAt >= maxCarryDuration)
            {
                ForceDropCarriedTarget(carriedTarget, true);
                return;
            }

            if (Vector3.Distance(transform.position, carryLastPosition) >= 0.15f)
            {
                carryLastPosition = transform.position;
                carryLastProgressAt = NetworkTime.time;
            }

            if (NetworkTime.time - carryLastProgressAt >= carryStuckTimeout && targetHookPoint != null)
            {
                BlockHookPoint(targetHookPoint);
                targetHookPoint = null;
                carryLastProgressAt = NetworkTime.time;
            }

            if (targetHookPoint == null || !targetHookPoint.IsAvailable || IsHookTemporarilyBlocked(targetHookPoint))
            {
                targetHookPoint = FindClosestAvailableHook();
            }

            if (targetHookPoint == null)
            {
                if (carryNoHookSinceAt <= 0)
                {
                    carryNoHookSinceAt = NetworkTime.time;
                }

                if (NetworkTime.time >= nextCarryRepathAt)
                {
                    PatrolPoint fallbackPoint = ResolveNearestPatrolPoint();
                    if (fallbackPoint != null)
                    {
                        TrySetDestination(fallbackPoint.transform.position);
                    }

                    nextCarryRepathAt = NetworkTime.time + carryRepathInterval;
                }

                if (NetworkTime.time - carryNoHookSinceAt < carryNoHookDropDelay)
                {
                    return;
                }

                ForceDropCarriedTarget(carriedTarget, false);
                return;
            }
            carryNoHookSinceAt = 0;

            if (NetworkTime.time >= nextCarryRepathAt || IsMovementStalled(hookReachDistance + 1.5f))
            {
                if (!TrySetDestination(targetHookPoint.ApproachPosition))
                {
                    BlockHookPoint(targetHookPoint);
                    targetHookPoint = null;
                    nextCarryRepathAt = NetworkTime.time + carryRepathInterval;
                    return;
                }

                nextCarryRepathAt = NetworkTime.time + carryRepathInterval;
            }

            if (!navMeshAgent.pathPending &&
                navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete &&
                navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f)
            {
                BlockHookPoint(targetHookPoint);
                targetHookPoint = null;
                return;
            }

            if (Vector3.Distance(transform.position, targetHookPoint.ApproachPosition) <= hookReachDistance + 1f)
            {
                if (!CanUseHookPointFromCurrentPosition(targetHookPoint))
                {
                    TryHandleNearbyDoor(targetHookPoint.ApproachPosition);
                    nextCarryRepathAt = NetworkTime.time + 0.2f;
                    return;
                }

                if (targetHookPoint.ServerHookPlayer(carriedTarget))
                {
                    HookPoint usedHook = targetHookPoint;
                    carriedTargetNetId = 0;
                    targetHookPoint = null;
                    BeginPostHookDisperse(usedHook);
                }
                else
                {
                    BlockHookPoint(targetHookPoint);
                    targetHookPoint = null;
                }
            }
        }

        [Server]
        private void BeginChase(NetworkPlayerStatus target)
        {
            bool changedTarget = chaseTargetNetId != target.netId || currentState != MonsterState.Chase;
            chaseTargetNetId = target.netId;
            attackTargetNetId = 0;
            lastTimeSawTarget = NetworkTime.time;
            lastKnownTargetPosition = target.transform.position;
            interestPoint = lastKnownTargetPosition;
            if (changedTarget)
            {
                chaseStartedAt = NetworkTime.time;
            }
            SetState(MonsterState.Chase);
        }

        [Server]
        private void BeginInvestigate(Vector3 worldPoint)
        {
            chaseTargetNetId = 0;
            attackTargetNetId = 0;
            interestPoint = worldPoint;
            stateEndsAt = NetworkTime.time + investigateTimeout;
            TrySetDestination(interestPoint);
            SetState(MonsterState.InvestigateSound);
        }

        [Server]
        private void BeginSuspicious(Vector3 worldPoint)
        {
            chaseTargetNetId = 0;
            attackTargetNetId = 0;
            interestPoint = worldPoint;
            stateEndsAt = NetworkTime.time + suspiciousDuration;
            SetState(MonsterState.Suspicious);
        }

        [Server]
        private void BeginSearch(Vector3 centerPoint)
        {
            chaseTargetNetId = 0;
            attackTargetNetId = 0;
            interestPoint = centerPoint;
            stateEndsAt = NetworkTime.time + searchDuration;
            currentSearchPoint = FindSearchPointNear(centerPoint);
            nextSearchPointAt = NetworkTime.time + searchPointInterval;
            TrySetDestination(currentSearchPoint);
            SetState(MonsterState.Search);
        }

        [Server]
        private void BeginAttack(NetworkPlayerStatus target)
        {
            if (target == null)
            {
                return;
            }

            if (!CanPerformMeleeAttack(target))
            {
                BeginChase(target);
                return;
            }

            attackTargetNetId = target.netId;
            attackResolveAt = NetworkTime.time + attackWindup;
            nextAttackTime = Time.time + attackCooldown;
            SetState(MonsterState.Attack);
        }

        [Server]
        private void BeginCooldown()
        {
            chaseTargetNetId = 0;
            attackTargetNetId = 0;
            stateEndsAt = NetworkTime.time + cooldownDuration;
            SetState(MonsterState.Cooldown);
        }

        [Server]
        private void BeginPostHookDisperse(HookPoint hookedAt)
        {
            chaseTargetNetId = 0;
            attackTargetNetId = 0;

            Vector3 disperseTarget = transform.position;
            if (hookedAt != null)
            {
                Vector3 away = transform.position - hookedAt.HookPosition;
                if (away.sqrMagnitude <= 0.1f)
                {
                    away = transform.forward;
                }

                away.Normalize();
                BlockHookPoint(hookedAt);

                Vector3 desired = hookedAt.HookPosition +
                                  away * postHookDisperseDistance +
                                  new Vector3(
                                      UnityEngine.Random.Range(-postHookDisperseJitter, postHookDisperseJitter),
                                      0f,
                                      UnityEngine.Random.Range(-postHookDisperseJitter, postHookDisperseJitter));

                if (NavMesh.SamplePosition(desired, out NavMeshHit hit, postHookDisperseDistance + postHookDisperseJitter + 2f, NavMesh.AllAreas))
                {
                    disperseTarget = hit.position;
                }
                else
                {
                    disperseTarget = transform.position + away * postHookDisperseDistance;
                }
            }

            interestPoint = disperseTarget;
            stateEndsAt = NetworkTime.time + postHookInvestigateDuration;
            TrySetDestination(disperseTarget);
            SetState(MonsterState.InvestigateSound);
        }

        [Server]
        private void SetState(MonsterState nextState)
        {
            if (currentState == nextState)
            {
                return;
            }

            MonsterState previousState = currentState;

            if (currentState == MonsterState.Carry && nextState != MonsterState.Carry)
            {
                carriedTargetNetId = 0;
                targetHookPoint = null;
                carryStartedAt = 0;
            }

            currentState = nextState;
            nextFootstepAt = 0;
            nextPathRefreshAt = 0;
            lastTravelProgressAt = NetworkTime.time;
            lastTravelProgressPosition = transform.position;

            if (previousState != MonsterState.Chase && nextState == MonsterState.Chase)
            {
                nextSurgeAt = Math.Max(nextSurgeAt, NetworkTime.time + 1.1f);
            }
        }

        private Vector3 ResolveDoorFocusPoint()
        {
            switch (currentState)
            {
                case MonsterState.Chase:
                {
                    NetworkPlayerStatus chaseTarget = ResolveChaseTarget();
                    if (chaseTarget != null)
                    {
                        return chaseTarget.transform.position;
                    }

                    break;
                }
                case MonsterState.Attack:
                {
                    NetworkPlayerStatus attackTarget = ResolveAttackTarget();
                    if (attackTarget != null)
                    {
                        return attackTarget.transform.position;
                    }

                    break;
                }
                case MonsterState.Carry:
                    if (targetHookPoint != null)
                    {
                        return targetHookPoint.ApproachPosition;
                    }

                    break;
                case MonsterState.InvestigateSound:
                case MonsterState.Search:
                case MonsterState.Suspicious:
                case MonsterState.Cooldown:
                    if (interestPoint != Vector3.zero)
                    {
                        return interestPoint;
                    }

                    break;
            }

            return transform.position + transform.forward * 4f;
        }

        [Server]
        private bool TryHandleNearbyDoor(Vector3 focusPoint)
        {
            if (currentState == MonsterState.Attack ||
                currentState == MonsterState.Suspicious ||
                NetworkTime.time < nextDoorOpenAt)
            {
                return false;
            }

            Vector3 desiredDirection = focusPoint - transform.position;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude <= 0.05f)
            {
                desiredDirection = transform.forward;
            }

            Vector3 probeCenter = transform.position + Vector3.up * 1.2f;
            if (TryGetDoorOnLine(probeCenter, focusPoint, out NetworkDoor directDoor))
            {
                if (HandleDoorCandidate(directDoor))
                {
                    return true;
                }
            }

            Collider[] nearby = Physics.OverlapSphere(
                probeCenter,
                monsterDoorOpenRange + 2.1f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            NetworkDoor bestDoor = null;
            float bestScore = float.MaxValue;

            foreach (Collider collider in nearby)
            {
                if (collider == null)
                {
                    continue;
                }

                NetworkDoor door = collider.GetComponentInParent<NetworkDoor>();
                if (door == null || door.IsOpen)
                {
                    continue;
                }

                Vector3 toDoor = door.transform.position - transform.position;
                toDoor.y = 0f;
                if (toDoor.sqrMagnitude <= 0.01f)
                {
                    continue;
                }

                float distance = toDoor.magnitude;
                float angle = Vector3.Angle(desiredDirection.normalized, toDoor.normalized);
                if (angle > 118f)
                {
                    continue;
                }

                float focusDistance = Vector3.Distance(
                    new Vector3(door.transform.position.x, 0f, door.transform.position.z),
                    new Vector3(focusPoint.x, 0f, focusPoint.z));
                float score = distance * 0.78f + angle * 0.032f + focusDistance * 0.028f;
                if (score < bestScore)
                {
                    bestDoor = door;
                    bestScore = score;
                }
            }

            if (bestDoor == null)
            {
                return false;
            }

            return HandleDoorCandidate(bestDoor);
        }

        [Server]
        private bool HandleDoorCandidate(NetworkDoor door)
        {
            if (door == null)
            {
                return false;
            }

            float bestDoorDistance = Vector3.Distance(transform.position, door.transform.position);
            if (bestDoorDistance <= monsterDoorOpenRange + 0.95f)
            {
                if (door.ServerOpenForMonster())
                {
                    nextDoorOpenAt = NetworkTime.time + monsterDoorOpenCooldown;
                    return true;
                }

                return false;
            }

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh &&
                NavMesh.SamplePosition(door.transform.position, out NavMeshHit doorHit, 2.8f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(doorHit.position);
                nextPathRefreshAt = NetworkTime.time + pathRefreshInterval;
                return true;
            }

            return false;
        }

        [Server]
        private bool TryGetDoorOnLine(Vector3 probeCenter, Vector3 focusPoint, out NetworkDoor door)
        {
            door = null;
            Vector3 toFocus = focusPoint - probeCenter;
            if (toFocus.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            float maxDistance = Mathf.Min(toFocus.magnitude, 18f);
            if (!Physics.Raycast(
                    probeCenter,
                    toFocus.normalized,
                    out RaycastHit hit,
                    maxDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            door = hit.collider != null ? hit.collider.GetComponentInParent<NetworkDoor>() : null;
            return door != null && !door.IsOpen;
        }

        [Server]
        private void ServerAttackTarget(NetworkPlayerStatus target)
        {
            if (target == null || !CanPerformMeleeAttack(target))
            {
                return;
            }

            NoiseSystem.Emit(transform.position, 10f, 1.9f, NoiseCategory.Attack);

            bool knockedDown = target.ServerApplyMonsterHit();
            if (!knockedDown)
            {
                return;
            }

            if (!target.ServerSetCarried(netIdentity))
            {
                return;
            }

            carriedTargetNetId = target.netId;
            chaseTargetNetId = 0;
            attackTargetNetId = 0;
            targetHookPoint = FindClosestAvailableHook();
            carryStartedAt = NetworkTime.time;
            nextCarryRepathAt = 0;
            carryNoHookSinceAt = 0;
            carryLastPosition = transform.position;
            carryLastProgressAt = NetworkTime.time;
            SetState(MonsterState.Carry);
        }

        [Server]
        private bool TryFindVisiblePlayer(out NetworkPlayerStatus bestVisiblePlayer)
        {
            bestVisiblePlayer = null;
            float bestDistance = float.MaxValue;

            foreach (NetworkPlayerStatus player in FindObjectsByType<NetworkPlayerStatus>())
            {
                if (player == null || !player.IsMonsterTargetable)
                {
                    continue;
                }

                if (!CanSeePlayer(player))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestVisiblePlayer = player;
                }
            }

            return bestVisiblePlayer != null;
        }

        [Server]
        private bool CanSeePlayer(NetworkPlayerStatus player)
        {
            Vector3 eyesPosition = transform.position + Vector3.up * 1.7f;
            Vector3 chestPosition = player.transform.position + Vector3.up * 1.28f;
            Vector3 headPosition = player.transform.position + Vector3.up * 1.62f;
            Vector3 hipPosition = player.transform.position + Vector3.up * 0.82f;
            Vector3 direction = chestPosition - eyesPosition;
            float distance = direction.magnitude;
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            if (flatForward.sqrMagnitude <= 0.0001f)
            {
                flatForward = transform.forward;
            }

            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = direction;
            }

            float visibilityMultiplier = ResolveVisibilityMultiplier(player);
            bool inCloseSightRange = distance <= closeSightDistance;
            float effectiveViewDistance = Mathf.Max(closeSightDistance + 0.8f, viewDistance * visibilityMultiplier);
            if (!inCloseSightRange && distance > effectiveViewDistance)
            {
                return false;
            }

            if (distance <= 2.6f)
            {
                return HasLineOfSight(eyesPosition, chestPosition, player) ||
                       HasLineOfSight(eyesPosition, headPosition, player) ||
                       HasLineOfSight(eyesPosition, hipPosition, player);
            }

            float effectiveViewAngle = inCloseSightRange
                ? Mathf.Max(closeSightAngle, 164f)
                : Mathf.Clamp(viewAngle + (visibilityMultiplier - 1f) * 16f, 64f, 98f);
            float angle = Vector3.Angle(flatForward, flatDirection);
            if (angle > effectiveViewAngle * 0.5f)
            {
                return false;
            }

            return HasLineOfSight(eyesPosition, headPosition, player) ||
                   HasLineOfSight(eyesPosition, chestPosition, player) ||
                   HasLineOfSight(eyesPosition, hipPosition, player);
        }

        [Server]
        private float ResolveVisibilityMultiplier(NetworkPlayerStatus player)
        {
            float multiplier = 1f;

            NetworkPlayerController controller = player.GetComponent<NetworkPlayerController>();
            if (controller != null)
            {
                switch (controller.LocomotionState)
                {
                    case PlayerLocomotionState.Run:
                        multiplier += 0.12f;
                        break;
                    case PlayerLocomotionState.Crouch:
                        multiplier -= 0.4f;
                        break;
                    case PlayerLocomotionState.Walk:
                        multiplier += 0.02f;
                        break;
                }
            }

            PlayerFlashlight flashlight = player.GetComponent<PlayerFlashlight>();
            if (flashlight != null && flashlight.IsOn)
            {
                multiplier += 0.1f;
            }

            if (player.Condition == PlayerCondition.Injured)
            {
                multiplier += 0.03f;
            }

            if (player.FocusAbilityActive)
            {
                multiplier -= 0.12f;
            }

            return Mathf.Clamp(multiplier, 0.5f, 1.16f);
        }

        [Server]
        private bool HasLineOfSight(Vector3 eyesPosition, Vector3 samplePosition, NetworkPlayerStatus player)
        {
            Vector3 toSample = samplePosition - eyesPosition;
            float sampleDistance = toSample.magnitude;
            if (sampleDistance <= 0.01f)
            {
                return true;
            }

            if (Physics.Raycast(eyesPosition, toSample.normalized, out RaycastHit hit, sampleDistance, sightMask, QueryTriggerInteraction.Ignore))
            {
                return hit.transform == player.transform || hit.transform.IsChildOf(player.transform);
            }

            return true;
        }

        [Server]
        private void OnServerNoiseEmitted(NoiseEvent noiseEvent)
        {
            if (currentState == MonsterState.Carry)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, noiseEvent.Position);
            if (distance > noiseEvent.Radius)
            {
                return;
            }

            if (currentState == MonsterState.Chase && chaseTargetNetId != 0 &&
                noiseEvent.Category == NoiseCategory.PlayerMovement &&
                noiseEvent.Priority < 2f)
            {
                return;
            }

            if (!bufferedNoise.HasValue)
            {
                bufferedNoise = noiseEvent;
                return;
            }

            NoiseEvent previous = bufferedNoise.Value;
            bool previousExpired = NetworkTime.time - previous.Timestamp > noiseMemorySeconds;
            if (previousExpired || noiseEvent.Priority >= previous.Priority)
            {
                bufferedNoise = noiseEvent;
            }
        }

        [Server]
        private bool TryConsumeRecentNoise(out Vector3 position)
        {
            if (!bufferedNoise.HasValue)
            {
                position = Vector3.zero;
                return false;
            }

            NoiseEvent noise = bufferedNoise.Value;
            if (NetworkTime.time - noise.Timestamp > noiseMemorySeconds)
            {
                bufferedNoise = null;
                position = Vector3.zero;
                return false;
            }

            position = noise.Position;
            bufferedNoise = null;
            return true;
        }

        [Server]
        private void RefreshTravelProgress()
        {
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            if (!navMeshAgent.hasPath || navMeshAgent.pathPending)
            {
                lastTravelProgressPosition = transform.position;
                lastTravelProgressAt = NetworkTime.time;
                return;
            }

            Vector3 planarNow = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 planarLast = new Vector3(lastTravelProgressPosition.x, 0f, lastTravelProgressPosition.z);
            if (Vector3.Distance(planarNow, planarLast) >= minimumProgressDistance)
            {
                lastTravelProgressPosition = transform.position;
                lastTravelProgressAt = NetworkTime.time;
            }
        }

        [Server]
        private bool IsMovementStalled(float minRemainingDistance)
        {
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh || !navMeshAgent.hasPath || navMeshAgent.pathPending)
            {
                return false;
            }

            if (navMeshAgent.remainingDistance <= minRemainingDistance)
            {
                return false;
            }

            if (navMeshAgent.velocity.sqrMagnitude > 0.06f)
            {
                return false;
            }

            return NetworkTime.time - lastTravelProgressAt >= stalledRepathDelay;
        }

        [Server]
        private void EnsureAgentReady()
        {
            if (navMeshAgent == null || navMeshAgent.isOnNavMesh)
            {
                return;
            }

            if (!TrySampleRecoverPosition(transform.position, 8f, out NavMeshHit hit) &&
                (roundStartPoint == null || !TrySampleRecoverPosition(roundStartPoint.position, 10f, out hit)))
            {
                return;
            }

            navMeshAgent.Warp(hit.position);
            lastTravelProgressPosition = hit.position;
            lastTravelProgressAt = NetworkTime.time;
            nextPathRefreshAt = 0;
        }

        [Server]
        private static bool TrySampleRecoverPosition(Vector3 desiredPosition, float radius, out NavMeshHit hit)
        {
            return NavMesh.SamplePosition(desiredPosition, out hit, radius, NavMesh.AllAreas);
        }

        [Server]
        private void LookToward(Vector3 targetPosition, float turnSpeed)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        [Server]
        private void SweepIdleLook()
        {
            transform.Rotate(Vector3.up, idleSweepTurnSpeed * Time.deltaTime, Space.World);
        }

        [Server]
        private HookPoint FindClosestAvailableHook()
        {
            HookPoint[] hookPoints = FindObjectsByType<HookPoint>();
            HookPoint best = null;
            float bestDistance = float.MaxValue;

            foreach (HookPoint hook in hookPoints)
            {
                if (hook == null || !hook.IsAvailable || IsHookTemporarilyBlocked(hook))
                {
                    continue;
                }

                if (!TryEstimatePathDistance(hook.ApproachPosition, out float pathDistance))
                {
                    BlockHookPoint(hook);
                    continue;
                }

                if (pathDistance < bestDistance)
                {
                    bestDistance = pathDistance;
                    best = hook;
                }
            }

            return best;
        }

        [Server]
        private bool IsHookTemporarilyBlocked(HookPoint hook)
        {
            if (hook == null || hook.netId == 0)
            {
                return true;
            }

            if (!blockedHookUntil.TryGetValue(hook.netId, out double untilTime))
            {
                return false;
            }

            if (NetworkTime.time >= untilTime)
            {
                blockedHookUntil.Remove(hook.netId);
                return false;
            }

            return true;
        }

        [Server]
        private void BlockHookPoint(HookPoint hook)
        {
            if (hook == null || hook.netId == 0)
            {
                return;
            }

            blockedHookUntil[hook.netId] = NetworkTime.time + hookFailureCooldown;
        }

        [Server]
        private PatrolPoint ResolveNearestPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }

            PatrolPoint nearest = null;
            float bestDistance = float.MaxValue;
            foreach (PatrolPoint point in patrolPoints)
            {
                if (point == null)
                {
                    continue;
                }

                float distance;
                if (!TryEstimatePathDistance(point.transform.position, out distance))
                {
                    distance = Vector3.Distance(transform.position, point.transform.position) + 20f;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        [Server]
        private bool TryEstimatePathDistance(Vector3 targetPosition, out float distance)
        {
            distance = float.MaxValue;
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!navMeshAgent.CalculatePath(targetPosition, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            if (path.corners == null || path.corners.Length < 2)
            {
                distance = Vector3.Distance(transform.position, targetPosition);
                return true;
            }

            float total = 0f;
            for (int index = 1; index < path.corners.Length; index++)
            {
                total += Vector3.Distance(path.corners[index - 1], path.corners[index]);
            }

            distance = total;
            return true;
        }

        [Server]
        private bool CanPerformMeleeAttack(NetworkPlayerStatus target)
        {
            if (target == null || !target.IsMonsterTargetable)
            {
                return false;
            }

            Vector3 chest = target.transform.position + Vector3.up * 1.24f;
            Vector3 head = target.transform.position + Vector3.up * 1.62f;
            Vector3 eyes = transform.position + Vector3.up * 1.65f;

            float maxDistance = attackRange + 0.45f;
            if (Vector3.Distance(transform.position, target.transform.position) > maxDistance)
            {
                return false;
            }

            return HasLineOfSight(eyes, chest, target) || HasLineOfSight(eyes, head, target);
        }

        [Server]
        private bool CanUseHookPointFromCurrentPosition(HookPoint hookPoint)
        {
            if (hookPoint == null || !hookPoint.IsAvailable)
            {
                return false;
            }

            if (!TryEstimatePathDistance(hookPoint.ApproachPosition, out float pathDistance))
            {
                return false;
            }

            if (pathDistance > hookReachDistance + 1.75f)
            {
                return false;
            }

            Vector3 probeOrigin = transform.position + Vector3.up * 1.35f;
            Vector3 hookProbe = hookPoint.ApproachPosition + Vector3.up * 1.25f;
            Vector3 direction = hookProbe - probeOrigin;
            if (direction.sqrMagnitude <= 0.01f)
            {
                return true;
            }

            if (!Physics.Raycast(
                    probeOrigin,
                    direction.normalized,
                    out RaycastHit hit,
                    direction.magnitude,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                return true;
            }

            Transform hitTransform = hit.transform;
            return hitTransform == hookPoint.transform || hitTransform.IsChildOf(hookPoint.transform);
        }

        [Server]
        private bool TrySetDestination(Vector3 desiredPosition)
        {
            EnsureAgentReady();
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
            {
                return false;
            }

            Vector3 sampledPosition = desiredPosition;
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                sampledPosition = hit.position;
            }

            NavMeshPath path = new NavMeshPath();
            bool hasCalculatedPath = navMeshAgent.CalculatePath(sampledPosition, path);
            if (hasCalculatedPath && path.status == NavMeshPathStatus.PathComplete)
            {
                return navMeshAgent.SetPath(path);
            }

            if (TryHandleNearbyDoor(sampledPosition))
            {
                path = new NavMeshPath();
                hasCalculatedPath = navMeshAgent.CalculatePath(sampledPosition, path);
                if (hasCalculatedPath && path.status == NavMeshPathStatus.PathComplete)
                {
                    return navMeshAgent.SetPath(path);
                }

                return true;
            }

            return navMeshAgent.SetDestination(sampledPosition);
        }

        [Server]
        private Vector3 FindSearchPointNear(Vector3 centerPoint)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * searchRadius;
                Vector3 sample = centerPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);
                if (NavMesh.SamplePosition(sample, out NavMeshHit hit, 2.8f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return centerPoint;
        }

        [Server]
        private void ConfigureAgent(float speed, bool stop)
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.speed = speed * ResolveSpeedMultiplier();
            navMeshAgent.isStopped = stop;
        }

        [Server]
        private float ResolveSpeedMultiplier()
        {
            return NetworkTime.time < surgeEndsAt ? surgeSpeedMultiplier : 1f;
        }

        [Server]
        private void ForceDropCarriedTarget(NetworkPlayerStatus carriedTarget, bool emitNoise)
        {
            if (carriedTarget == null)
            {
                carriedTargetNetId = 0;
                targetHookPoint = null;
                SetState(MonsterState.Patrol);
                return;
            }

            Vector3 dropPosition = transform.position + transform.right * 1.1f;
            carriedTarget.ServerDropFromCarry(dropPosition);
            carriedTargetNetId = 0;
            targetHookPoint = null;
            carryStartedAt = 0;
            carryNoHookSinceAt = 0;

            if (emitNoise)
            {
                NoiseSystem.Emit(transform.position, 9f, 1.2f, NoiseCategory.Attack);
            }

            SetState(MonsterState.Cooldown);
            stateEndsAt = NetworkTime.time + cooldownDuration;
        }

        [Server]
        private NetworkPlayerStatus ResolveChaseTarget()
        {
            if (chaseTargetNetId == 0)
            {
                return null;
            }

            return NetworkServer.spawned.TryGetValue(chaseTargetNetId, out NetworkIdentity identity)
                ? identity.GetComponent<NetworkPlayerStatus>()
                : null;
        }

        [Server]
        private NetworkPlayerStatus ResolveCarriedTarget()
        {
            if (carriedTargetNetId == 0)
            {
                return null;
            }

            return NetworkServer.spawned.TryGetValue(carriedTargetNetId, out NetworkIdentity identity)
                ? identity.GetComponent<NetworkPlayerStatus>()
                : null;
        }

        [Server]
        private NetworkPlayerStatus ResolveAttackTarget()
        {
            if (attackTargetNetId == 0)
            {
                return null;
            }

            return NetworkServer.spawned.TryGetValue(attackTargetNetId, out NetworkIdentity identity)
                ? identity.GetComponent<NetworkPlayerStatus>()
                : null;
        }

        [Server]
        private void CachePatrolPoints()
        {
            patrolPoints = FindObjectsByType<PatrolPoint>()
                .OrderBy(point => point.name)
                .ToArray();
        }

        [Server]
        private void TickAbilities()
        {
            TrySensePulse();
            TryTriggerSurge();
        }

        [Server]
        private void TrySensePulse()
        {
            if (currentState == MonsterState.Chase ||
                currentState == MonsterState.Attack ||
                currentState == MonsterState.Carry ||
                NetworkTime.time < nextSensePulseAt)
            {
                return;
            }

            NetworkPlayerStatus sensedTarget = null;
            float bestDistance = float.MaxValue;

            foreach (NetworkPlayerStatus player in FindObjectsByType<NetworkPlayerStatus>())
            {
                if (player == null || !player.IsMonsterTargetable)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance > huntingSenseRadius)
                {
                    continue;
                }

                NetworkPlayerController controller = player.GetComponent<NetworkPlayerController>();
                if (controller != null &&
                    controller.LocomotionState == PlayerLocomotionState.Crouch &&
                    distance > crouchSenseRadius)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    sensedTarget = player;
                }
            }

            nextSensePulseAt = NetworkTime.time + huntingSenseCooldown * UnityEngine.Random.Range(0.88f, 1.18f);
            if (sensedTarget == null)
            {
                return;
            }

            Vector3 probePoint = sensedTarget.transform.position;
            if (sensedTarget.TryGetComponent(out CharacterController characterController))
            {
                probePoint += characterController.velocity * 0.18f;
            }

            lastKnownTargetPosition = probePoint;
            BeginInvestigate(probePoint);
        }

        [Server]
        private void TryTriggerSurge()
        {
            if (currentState != MonsterState.Chase || NetworkTime.time < nextSurgeAt || NetworkTime.time < surgeEndsAt)
            {
                return;
            }

            NetworkPlayerStatus target = ResolveChaseTarget();
            if (target == null || !target.IsMonsterTargetable || !CanSeePlayer(target))
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < surgeMinDistance || distance > surgeMaxDistance)
            {
                return;
            }

            surgeEndsAt = NetworkTime.time + surgeDuration;
            nextSurgeAt = NetworkTime.time + surgeCooldown;
        }

        [Server]
        private void MaybeEmitFootstep()
        {
            if (stepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            if (currentState == MonsterState.Attack || currentState == MonsterState.Suspicious)
            {
                return;
            }

            if (NetworkTime.time < nextFootstepAt)
            {
                return;
            }

            float velocity = navMeshAgent != null ? navMeshAgent.velocity.magnitude : 0f;
            if (velocity < 0.25f)
            {
                return;
            }

            float interval = footstepIntervalPatrol;
            if (currentState == MonsterState.Chase || currentState == MonsterState.Search)
            {
                interval = footstepIntervalChase;
            }
            else if (currentState == MonsterState.Carry)
            {
                interval = footstepIntervalCarry;
            }

            nextFootstepAt = NetworkTime.time + interval;
            int clipIndex = UnityEngine.Random.Range(0, footstepClips.Length);
            RpcPlayFootstep(clipIndex);
        }

        private void OnStateSyncChanged(MonsterState _, MonsterState nextState)
        {
            if (animator != null)
            {
                animator.SetInteger("MonsterState", (int)nextState);
            }

            if (audioSource == null)
            {
                return;
            }

            AudioClip nextClip = nextState == MonsterState.Chase || nextState == MonsterState.Attack
                ? chaseClip
                : patrolClip;

            if (nextClip == null)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                audioSource.clip = null;
                return;
            }

            if (audioSource.clip != nextClip)
            {
                audioSource.clip = nextClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        [ClientRpc]
        private void RpcPlayFootstep(int clipIndex)
        {
            if (stepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(clipIndex, 0, footstepClips.Length - 1);
            AudioClip clip = footstepClips[safeIndex];
            if (clip != null)
            {
                switch (currentState)
                {
                    case MonsterState.Chase:
                    case MonsterState.Attack:
                        stepAudioSource.pitch = 0.78f;
                        break;
                    case MonsterState.Carry:
                        stepAudioSource.pitch = 0.72f;
                        break;
                    default:
                        stepAudioSource.pitch = 0.84f;
                        break;
                }

                stepAudioSource.PlayOneShot(clip);
            }
        }

        private void EnsureFallbackAudioClips()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                audioSource.volume = 0f;
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 4f;
                audioSource.maxDistance = 30f;
            }

            if (footstepClips == null || footstepClips.Length == 0)
            {
                footstepClips = new[]
                {
                    ProceduralAudioFactory.GetFootstepClip("monster_a", 82f, 0.95f),
                    ProceduralAudioFactory.GetFootstepClip("monster_b", 70f, 0.95f),
                    ProceduralAudioFactory.GetFootstepClip("monster_c", 90f, 0.95f)
                };
            }

            if (stepAudioSource != null)
            {
                stepAudioSource.spatialBlend = 1f;
                stepAudioSource.minDistance = 10f;
                stepAudioSource.maxDistance = 58f;
                stepAudioSource.volume = 1.28f;
            }
        }

        public void SetClientRevealBoost(float reveal01)
        {
            if (!isClient)
            {
                return;
            }

            if (revealLight == null)
            {
                foreach (Light childLight in GetComponentsInChildren<Light>(true))
                {
                    if (childLight != null &&
                        childLight.name.IndexOf("EyeGlow", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        revealLight = childLight;
                        break;
                    }
                }

                if (revealLight == null)
                {
                    revealLight = GetComponentInChildren<Light>(true);
                }
            }

            if (revealLight == null)
            {
                return;
            }

            if (revealLightBaseIntensity < 0f)
            {
                revealLightBaseIntensity = revealLight.intensity;
                revealLightBaseRange = revealLight.range;
            }

            revealLight.intensity = Mathf.Lerp(revealLightBaseIntensity, revealLightBaseIntensity + 5.2f, Mathf.Clamp01(reveal01));
            revealLight.range = Mathf.Lerp(revealLightBaseRange, revealLightBaseRange + 7f, Mathf.Clamp01(reveal01));
        }
    }
}
