using AsylumHorror.Audio;
using AsylumHorror.Interaction;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class BatteryPickupTask : NetworkInteractable
    {
        [SerializeField] private float rechargeSeconds = 75f;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private Collider pickupCollider;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pickupClip;

        [SyncVar(hook = nameof(OnConsumedChanged))] private bool consumed;

        private void Awake()
        {
            if (pickupClip == null)
            {
                pickupClip = ProceduralAudioFactory.GetKeycardPickupClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player) || consumed || player == null)
            {
                return false;
            }

            PlayerFlashlight flashlight = player.GetComponent<PlayerFlashlight>();
            return flashlight != null && flashlight.Battery01 < 0.995f;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            if (consumed)
            {
                return "Battery Used";
            }

            return "E: Insert Battery";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (consumed || player == null)
            {
                return;
            }

            PlayerFlashlight flashlight = player.GetComponent<PlayerFlashlight>();
            if (flashlight == null)
            {
                return;
            }

            flashlight.ServerRechargeBattery(rechargeSeconds);
            consumed = true;
            RpcPlayPickupSound();
        }

        [Server]
        public void ServerResetState()
        {
            consumed = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyConsumedVisual(consumed);
        }

        [ClientRpc]
        private void RpcPlayPickupSound()
        {
            if (audioSource != null && pickupClip != null)
            {
                audioSource.PlayOneShot(pickupClip);
            }
        }

        private void OnConsumedChanged(bool _, bool nextValue)
        {
            ApplyConsumedVisual(nextValue);
        }

        private void ApplyConsumedVisual(bool isConsumed)
        {
            if (visualRenderers != null && visualRenderers.Length > 0)
            {
                foreach (Renderer renderer in visualRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = !isConsumed;
                    }
                }
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = !isConsumed;
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = !isConsumed;
            }
        }
    }
}
