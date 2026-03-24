using System;
using System.Linq;
using AsylumHorror.Monster;
using AsylumHorror.Network;
using AsylumHorror.Player;
using AsylumHorror.Tasks;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Core
{
    public class GameStateManager : NetworkBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public static event Action ObjectivesChanged;
        public static event Action<bool> RoundEnded;

        [Header("Objectives")]
        [SerializeField] private int requiredGenerators = 2;

        [Header("Round")]
        [SerializeField] private float returnToLobbyDelay = 12f;
        [SerializeField] private RoundRandomizer roundRandomizer;

        [SyncVar(hook = nameof(OnGeneratorsChanged))] private int generatorsCompleted;
        [SyncVar(hook = nameof(OnKeycardChanged))] private bool keycardCollected;
        [SyncVar(hook = nameof(OnPowerChanged))] private bool powerRestored;
        [SyncVar(hook = nameof(OnExitChanged))] private bool exitOpened;
        [SyncVar(hook = nameof(OnRoundOverSyncChanged))] private bool isRoundOver;
        [SyncVar] private bool didPlayersWin;
        [SyncVar] private int escapedPlayers;
        [SyncVar] private double returnToLobbyAtNetworkTime;

        private ExitDoorTask exitDoor;

        public int RequiredGenerators => requiredGenerators;
        public int GeneratorsCompleted => generatorsCompleted;
        public bool KeycardCollected => keycardCollected;
        public bool PowerRestored => powerRestored;
        public bool ExitOpened => exitOpened;
        public bool IsRoundOver => isRoundOver;
        public bool DidPlayersWin => didPlayersWin;

        public bool AreCoreTasksCompleted =>
            generatorsCompleted >= requiredGenerators &&
            keycardCollected &&
            powerRestored;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ObjectivesChanged?.Invoke();
        }

        [ServerCallback]
        private void Update()
        {
            if (!isRoundOver)
            {
                return;
            }

            if (NetworkTime.time >= returnToLobbyAtNetworkTime)
            {
                HorrorNetworkManager.Instance?.ServerReturnToLobby();
            }
        }

        [Server]
        public void ServerInitializeRound()
        {
            generatorsCompleted = 0;
            keycardCollected = false;
            powerRestored = false;
            exitOpened = false;
            escapedPlayers = 0;
            didPlayersWin = false;
            isRoundOver = false;
            returnToLobbyAtNetworkTime = 0;

            if (roundRandomizer == null)
            {
                roundRandomizer = FindAnyObjectByType<RoundRandomizer>();
            }

            roundRandomizer?.ServerRandomizeLayout();

            foreach (GeneratorTask generatorTask in FindObjectsByType<GeneratorTask>())
            {
                generatorTask.ServerResetState();
            }

            foreach (KeycardTask keycardTask in FindObjectsByType<KeycardTask>())
            {
                keycardTask.ServerResetState();
            }

            foreach (PowerRestoreTask powerTask in FindObjectsByType<PowerRestoreTask>())
            {
                powerTask.ServerResetState();
            }

            foreach (BatteryPickupTask batteryTask in FindObjectsByType<BatteryPickupTask>())
            {
                batteryTask.ServerResetState();
            }

            foreach (ExitDoorTask doorTask in FindObjectsByType<ExitDoorTask>())
            {
                doorTask.ServerResetState();
                exitDoor = doorTask;
            }

            foreach (HookPoint hookPoint in FindObjectsByType<HookPoint>())
            {
                hookPoint.ServerResetState();
            }

            foreach (NetworkPlayerStatus player in FindObjectsByType<NetworkPlayerStatus>())
            {
                player.ServerResetForRound();
            }

            foreach (MonsterAI monster in FindObjectsByType<MonsterAI>())
            {
                monster.ServerResetForRound();
            }
        }

        [Server]
        public void ServerRegisterExitDoor(ExitDoorTask doorTask)
        {
            exitDoor = doorTask;
        }

        [Server]
        public void ServerMarkGeneratorActivated()
        {
            if (isRoundOver)
            {
                return;
            }

            generatorsCompleted = Mathf.Clamp(generatorsCompleted + 1, 0, requiredGenerators);
            ServerTryOpenExit();
        }

        [Server]
        public void ServerMarkKeycardCollected()
        {
            if (isRoundOver || keycardCollected)
            {
                return;
            }

            keycardCollected = true;
            ServerTryOpenExit();
        }

        [Server]
        public void ServerMarkPowerRestored()
        {
            if (isRoundOver || powerRestored)
            {
                return;
            }

            powerRestored = true;
            ServerTryOpenExit();
        }

        [Server]
        public void ServerTryEscape(NetworkPlayerStatus player)
        {
            if (isRoundOver ||
                !exitOpened ||
                player == null ||
                player.Condition == PlayerCondition.Dead ||
                player.Condition == PlayerCondition.Escaped)
            {
                return;
            }

            player.ServerSetEscaped();
            escapedPlayers++;
            ServerEndRound(true);
        }

        [Server]
        public void ServerEvaluateRoundState()
        {
            if (isRoundOver)
            {
                return;
            }

            if (escapedPlayers > 0)
            {
                ServerEndRound(true);
                return;
            }

            NetworkPlayerStatus[] players = FindObjectsByType<NetworkPlayerStatus>();
            if (players.Length == 0)
            {
                ServerEndRound(false);
                return;
            }

            bool anySurvivor = players.Any(player =>
                player.Condition != PlayerCondition.Dead &&
                player.Condition != PlayerCondition.Escaped);

            if (!anySurvivor)
            {
                ServerEndRound(false);
            }
        }

        public string BuildObjectivesText()
        {
            string objective1 = $"Generators: {generatorsCompleted}/{requiredGenerators}";
            string objective2 = $"Keycard: {(keycardCollected ? "Found" : "Missing")}";
            string objective3 = $"Power Restore: {(powerRestored ? "Done" : "Pending")}";
            string objective4 = $"Main Exit: {(exitOpened ? "OPEN" : "LOCKED")}";
            string objective5 = "Goal: At least one survivor escapes";
            return $"{objective1}\n{objective2}\n{objective3}\n{objective4}\n{objective5}";
        }

        [Server]
        private void ServerTryOpenExit()
        {
            if (isRoundOver || exitOpened || !AreCoreTasksCompleted)
            {
                return;
            }

            exitOpened = true;
            if (exitDoor != null)
            {
                exitDoor.ServerForceOpen();
            }
        }

        [Server]
        private void ServerEndRound(bool playersWon)
        {
            if (isRoundOver)
            {
                return;
            }

            didPlayersWin = playersWon;
            isRoundOver = true;
            returnToLobbyAtNetworkTime = NetworkTime.time + returnToLobbyDelay;
        }

        private void OnGeneratorsChanged(int _, int __) => ObjectivesChanged?.Invoke();
        private void OnKeycardChanged(bool _, bool __) => ObjectivesChanged?.Invoke();
        private void OnPowerChanged(bool _, bool __) => ObjectivesChanged?.Invoke();
        private void OnExitChanged(bool _, bool __) => ObjectivesChanged?.Invoke();

        private void OnRoundOverSyncChanged(bool _, bool currentValue)
        {
            if (currentValue)
            {
                RoundEnded?.Invoke(didPlayersWin);
            }
        }
    }
}
