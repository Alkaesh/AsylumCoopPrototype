using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using AsylumHorror.Core;
using AsylumHorror.World;
using Mirror;
using kcp2k;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AsylumHorror.Network
{
    public class HorrorNetworkManager : NetworkManager
    {
        public static HorrorNetworkManager Instance { get; private set; }

        [Header("Scene Flow")]
        [Scene] [SerializeField] private string menuSceneName = "MainMenu";
        [Scene] [SerializeField] private string lobbySceneName = "Lobby";
        [Scene] [SerializeField] private string gameplaySceneName = "HospitalLevel";

        [Header("Network")]
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private bool enableAutomaticPortMapping = true;

        public string MenuSceneName => menuSceneName;
        public string LobbySceneName => lobbySceneName;
        public string GameplaySceneName => gameplaySceneName;
        public ushort DefaultPort => defaultPort;
        public int MaxPlayers => Mathf.Clamp(maxPlayers, 1, 16);
        public event System.Action ConnectionHintsChanged;

        private string localInviteSummary = "LAN host: pending";
        private string publicInviteSummary = "WAN host: start a host to generate an internet endpoint.";
        private string portMappingSummary = "Direct internet play idle.";
        private bool upnpMappingOpened;
        private ushort mappedPort;
        private string mappedLocalAddress;
        private Coroutine publicEndpointRoutine;

        public override void Awake()
        {
            base.Awake();
            Instance = this;
            offlineScene = menuSceneName;
            onlineScene = lobbySceneName;
            maxConnections = Mathf.Clamp(maxPlayers, 1, 16);
            RefreshLocalInviteSummary();
            NotifyConnectionHintsChanged();
        }

        public override void OnDestroy()
        {
            ReleaseMappedPort();
            base.OnDestroy();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartHostFromMenu()
        {
            StartHostFromMenu(defaultPort);
        }

        public void StartHostFromMenu(ushort port)
        {
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                return;
            }

            ApplyTransportPort(port);
            PrepareInternetHosting(port);
            onlineScene = lobbySceneName;
            StartHost();
        }

        public void StartClientFromMenu(string address)
        {
            StartClientFromMenu(address, 0);
        }

        public void StartClientFromMenu(string address, ushort explicitPort)
        {
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                return;
            }

            string parsedAddress = ParseAddress(address, explicitPort, out ushort parsedPort);
            ApplyTransportPort(parsedPort);
            networkAddress = parsedAddress;
            StartClient();
        }

        public string BuildConnectionHint()
        {
            RefreshLocalInviteSummary();
            return $"{localInviteSummary}\n{publicInviteSummary}\n{portMappingSummary}\nRemote play: host shares the WAN line above. If WAN stays unavailable, expose UDP {defaultPort} on the host router and share the public IP manually.";
        }

        public string ResolveBestLocalIPv4()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                IPAddress localAddress = addresses.FirstOrDefault(address =>
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address));

                if (localAddress != null)
                {
                    return localAddress.ToString();
                }
            }
            catch
            {
                // Ignore and fallback to localhost.
            }

            return "127.0.0.1";
        }

        public void LeaveSessionToMenu()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StopHost();
            }
            else if (NetworkServer.active)
            {
                StopServer();
            }
            else if (NetworkClient.isConnected)
            {
                StopClient();
            }

            if (SceneManager.GetActiveScene().name != menuSceneName)
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        [Server]
        public void ServerStartGameplay()
        {
            int requiredPlayers = 1;
            LobbyState lobbyState = FindAnyObjectByType<LobbyState>();
            if (lobbyState != null)
            {
                requiredPlayers = lobbyState.MinPlayersToStart;
            }

            if (numPlayers < requiredPlayers)
            {
                return;
            }

            ServerChangeScene(gameplaySceneName);
        }

        [Server]
        public void ServerReturnToLobby()
        {
            ServerChangeScene(lobbySceneName);
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);

            if (sceneName != gameplaySceneName)
            {
                return;
            }

            ServerPositionPlayersAtSpawns();
            GameStateManager.Instance?.ServerInitializeRound();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            GameStateManager.Instance?.ServerEvaluateRoundState();
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            ResetInternetHostState();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ResetInternetHostState();
        }

        [Server]
        private void ServerPositionPlayersAtSpawns()
        {
            List<PlayerSpawnPoint> spawnPoints = FindObjectsByType<PlayerSpawnPoint>()
                .OrderBy(point => point.SpawnIndex)
                .ToList();

            if (spawnPoints.Count == 0)
            {
                return;
            }

            int index = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection?.identity == null)
                {
                    continue;
                }

                PlayerSpawnPoint spawn = spawnPoints[index % spawnPoints.Count];
                connection.identity.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
                index++;
            }
        }

        private void ApplyTransportPort(ushort requestedPort)
        {
            ushort port = requestedPort == 0 ? defaultPort : requestedPort;
            defaultPort = port;
            RefreshLocalInviteSummary();
            NotifyConnectionHintsChanged();

            KcpTransport kcpTransport = transport as KcpTransport;
            if (kcpTransport == null)
            {
                kcpTransport = GetComponent<KcpTransport>();
                if (kcpTransport != null)
                {
                    transport = kcpTransport;
                }
            }

            if (kcpTransport != null)
            {
                kcpTransport.port = port;
            }
        }

        private string ParseAddress(string addressInput, ushort explicitPort, out ushort parsedPort)
        {
            string value = string.IsNullOrWhiteSpace(addressInput) ? "localhost" : addressInput.Trim();
            if (explicitPort > 0)
            {
                parsedPort = explicitPort;
                return value;
            }

            if (TrySplitAddressAndPort(value, out string host, out ushort portFromAddress))
            {
                parsedPort = portFromAddress;
                return host;
            }

            parsedPort = defaultPort;
            return value;
        }

        private static bool TrySplitAddressAndPort(string value, out string host, out ushort port)
        {
            host = value;
            port = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();

            if (trimmed.StartsWith("[") && trimmed.Contains("]:"))
            {
                int bracketIndex = trimmed.IndexOf("]:");
                string portPart = trimmed.Substring(bracketIndex + 2);
                if (ushort.TryParse(portPart, out ushort ipv6Port))
                {
                    host = trimmed.Substring(1, bracketIndex - 1);
                    port = ipv6Port;
                    return true;
                }

                return false;
            }

            int firstColon = trimmed.IndexOf(':');
            int lastColon = trimmed.LastIndexOf(':');
            if (firstColon > 0 && firstColon == lastColon)
            {
                string hostPart = trimmed.Substring(0, lastColon).Trim();
                string portPart = trimmed.Substring(lastColon + 1).Trim();
                if (!string.IsNullOrEmpty(hostPart) && ushort.TryParse(portPart, out ushort parsed))
                {
                    host = hostPart;
                    port = parsed;
                    return true;
                }
            }

            return false;
        }

        private void PrepareInternetHosting(ushort port)
        {
            RefreshLocalInviteSummary();
            publicInviteSummary = $"WAN host: resolving public endpoint for UDP {port}...";
            portMappingSummary = "Direct internet play: preparing router mapping.";

            ReleaseMappedPort();

            if (enableAutomaticPortMapping)
            {
                if (TryOpenAutomaticPortMapping(port, out string localAddress, out string reason))
                {
                    mappedPort = port;
                    mappedLocalAddress = localAddress;
                    upnpMappingOpened = true;
                    portMappingSummary = $"Direct internet play: UPnP opened UDP {port} on {localAddress}.";
                }
                else
                {
                    portMappingSummary = $"Direct internet play: automatic UPnP failed ({reason}). Direct join may still work if the router already exposes UDP {port}.";
                }
            }
            else
            {
                portMappingSummary = $"Direct internet play: automatic UPnP disabled. Expose UDP {port} manually if your router blocks inbound traffic.";
            }

            if (publicEndpointRoutine != null)
            {
                StopCoroutine(publicEndpointRoutine);
            }

            publicEndpointRoutine = StartCoroutine(FetchPublicEndpointRoutine(port));
            NotifyConnectionHintsChanged();
        }

        private void ResetInternetHostState()
        {
            ReleaseMappedPort();

            if (publicEndpointRoutine != null)
            {
                StopCoroutine(publicEndpointRoutine);
                publicEndpointRoutine = null;
            }

            RefreshLocalInviteSummary();
            publicInviteSummary = "WAN host: start a host to generate an internet endpoint.";
            portMappingSummary = "Direct internet play idle.";
            NotifyConnectionHintsChanged();
        }

        private void RefreshLocalInviteSummary()
        {
            localInviteSummary = $"LAN host: {ResolveBestLocalIPv4()}:{defaultPort}";
        }

        private void NotifyConnectionHintsChanged()
        {
            ConnectionHintsChanged?.Invoke();
        }

        private IEnumerator FetchPublicEndpointRoutine(ushort port)
        {
            using UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org");
            request.timeout = 6;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string publicAddress = request.downloadHandler.text != null
                    ? request.downloadHandler.text.Trim()
                    : string.Empty;

                if (!string.IsNullOrWhiteSpace(publicAddress))
                {
                    publicInviteSummary = $"WAN host: {publicAddress}:{port}";
                }
                else
                {
                    publicInviteSummary = $"WAN host: public endpoint lookup returned an empty response. Share your public IP and UDP port {port} manually if needed.";
                }
            }
            else
            {
                publicInviteSummary = $"WAN host: public endpoint lookup failed. Share your public IP and UDP port {port} manually if needed.";
            }

            NotifyConnectionHintsChanged();
            publicEndpointRoutine = null;
        }

        private bool TryOpenAutomaticPortMapping(ushort port, out string localAddress, out string failureReason)
        {
            localAddress = ResolveBestLocalIPv4();
            failureReason = "router did not expose a UPnP port mapping service";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                Type natType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
                if (natType == null)
                {
                    failureReason = "NATUPnP COM interface is unavailable on this machine";
                    return false;
                }

                object nat = System.Activator.CreateInstance(natType);
                if (nat == null)
                {
                    failureReason = "failed to create NATUPnP instance";
                    return false;
                }

                object mappingCollection = natType.InvokeMember(
                    "StaticPortMappingCollection",
                    BindingFlags.GetProperty,
                    null,
                    nat,
                    null);

                if (mappingCollection == null)
                {
                    failureReason = "router returned no UPnP mapping collection";
                    return false;
                }

                RemoveAutomaticPortMapping(mappingCollection, port, "UDP");
                mappingCollection.GetType().InvokeMember(
                    "Add",
                    BindingFlags.InvokeMethod,
                    null,
                    mappingCollection,
                    new object[] { port, "UDP", port, localAddress, true, "Asylum Horror Host" });

                return true;
            }
            catch (System.Exception exception)
            {
                failureReason = exception.Message;
                return false;
            }
#else
            failureReason = "automatic UPnP is only available in Windows builds";
            return false;
#endif
        }

        private void ReleaseMappedPort()
        {
            if (!upnpMappingOpened || mappedPort == 0)
            {
                upnpMappingOpened = false;
                mappedPort = 0;
                mappedLocalAddress = null;
                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                Type natType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
                if (natType != null)
                {
                    object nat = System.Activator.CreateInstance(natType);
                    object mappingCollection = natType.InvokeMember(
                        "StaticPortMappingCollection",
                        BindingFlags.GetProperty,
                        null,
                        nat,
                        null);

                    if (mappingCollection != null)
                    {
                        RemoveAutomaticPortMapping(mappingCollection, mappedPort, "UDP");
                    }
                }
            }
            catch
            {
                // Ignore cleanup failures on shutdown/scene changes.
            }
#endif

            upnpMappingOpened = false;
            mappedPort = 0;
            mappedLocalAddress = null;
        }

        private static void RemoveAutomaticPortMapping(object mappingCollection, ushort port, string protocol)
        {
            if (mappingCollection == null)
            {
                return;
            }

            try
            {
                mappingCollection.GetType().InvokeMember(
                    "Remove",
                    BindingFlags.InvokeMethod,
                    null,
                    mappingCollection,
                    new object[] { port, protocol });
            }
            catch
            {
                // Ignore missing mapping cases.
            }
        }
    }
}
