using AsylumHorror.Network;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace AsylumHorror.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Text playersText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text rosterText;
        [SerializeField] private Text briefingText;
        [SerializeField] private Text postMatchText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text readyButtonLabel;
        [SerializeField] private Button leaveButton;

        private LobbyState lobbyState;
        private HorrorNetworkManager networkManager;
        private bool isLocalReady;

        private void OnEnable()
        {
            LobbyState.LobbyChanged += Refresh;
        }

        private void OnDisable()
        {
            LobbyState.LobbyChanged -= Refresh;
        }

        private void Start()
        {
            networkManager = HorrorNetworkManager.Instance;
            Refresh();
        }

        private void Update()
        {
            if (lobbyState == null)
            {
                lobbyState = FindAnyObjectByType<LobbyState>();
            }

            Refresh();
        }

        public void OnStartGameClicked()
        {
            if (lobbyState != null)
            {
                lobbyState.CmdRequestStartGame();
            }
        }

        public void OnReadyClicked()
        {
            if (lobbyState == null)
            {
                return;
            }

            isLocalReady = !isLocalReady;
            lobbyState.CmdSetReadyState(isLocalReady);
            Refresh();
        }

        public void OnLeaveClicked()
        {
            if (networkManager != null)
            {
                networkManager.LeaveSessionToMenu();
            }
        }

        private void Refresh()
        {
            if (lobbyState == null)
            {
                lobbyState = FindAnyObjectByType<LobbyState>();
            }

            int count = lobbyState != null ? lobbyState.PlayerCount : 0;
            int readyCount = lobbyState != null ? lobbyState.ReadyCount : 0;
            bool canStart = lobbyState != null && lobbyState.CanStart;
            bool isHost = NetworkServer.active && NetworkClient.isConnected;
            int maxPlayers = lobbyState != null ? lobbyState.MaxPlayers : 4;
            int minPlayers = lobbyState != null ? lobbyState.MinPlayersToStart : 1;

            if (playersText != null)
            {
                playersText.text = $"Survivors in staging: {count}/{maxPlayers}\nReady: {readyCount}/{Mathf.Max(1, count)}";
            }

            if (infoText != null)
            {
                if (canStart)
                {
                    infoText.text = "All survivors are ready. Host can start the insertion.";
                }
                else if (count < minPlayers)
                {
                    infoText.text = $"Need at least {minPlayers} survivor(s) in staging.";
                }
                else
                {
                    infoText.text = "Everyone must ready up before the host can deploy the team.";
                }
            }

            if (rosterText != null)
            {
                rosterText.text = lobbyState != null ? lobbyState.RosterSummary : "Scanning roster...";
            }

            if (briefingText != null)
            {
                briefingText.text =
                    "BRIEFING\n" +
                    "Restore auxiliary power, recover the access card, unlock the north exit and move as a team.\n" +
                    "The creature responds to light, footsteps and clattering gear. Injured survivors stay loud after rescue.\n\n" +
                    "LOBBY CONTROLS\n" +
                    "WASD move | Mouse orbit | Shift jog | Q steady hands | F tempt fate on hook";
            }

            if (postMatchText != null)
            {
                postMatchText.text = RoundPresentationMemory.HasSummary
                    ? $"LAST OUTCOME\n{RoundPresentationMemory.LastHeadline}\n\n{RoundPresentationMemory.LastSummary}"
                    : "LAST OUTCOME\nNo previous shift record.";
            }

            if (startGameButton != null)
            {
                startGameButton.interactable = isHost && canStart;
                startGameButton.gameObject.SetActive(isHost);
            }

            if (readyButton != null)
            {
                readyButton.interactable = NetworkClient.isConnected;
            }

            if (readyButtonLabel != null)
            {
                readyButtonLabel.text = isLocalReady ? "CANCEL READY" : "READY UP";
            }

            if (leaveButton != null)
            {
                leaveButton.interactable = true;
            }
        }
    }
}
