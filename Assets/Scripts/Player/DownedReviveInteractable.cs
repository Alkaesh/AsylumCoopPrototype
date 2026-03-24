using AsylumHorror.Interaction;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(NetworkPlayerStatus))]
    public class DownedReviveInteractable : NetworkInteractable
    {
        [SerializeField] private NetworkPlayerStatus ownerStatus;
        [SerializeField] private float reviveOffsetDistance = 1f;

        private void Awake()
        {
            if (ownerStatus == null)
            {
                ownerStatus = GetComponent<NetworkPlayerStatus>();
            }
        }

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player) || ownerStatus == null)
            {
                return false;
            }

            return ownerStatus.Condition == PlayerCondition.Knocked && player.netId != ownerStatus.netId;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            if (ownerStatus == null || ownerStatus.Condition != PlayerCondition.Knocked)
            {
                return string.Empty;
            }

            return "Hold E: Revive Teammate";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (ownerStatus == null || player == null)
            {
                return;
            }

            Vector3 direction = ownerStatus.transform.position - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = player.transform.forward;
            }

            Vector3 revivePosition = ownerStatus.transform.position + direction.normalized * reviveOffsetDistance;
            if (ownerStatus.ServerReviveFromKnocked(revivePosition))
            {
                player.ServerRegisterSave();
            }
        }
    }
}
