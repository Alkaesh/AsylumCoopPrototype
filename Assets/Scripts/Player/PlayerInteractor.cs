using AsylumHorror.Interaction;
using AsylumHorror.UI;
using Mirror;
using System;
using UnityEngine;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(NetworkPlayerStatus))]
    public class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float interactionDistance = 2.7f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        private NetworkPlayerStatus playerStatus;
        private NetworkInteractable currentInteractable;
        private HudController hudController;
        private float holdProgressTime;
        private float nextServerInteractTime;

        private void Awake()
        {
            playerStatus = GetComponent<NetworkPlayerStatus>();
            if (interactionCamera == null)
            {
                interactionCamera = GetComponentInChildren<Camera>(true);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (hudController == null)
            {
                hudController = FindAnyObjectByType<HudController>();
            }

            if (!playerStatus.CanUseInteraction)
            {
                ResetInteractionPrompt();
                return;
            }

            NetworkInteractable nextInteractable = FindInteractableFromCenterRay();
            if (nextInteractable == null || !nextInteractable.CanInteract(playerStatus))
            {
                ResetInteractionPrompt();
                return;
            }

            if (currentInteractable != nextInteractable)
            {
                holdProgressTime = 0f;
            }

            currentInteractable = nextInteractable;
            float effectiveHoldDuration = Mathf.Max(0f, currentInteractable.GetHoldDuration(playerStatus));
            bool instantInteract = effectiveHoldDuration <= 0.05f;
            float holdDuration = instantInteract ? 0f : Mathf.Max(0.05f, effectiveHoldDuration);

            if (instantInteract)
            {
                holdProgressTime = 0f;
                if (Input.GetKeyDown(interactionKey) && Time.time >= nextServerInteractTime)
                {
                    nextServerInteractTime = Time.time + 0.15f;
                    CmdTryInteract(currentInteractable.netIdentity);
                }
            }
            else if (Input.GetKey(interactionKey))
            {
                holdProgressTime += Time.deltaTime;
                if (holdProgressTime >= holdDuration && Time.time >= nextServerInteractTime)
                {
                    nextServerInteractTime = Time.time + 0.15f;
                    holdProgressTime = 0f;
                    CmdTryInteract(currentInteractable.netIdentity);
                }
            }
            else
            {
                holdProgressTime = 0f;
            }

            float holdNormalized = instantInteract || holdDuration <= 0.001f
                ? 0f
                : Mathf.Clamp01(holdProgressTime / holdDuration);
            hudController?.SetInteractionPrompt(currentInteractable.GetPrompt(playerStatus), holdNormalized);
        }

        [Command]
        private void CmdTryInteract(NetworkIdentity targetIdentity)
        {
            if (targetIdentity == null || !targetIdentity.TryGetComponent(out NetworkInteractable interactable))
            {
                return;
            }

            if (playerStatus == null)
            {
                playerStatus = GetComponent<NetworkPlayerStatus>();
            }

            float distance = Vector3.Distance(transform.position, interactable.transform.position);
            if (distance > interactable.ServerInteractDistance + 0.5f)
            {
                return;
            }

            if (!interactable.CanInteract(playerStatus))
            {
                return;
            }

            interactable.ServerInteract(playerStatus);
        }

        private NetworkInteractable FindInteractableFromCenterRay()
        {
            if (interactionCamera == null)
            {
                return null;
            }

            Collider[] nearbyCamera = Physics.OverlapSphere(
                interactionCamera.transform.position + interactionCamera.transform.forward * 0.35f,
                0.85f,
                interactionMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider collider in nearbyCamera)
            {
                NetworkInteractable interactable = collider.GetComponentInParent<NetworkInteractable>();
                if (interactable != null)
                {
                    return interactable;
                }
            }

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactionMask, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    NetworkInteractable interactable = hit.collider.GetComponentInParent<NetworkInteractable>();
                    if (interactable != null)
                    {
                        return interactable;
                    }
                }
            }

            Vector3 probePoint = ray.origin + ray.direction * interactionDistance;
            Collider[] nearby = Physics.OverlapSphere(probePoint, 0.4f, interactionMask, QueryTriggerInteraction.Collide);
            foreach (Collider collider in nearby)
            {
                NetworkInteractable interactable = collider.GetComponentInParent<NetworkInteractable>();
                if (interactable != null)
                {
                    return interactable;
                }
            }

            return null;
        }

        private void ResetInteractionPrompt()
        {
            holdProgressTime = 0f;
            currentInteractable = null;
            hudController?.SetInteractionPrompt(string.Empty, 0f);
        }
    }
}
