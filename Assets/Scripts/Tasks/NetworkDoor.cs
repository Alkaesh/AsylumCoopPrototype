using AsylumHorror.Audio;
using AsylumHorror.Core;
using AsylumHorror.Interaction;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.Tasks
{
    public class NetworkDoor : NetworkInteractable
    {
        [Header("Door")]
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Transform secondaryDoorVisual;
        [SerializeField] private NavMeshObstacle navObstacle;
        [SerializeField] private NavMeshObstacle secondaryNavObstacle;
        [SerializeField] private Collider[] blockingColliders;
        [SerializeField] private Vector3 closedEuler = Vector3.zero;
        [SerializeField] private Vector3 openedEuler = new Vector3(0f, 96f, 0f);
        [SerializeField] private Vector3 secondaryClosedEuler = Vector3.zero;
        [SerializeField] private Vector3 secondaryOpenedEuler = new Vector3(0f, -96f, 0f);
        [SerializeField] private bool startOpen;
        [SerializeField] private bool startLocked;
        [SerializeField] private bool requiresPower;
        [SerializeField] private bool requiresKeycard;
        [SerializeField] private float animationDuration = 0.42f;
        [SerializeField] private float interactionCooldown = 0.08f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip lockedClip;
        [SerializeField] private float openNoiseRadius = 14f;

        [SyncVar(hook = nameof(OnOpenChanged))] private bool isOpen;
        [SyncVar(hook = nameof(OnLockedChanged))] private bool isLocked;
        [SyncVar(hook = nameof(OnTransitionStateChanged))] private bool isTransitioning;

        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;
        public bool IsTransitioning => isTransitioning;

        private double transitionEndsAt;
        private Quaternion leftVisualFromRotation;
        private Quaternion leftVisualTargetRotation;
        private Quaternion rightVisualFromRotation;
        private Quaternion rightVisualTargetRotation;
        private float visualTransitionStartedAt;
        private bool visualTransitionActive;
        private bool visualsInitialized;
        private bool bakePassThroughMode;

        private void Awake()
        {
            if (openClip == null)
            {
                openClip = ProceduralAudioFactory.GetDoorOpenClip();
            }

            if (closeClip == null)
            {
                closeClip = ProceduralAudioFactory.GetFlashlightOffClip();
            }

            if (lockedClip == null)
            {
                lockedClip = ProceduralAudioFactory.GetKeycardPickupClip();
            }
        }

        [ServerCallback]
        private void Update()
        {
            UpdateVisualTransition();

            if (isServer && isTransitioning && NetworkTime.time >= transitionEndsAt)
            {
                isTransitioning = false;
                ApplyCollisionState(isOpen);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            isOpen = startOpen;
            isLocked = startLocked;
            isTransitioning = false;
            transitionEndsAt = 0;
            SnapVisual(isOpen);
            ApplyCollisionState(isOpen);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SnapVisual(isOpen);
            ApplyCollisionState(isOpen);
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            return base.CanInteract(player) && !isTransitioning;
        }

        public override float GetHoldDuration(NetworkPlayerStatus player)
        {
            if (isTransitioning)
            {
                return 0f;
            }

            if (player == null || !player.HasRescueTrauma)
            {
                return 0f;
            }

            if (isLocked && !HasUnlockRequirements())
            {
                return 0f;
            }

            float baseDuration = isOpen ? 0.24f : 0.48f;
            return baseDuration * player.InteractionDurationMultiplier;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            if (isTransitioning)
            {
                return "Door Moving";
            }

            if (isOpen)
            {
                return "E: Close Door";
            }

            if (!isLocked)
            {
                return "E: Open Door";
            }

            if (!HasUnlockRequirements())
            {
                if (requiresPower && requiresKeycard)
                {
                    return "Locked: Need Power + Keycard";
                }

                if (requiresPower)
                {
                    return "Locked: Need Power";
                }

                if (requiresKeycard)
                {
                    return "Locked: Need Keycard";
                }

                return "Locked Door";
            }

            return "E: Unlock Door";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (isTransitioning)
            {
                return;
            }

            if (isOpen)
            {
                BeginDoorTransition(false, false);
                return;
            }

            if (isLocked && !HasUnlockRequirements())
            {
                RpcPlaySound(false, true);
                return;
            }

            BeginDoorTransition(true, true);
        }

        [Server]
        public bool ServerOpenForMonster()
        {
            if (isOpen || isTransitioning)
            {
                return false;
            }

            if (isLocked && !HasUnlockRequirements())
            {
                return false;
            }

            return BeginDoorTransition(true, false);
        }

        [Server]
        public void ServerSetRandomState(bool opened, bool locked)
        {
            if (requiresPower || requiresKeycard)
            {
                isOpen = false;
                isLocked = true;
                isTransitioning = false;
                transitionEndsAt = 0;
                SnapVisual(false);
                ApplyCollisionState(false);
                return;
            }

            isOpen = opened;
            isLocked = locked;
            isTransitioning = false;
            transitionEndsAt = 0;
            SnapVisual(isOpen);
            ApplyCollisionState(isOpen);
        }

        private void OnOpenChanged(bool _, bool nextValue)
        {
            if (!visualsInitialized)
            {
                SnapVisual(nextValue);
                return;
            }

            BeginVisualTransition(nextValue);
        }

        private void OnLockedChanged(bool _, bool __)
        {
        }

        private void OnTransitionStateChanged(bool _, bool nextValue)
        {
            if (nextValue)
            {
                if (!isOpen)
                {
                    ApplyCollisionState(false);
                }

                return;
            }

            ApplyCollisionState(isOpen);
        }

        [ClientRpc]
        private void RpcPlaySound(bool opening, bool blocked)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = blocked ? lockedClip : (opening ? openClip : closeClip);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private bool HasUnlockRequirements()
        {
            if (!isLocked)
            {
                return true;
            }

            GameStateManager gameState = GameStateManager.Instance;
            if (gameState == null)
            {
                return false;
            }

            if (requiresPower && !gameState.PowerRestored)
            {
                return false;
            }

            if (requiresKeycard && !gameState.KeycardCollected)
            {
                return false;
            }

            return true;
        }

        [Server]
        private bool BeginDoorTransition(bool openDoor, bool emitNoise)
        {
            if (isTransitioning)
            {
                return false;
            }

            if (!openDoor && !isOpen)
            {
                return false;
            }

            if (openDoor)
            {
                if (isLocked && !HasUnlockRequirements())
                {
                    return false;
                }

                if (isLocked)
                {
                    isLocked = false;
                }
            }

            isOpen = openDoor;
            isTransitioning = true;
            transitionEndsAt = NetworkTime.time + animationDuration + interactionCooldown;
            BeginVisualTransition(openDoor);
            if (!openDoor)
            {
                ApplyCollisionState(false);
            }

            if (emitNoise && openDoor)
            {
                NoiseSystem.Emit(transform.position, openNoiseRadius, 1.2f, NoiseCategory.Task);
            }

            RpcPlaySound(openDoor, false);
            return true;
        }

        public void SetBakePassThroughMode(bool enabled)
        {
            bakePassThroughMode = enabled;
            if (enabled)
            {
                SetBlockersEnabled(false);
                SetNavObstaclesEnabled(false);
                return;
            }

            ApplyCollisionState(isOpen);
        }

        private void SnapVisual(bool opened)
        {
            visualTransitionActive = false;
            visualsInitialized = true;
            leftVisualTargetRotation = Quaternion.Euler(opened ? openedEuler : closedEuler);
            rightVisualTargetRotation = Quaternion.Euler(opened ? secondaryOpenedEuler : secondaryClosedEuler);

            if (doorVisual != null)
            {
                doorVisual.localRotation = leftVisualTargetRotation;
            }

            if (secondaryDoorVisual != null)
            {
                secondaryDoorVisual.localRotation = rightVisualTargetRotation;
            }
        }

        private void BeginVisualTransition(bool opened)
        {
            visualsInitialized = true;
            visualTransitionStartedAt = Time.time;
            visualTransitionActive = animationDuration > 0.01f;

            leftVisualFromRotation = doorVisual != null ? doorVisual.localRotation : Quaternion.identity;
            leftVisualTargetRotation = Quaternion.Euler(opened ? openedEuler : closedEuler);

            rightVisualFromRotation = secondaryDoorVisual != null ? secondaryDoorVisual.localRotation : Quaternion.identity;
            rightVisualTargetRotation = Quaternion.Euler(opened ? secondaryOpenedEuler : secondaryClosedEuler);

            if (!visualTransitionActive)
            {
                SnapVisual(opened);
            }
        }

        private void UpdateVisualTransition()
        {
            if (!visualTransitionActive)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, animationDuration);
            float progress = Mathf.Clamp01((Time.time - visualTransitionStartedAt) / duration);

            if (doorVisual != null)
            {
                doorVisual.localRotation = Quaternion.Slerp(leftVisualFromRotation, leftVisualTargetRotation, progress);
            }

            if (secondaryDoorVisual != null)
            {
                secondaryDoorVisual.localRotation = Quaternion.Slerp(rightVisualFromRotation, rightVisualTargetRotation, progress);
            }

            if (progress >= 1f)
            {
                visualTransitionActive = false;
            }
        }

        private void ApplyCollisionState(bool opened)
        {
            if (bakePassThroughMode)
            {
                SetBlockersEnabled(false);
                SetNavObstaclesEnabled(false);
                return;
            }

            SetNavObstaclesEnabled(!opened);
            SetBlockersEnabled(!opened);
        }

        private void SetNavObstaclesEnabled(bool enabled)
        {
            if (navObstacle != null)
            {
                navObstacle.enabled = enabled;
            }

            if (secondaryNavObstacle != null)
            {
                secondaryNavObstacle.enabled = enabled;
            }
        }

        private void SetBlockersEnabled(bool enabled)
        {
            if (blockingColliders == null)
            {
                return;
            }

            foreach (Collider blocker in blockingColliders)
            {
                if (blocker != null)
                {
                    blocker.enabled = enabled;
                }
            }
        }
    }
}
