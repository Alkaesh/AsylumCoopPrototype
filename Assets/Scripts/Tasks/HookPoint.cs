using AsylumHorror.Interaction;
using AsylumHorror.Audio;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class HookPoint : NetworkInteractable
    {
        [SerializeField] private Transform hookAttachPoint;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color freeColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color occupiedColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private AudioSource rescueAudioSource;
        [SerializeField] private AudioClip rescueClip;
        [SerializeField] private float rescueNoiseRadius = 10f;
        [SerializeField] private float approachDistance = 1.05f;

        [SyncVar(hook = nameof(OnOccupiedPlayerChanged))] private uint occupiedPlayerNetId;

        public bool IsOccupied => occupiedPlayerNetId != 0;
        public bool IsAvailable => occupiedPlayerNetId == 0;
        public Vector3 HookPosition => hookAttachPoint != null ? hookAttachPoint.position : transform.position + Vector3.up * 1.5f;
        public Vector3 ApproachPosition
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude <= 0.01f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();
                return transform.position + forward * approachDistance;
            }
        }

        private void Awake()
        {
            if (rescueClip == null)
            {
                rescueClip = ProceduralAudioFactory.GetRescueClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player))
            {
                return false;
            }

            return occupiedPlayerNetId != 0 && player.netId != occupiedPlayerNetId;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            return occupiedPlayerNetId == 0 ? "Hook Empty" : "Hold E: Rescue Teammate";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            NetworkPlayerStatus hookedPlayer = ResolveOccupantServer();
            if (hookedPlayer == null || hookedPlayer.Condition != PlayerCondition.Hooked)
            {
                occupiedPlayerNetId = 0;
                return;
            }

            player.ServerRegisterSave();
            hookedPlayer.ServerRescueFromHook(this);
            occupiedPlayerNetId = 0;
            NoiseSystem.Emit(transform.position, rescueNoiseRadius, 1f, NoiseCategory.Task);
            RpcPlayRescueSound();
        }

        [Server]
        public bool ServerHookPlayer(NetworkPlayerStatus playerToHook)
        {
            if (playerToHook == null ||
                occupiedPlayerNetId != 0 ||
                playerToHook.Condition != PlayerCondition.Carried ||
                Vector3.Distance(playerToHook.transform.position, HookPosition) > 2.4f)
            {
                return false;
            }

            occupiedPlayerNetId = playerToHook.netId;
            playerToHook.ServerSetHooked(this, HookPosition);
            return true;
        }

        [Server]
        public void ServerClearIfOccupant(NetworkPlayerStatus player)
        {
            if (player != null && occupiedPlayerNetId == player.netId)
            {
                occupiedPlayerNetId = 0;
            }
        }

        [Server]
        public void ServerResetState()
        {
            occupiedPlayerNetId = 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisual(IsOccupied);
        }

        [ClientRpc]
        private void RpcPlayRescueSound()
        {
            if (rescueAudioSource != null && rescueClip != null)
            {
                rescueAudioSource.PlayOneShot(rescueClip);
            }
        }

        private void OnOccupiedPlayerChanged(uint _, uint __)
        {
            ApplyVisual(IsOccupied);
        }

        [Server]
        private NetworkPlayerStatus ResolveOccupantServer()
        {
            if (occupiedPlayerNetId == 0)
            {
                return null;
            }

            if (NetworkServer.spawned.TryGetValue(occupiedPlayerNetId, out NetworkIdentity identity) &&
                identity.TryGetComponent(out NetworkPlayerStatus playerStatus))
            {
                return playerStatus;
            }

            return null;
        }

        private void ApplyVisual(bool occupied)
        {
            if (indicatorRenderer != null && indicatorRenderer.material != null)
            {
                indicatorRenderer.material.color = occupied ? occupiedColor : freeColor;
            }
        }
    }
}
