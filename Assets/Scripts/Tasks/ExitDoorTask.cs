using AsylumHorror.Core;
using AsylumHorror.Audio;
using AsylumHorror.Interaction;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class ExitDoorTask : NetworkInteractable
    {
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Vector3 closedEuler = Vector3.zero;
        [SerializeField] private Vector3 openedEuler = new Vector3(0f, 100f, 0f);
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private float openNoiseRadius = 12f;

        [SyncVar(hook = nameof(OnDoorStateChanged))] private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (openClip == null)
            {
                openClip = ProceduralAudioFactory.GetDoorOpenClip();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            GameStateManager.Instance?.ServerRegisterExitDoor(this);
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player))
            {
                return false;
            }

            GameStateManager gameState = GameStateManager.Instance;
            if (gameState == null)
            {
                return false;
            }

            return isOpen || gameState.AreCoreTasksCompleted;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            GameStateManager gameState = GameStateManager.Instance;
            if (isOpen)
            {
                return "Hold E: Go";
            }

            if (gameState == null || !gameState.AreCoreTasksCompleted)
            {
                return "The breach is still sealed";
            }

            return "Hold E: Pull the breach open";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (!isOpen)
            {
                if (GameStateManager.Instance != null && GameStateManager.Instance.AreCoreTasksCompleted)
                {
                    ServerForceOpen();
                }
                else
                {
                    return;
                }
            }

            GameStateManager.Instance?.ServerTryEscape(player);
        }

        [Server]
        public void ServerForceOpen()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            NoiseSystem.Emit(transform.position, openNoiseRadius, 1f, NoiseCategory.Task);
            RpcPlayOpenSound();
        }

        [Server]
        public void ServerResetState()
        {
            isOpen = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyDoorVisual(isOpen);
        }

        private void OnDoorStateChanged(bool _, bool nextValue)
        {
            ApplyDoorVisual(nextValue);
        }

        [ClientRpc]
        private void RpcPlayOpenSound()
        {
            if (audioSource != null && openClip != null)
            {
                audioSource.PlayOneShot(openClip);
            }
        }

        private void ApplyDoorVisual(bool opened)
        {
            if (doorVisual != null)
            {
                doorVisual.localRotation = Quaternion.Euler(opened ? openedEuler : closedEuler);
            }
        }
    }
}
