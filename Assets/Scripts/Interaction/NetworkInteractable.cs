using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Interaction
{
    public abstract class NetworkInteractable : NetworkBehaviour
    {
        [SerializeField] private string interactionName = "Interact";
        [SerializeField] private float holdDuration = 1.5f;
        [SerializeField] private float serverInteractDistance = 3f;

        public string InteractionName => interactionName;
        public float HoldDuration => holdDuration;
        public float ServerInteractDistance => serverInteractDistance;

        public virtual float GetHoldDuration(NetworkPlayerStatus player)
        {
            return holdDuration;
        }

        public virtual bool CanInteract(NetworkPlayerStatus player)
        {
            return player != null && player.CanUseInteraction;
        }

        public virtual string GetPrompt(NetworkPlayerStatus player)
        {
            return interactionName;
        }

        public abstract void ServerInteract(NetworkPlayerStatus player);
    }
}
