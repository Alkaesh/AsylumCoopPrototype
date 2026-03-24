using AsylumHorror.Core;
using AsylumHorror.Audio;
using AsylumHorror.Interaction;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class KeycardTask : NetworkInteractable
    {
        [SerializeField] private Renderer keycardRenderer;
        [SerializeField] private Collider keycardCollider;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pickupClip;

        [SyncVar(hook = nameof(OnCollectedChanged))] private bool collected;

        private void Awake()
        {
            if (pickupClip == null)
            {
                pickupClip = ProceduralAudioFactory.GetKeycardPickupClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            return base.CanInteract(player) && !collected;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            return collected ? string.Empty : "Hold E: Take clearance";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (collected)
            {
                return;
            }

            collected = true;
            GameStateManager.Instance?.ServerMarkKeycardCollected();
            RpcPlayPickupSound();
        }

        [Server]
        public void ServerResetState()
        {
            collected = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyCollectedVisual(collected);
        }

        private void OnCollectedChanged(bool _, bool nextValue)
        {
            ApplyCollectedVisual(nextValue);
        }

        [ClientRpc]
        private void RpcPlayPickupSound()
        {
            if (audioSource != null && pickupClip != null)
            {
                audioSource.PlayOneShot(pickupClip);
            }
        }

        private void ApplyCollectedVisual(bool isCollected)
        {
            if (keycardRenderer != null)
            {
                keycardRenderer.enabled = !isCollected;
            }

            if (keycardCollider != null)
            {
                keycardCollider.enabled = !isCollected;
            }
        }
    }
}
