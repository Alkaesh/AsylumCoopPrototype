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
        [SyncVar(hook = nameof(OnSpawnEnabledChanged))] private bool spawnEnabled = true;

        private void Awake()
        {
            if (pickupClip == null)
            {
                pickupClip = ProceduralAudioFactory.GetKeycardPickupClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player) || consumed || !spawnEnabled || player == null)
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
                return string.Empty;
            }

            if (!spawnEnabled)
            {
                return string.Empty;
            }

            return "E: Replace cell";
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

        [Server]
        public void ServerSetSpawnEnabled(bool enabled)
        {
            spawnEnabled = enabled;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisualState(consumed, spawnEnabled);
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
            ApplyVisualState(nextValue, spawnEnabled);
        }

        private void OnSpawnEnabledChanged(bool _, bool nextValue)
        {
            ApplyVisualState(consumed, nextValue);
        }

        private void ApplyVisualState(bool isConsumed, bool isSpawnEnabled)
        {
            bool visible = !isConsumed && isSpawnEnabled;

            if (visualRenderers != null && visualRenderers.Length > 0)
            {
                foreach (Renderer renderer in visualRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = visible;
                    }
                }
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = visible;
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = visible;
            }
        }
    }
}
