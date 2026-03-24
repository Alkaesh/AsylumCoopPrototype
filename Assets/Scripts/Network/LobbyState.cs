using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Network
{
    public class LobbyState : NetworkBehaviour
    {
        public static event Action LobbyChanged;

        [Header("Lobby")]
        [SerializeField] private int minPlayersToStart = 1;
        [SerializeField] private int maxPlayers = 4;

        [SyncVar(hook = nameof(OnPlayerCountChanged))] private int playerCount;
        [SyncVar(hook = nameof(OnReadyCountChanged))] private int readyCount;
        [SyncVar(hook = nameof(OnCanStartChanged))] private bool canStart;
        [SyncVar(hook = nameof(OnRosterSummaryChanged))] private string rosterSummary;

        private float nextRefreshTime;
        private readonly HashSet<int> readyConnections = new HashSet<int>();

        public int PlayerCount => playerCount;
        public int ReadyCount => readyCount;
        public bool CanStart => canStart;
        public int MinPlayersToStart => Mathf.Max(1, minPlayersToStart);
        public int MaxPlayers => Mathf.Max(MinPlayersToStart, maxPlayers);
        public string RosterSummary => string.IsNullOrWhiteSpace(rosterSummary) ? "No survivors connected." : rosterSummary;

        [ServerCallback]
        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.25f;
            int connectedPlayers = 0;
            readyConnections.RemoveWhere(connectionId =>
                !NetworkServer.connections.ContainsKey(connectionId) ||
                NetworkServer.connections[connectionId]?.identity == null);

            List<NetworkConnectionToClient> orderedConnections = new List<NetworkConnectionToClient>(NetworkServer.connections.Values);
            orderedConnections.Sort((a, b) => (a?.connectionId ?? int.MaxValue).CompareTo(b?.connectionId ?? int.MaxValue));
            StringBuilder rosterBuilder = new StringBuilder();
            int rosterIndex = 1;

            foreach (NetworkConnectionToClient connection in orderedConnections)
            {
                if (connection?.identity != null)
                {
                    connectedPlayers++;
                    bool ready = readyConnections.Contains(connection.connectionId);
                    rosterBuilder
                        .Append("SURVIVOR ")
                        .Append(rosterIndex++)
                        .Append(connection.connectionId == 0 ? "  HOST" : string.Empty)
                        .Append("  |  ")
                        .Append(ready ? "READY" : "STANDING BY")
                        .Append('\n');
                }
            }

            if (playerCount != connectedPlayers)
            {
                playerCount = connectedPlayers;
            }

            int nextReadyCount = readyConnections.Count;
            if (readyCount != nextReadyCount)
            {
                readyCount = nextReadyCount;
            }

            string nextRosterSummary = rosterBuilder.Length > 0
                ? rosterBuilder.ToString().TrimEnd()
                : "No survivors connected.";
            if (rosterSummary != nextRosterSummary)
            {
                rosterSummary = nextRosterSummary;
            }

            bool nextCanStart = connectedPlayers >= MinPlayersToStart &&
                                connectedPlayers <= MaxPlayers &&
                                connectedPlayers > 0 &&
                                readyCount == connectedPlayers;
            if (canStart != nextCanStart)
            {
                canStart = nextCanStart;
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestStartGame()
        {
            if (!canStart)
            {
                return;
            }

            HorrorNetworkManager.Instance?.ServerStartGameplay();
        }

        [Command(requiresAuthority = false)]
        public void CmdSetReadyState(bool ready, NetworkConnectionToClient sender = null)
        {
            if (sender?.identity == null)
            {
                return;
            }

            if (ready)
            {
                readyConnections.Add(sender.connectionId);
            }
            else
            {
                readyConnections.Remove(sender.connectionId);
            }

            nextRefreshTime = 0f;
        }

        private void OnPlayerCountChanged(int _, int __)
        {
            LobbyChanged?.Invoke();
        }

        private void OnReadyCountChanged(int _, int __)
        {
            LobbyChanged?.Invoke();
        }

        private void OnCanStartChanged(bool _, bool __)
        {
            LobbyChanged?.Invoke();
        }

        private void OnRosterSummaryChanged(string _, string __)
        {
            LobbyChanged?.Invoke();
        }
    }
}
