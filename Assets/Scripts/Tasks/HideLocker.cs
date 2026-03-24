using AsylumHorror.Interaction;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Tasks
{
    public class HideLocker : NetworkInteractable
    {
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip enterClip;
        [SerializeField] private AudioClip exitClip;

        [SyncVar(hook = nameof(OnOccupantChanged))] private uint occupantNetId;

        public bool IsOccupied => occupantNetId != 0;

        public override bool CanInteract(NetworkPlayerStatus player)
        {
            if (!base.CanInteract(player) || player == null)
            {
                return false;
            }

            return occupantNetId == 0 || occupantNetId == player.netId;
        }

        public override string GetPrompt(NetworkPlayerStatus player)
        {
            if (player != null && occupantNetId == player.netId)
            {
                return "E: Exit Hideout";
            }

            return occupantNetId == 0 ? $"E: {InteractionName}" : "Hideout Occupied";
        }

        [Server]
        public override void ServerInteract(NetworkPlayerStatus player)
        {
            if (player == null)
            {
                return;
            }

            if (occupantNetId == player.netId)
            {
                Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position + transform.forward * 1.4f;
                Quaternion exitRotation = exitPoint != null ? exitPoint.rotation : transform.rotation;
                player.ServerSetHidden(false, exitPosition, exitRotation);
                occupantNetId = 0;
                RpcPlaySound(false);
                return;
            }

            if (occupantNetId != 0 || player.Condition == PlayerCondition.Knocked)
            {
                return;
            }

            Vector3 hidePosition = hidePoint != null ? hidePoint.position : transform.position;
            Quaternion hideRotation = hidePoint != null ? hidePoint.rotation : transform.rotation;
            player.ServerSetHidden(true, hidePosition, hideRotation);
            occupantNetId = player.netId;
            RpcPlaySound(true);
        }

        [ServerCallback]
        private void Update()
        {
            if (occupantNetId == 0)
            {
                return;
            }

            if (!NetworkServer.spawned.TryGetValue(occupantNetId, out NetworkIdentity identity) ||
                !identity.TryGetComponent(out NetworkPlayerStatus player) ||
                !player.IsHidden)
            {
                occupantNetId = 0;
            }
        }

        private void OnOccupantChanged(uint _, uint __)
        {
        }

        [ClientRpc]
        private void RpcPlaySound(bool entering)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = entering ? enterClip : exitClip;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
