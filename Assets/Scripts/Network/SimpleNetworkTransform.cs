using Mirror;
using UnityEngine;
using AsylumHorror.Player;

namespace AsylumHorror.Network
{
    public class SimpleNetworkTransform : NetworkBehaviour
    {
        [Header("Authority")]
        [SerializeField] private bool ownerAuthoritative = true;

        [Header("Sync")]
        [SerializeField] private float sendInterval = 0.05f;
        [SerializeField] private float lerpRate = 18f;
        [SerializeField] private float snapDistance = 4f;

        [SyncVar] private Vector3 syncedPosition;
        [SyncVar] private Quaternion syncedRotation = Quaternion.identity;

        private float nextSendTime;
        private NetworkPlayerStatus playerStatus;

        [ServerCallback]
        private void Start()
        {
            syncedPosition = transform.position;
            syncedRotation = transform.rotation;
            playerStatus = GetComponent<NetworkPlayerStatus>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            syncedPosition = transform.position;
            syncedRotation = transform.rotation;
        }

        private void Update()
        {
            if (isServer)
            {
                ServerPublishTransformIfNeeded();
                return;
            }

            if (isOwned && ownerAuthoritative)
            {
                if (playerStatus == null)
                {
                    playerStatus = GetComponent<NetworkPlayerStatus>();
                }

                if (playerStatus != null && !playerStatus.CanControlCharacter)
                {
                    ApplyRemoteTransform();
                    return;
                }

                ClientSendTransformIfNeeded();
                return;
            }

            if (!isOwned)
            {
                ApplyRemoteTransform();
            }
        }

        [Server]
        private void ServerPublishTransformIfNeeded()
        {
            if (Time.time < nextSendTime)
            {
                return;
            }

            nextSendTime = Time.time + sendInterval;
            syncedPosition = transform.position;
            syncedRotation = transform.rotation;
        }

        [Client]
        private void ClientSendTransformIfNeeded()
        {
            if (Time.time < nextSendTime)
            {
                return;
            }

            nextSendTime = Time.time + sendInterval;
            CmdUploadTransform(transform.position, transform.rotation);
        }

        [Command]
        private void CmdUploadTransform(Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            syncedPosition = worldPosition;
            syncedRotation = worldRotation;
        }

        [Client]
        private void ApplyRemoteTransform()
        {
            float distance = Vector3.Distance(transform.position, syncedPosition);
            if (distance > snapDistance)
            {
                transform.SetPositionAndRotation(syncedPosition, syncedRotation);
                return;
            }

            transform.position = Vector3.Lerp(transform.position, syncedPosition, Time.deltaTime * lerpRate);
            transform.rotation = Quaternion.Slerp(transform.rotation, syncedRotation, Time.deltaTime * lerpRate);
        }
    }
}
