using AsylumHorror.Player;
using UnityEngine;

namespace AsylumHorror.UI
{
    [RequireComponent(typeof(Camera))]
    public class LobbyFallbackCamera : MonoBehaviour
    {
        [SerializeField] private AudioListener audioListener;

        private Camera fallbackCamera;

        private void Awake()
        {
            fallbackCamera = GetComponent<Camera>();
            if (audioListener == null)
            {
                audioListener = GetComponent<AudioListener>();
            }
        }

        private void LateUpdate()
        {
            bool shouldEnable = true;
            foreach (NetworkPlayerController controller in FindObjectsByType<NetworkPlayerController>())
            {
                if (!controller.isLocalPlayer)
                {
                    continue;
                }

                shouldEnable = false;
                break;
            }

            if (fallbackCamera != null && fallbackCamera.enabled != shouldEnable)
            {
                fallbackCamera.enabled = shouldEnable;
            }

            if (audioListener != null && audioListener.enabled != shouldEnable)
            {
                audioListener.enabled = shouldEnable;
            }
        }
    }
}
