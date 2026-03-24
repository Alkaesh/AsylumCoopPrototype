using AsylumHorror.Core;
using AsylumHorror.Audio;
using AsylumHorror.Interaction;
using AsylumHorror.Monster;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class GeneratorTask : NetworkInteractable
    {
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color inactiveColor = Color.red;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource loopAudioSource;
        [SerializeField] private AudioClip activateClip;
        [SerializeField] private AudioClip loopClip;
        [SerializeField] private float noiseRadius = 22f;

        [SyncVar(hook = nameof(OnActivatedChanged))] private bool activated;

        public bool IsActivated => activated;

        private void Awake()
        {
            if (activateClip == null)
            {
                activateClip = ProceduralAudioFactory.GetGeneratorStartClip();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            return base.CanInteract(player) && !activated;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            return activated ? "Generator Activated" : "Hold E: Start Generator";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (activated)
            {
                return;
            }

            activated = true;
            GameStateManager.Instance?.ServerMarkGeneratorActivated();
            NoiseSystem.Emit(transform.position, noiseRadius, 1.8f, NoiseCategory.Task);
            RpcPlayActivateSound();
        }

        [Server]
        public void ServerResetState()
        {
            activated = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisualState(activated);
        }

        private void OnActivatedChanged(bool _, bool nextValue)
        {
            ApplyVisualState(nextValue);
        }

        [ClientRpc]
        private void RpcPlayActivateSound()
        {
            if (audioSource != null && activateClip != null)
            {
                audioSource.PlayOneShot(activateClip);
            }
        }

        private void ApplyVisualState(bool isActivated)
        {
            if (indicatorRenderer != null && indicatorRenderer.material != null)
            {
                indicatorRenderer.material.color = isActivated ? activeColor : inactiveColor;
            }

            if (loopAudioSource == null || loopClip == null)
            {
                return;
            }

            loopAudioSource.clip = loopClip;
            loopAudioSource.loop = true;
            if (isActivated)
            {
                if (!loopAudioSource.isPlaying)
                {
                    loopAudioSource.Play();
                }
            }
            else if (loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }
        }
    }
}
