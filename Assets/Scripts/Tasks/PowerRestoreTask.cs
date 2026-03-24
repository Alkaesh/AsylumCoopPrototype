using AsylumHorror.Core;
using AsylumHorror.Audio;
using AsylumHorror.Interaction;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class PowerRestoreTask : NetworkInteractable
    {
        [SerializeField] private Renderer panelRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color activeColor = new Color(0.3f, 0.95f, 0.5f);
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip restoreClip;
        [SerializeField] private float noiseRadius = 25f;

        [SyncVar(hook = nameof(OnRestoredChanged))] private bool restored;

        private void Awake()
        {
            if (restoreClip == null)
            {
                restoreClip = ProceduralAudioFactory.GetPowerRestoreClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            return base.CanInteract(player) && !restored && ArePrerequisitesMet();
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            if (restored)
            {
                return string.Empty;
            }

            if (!ArePrerequisitesMet())
            {
                return "The final relay is still dead";
            }

            return "Hold E: Route main power";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (restored || !ArePrerequisitesMet())
            {
                return;
            }

            restored = true;
            GameStateManager.Instance?.ServerMarkPowerRestored();
            NoiseSystem.Emit(transform.position, noiseRadius, 2.2f, NoiseCategory.Task);
            RpcPlayRestoreSound();
        }

        [Server]
        public void ServerResetState()
        {
            restored = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisual(restored);
        }

        [ClientRpc]
        private void RpcPlayRestoreSound()
        {
            if (audioSource != null && restoreClip != null)
            {
                audioSource.PlayOneShot(restoreClip);
            }
        }

        private bool ArePrerequisitesMet()
        {
            GameStateManager gameState = GameStateManager.Instance;
            if (gameState == null)
            {
                return false;
            }

            return gameState.GeneratorsCompleted >= gameState.RequiredGenerators &&
                   gameState.KeycardCollected;
        }

        private void OnRestoredChanged(bool _, bool nextValue)
        {
            ApplyVisual(nextValue);
        }

        private void ApplyVisual(bool isRestored)
        {
            if (panelRenderer != null && panelRenderer.material != null)
            {
                panelRenderer.material.color = isRestored ? activeColor : inactiveColor;
            }
        }
    }
}
