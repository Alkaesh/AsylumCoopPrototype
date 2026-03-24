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
        [SyncVar(hook = nameof(OnRouteChanged))] private RoundRouteKind activeRouteKind;
        [SyncVar(hook = nameof(OnRoundOverSyncChanged))] private bool isRoundOver;
        [SyncVar] private bool didPlayersWin;
        [SyncVar] private int escapedPlayers;
        [SyncVar] private double returnToLobbyAtNetworkTime;
        [SyncVar(hook = nameof(OnRoundStartedChanged))] private double roundStartedAtNetworkTime;

        private ExitDoorTask exitDoor;

        public int RequiredGenerators => requiredGenerators;
        public int GeneratorsCompleted => generatorsCompleted;
        public bool KeycardCollected => keycardCollected;
        public bool PowerRestored => powerRestored;
        public bool ExitOpened => exitOpened;
        public bool IsRoundOver => isRoundOver;
        public bool DidPlayersWin => didPlayersWin;
        public RoundRouteKind ActiveRouteKind => activeRouteKind;
        public float RoundElapsedSeconds =>
            roundStartedAtNetworkTime <= 0d
                ? 0f
                : Mathf.Max(0f, (float)(NetworkTime.time - roundStartedAtNetworkTime));
        public RoundObjectivePhase CurrentObjectivePhase
        {
            get
            {
                if (generatorsCompleted < requiredGenerators)
                {
                    return RoundObjectivePhase.RestoreAuxiliaryPower;
                }

                if (exitOpened)
                {
                    return RoundObjectivePhase.Escape;
                }

                if (!keycardCollected)
                {
                    return RoundObjectivePhase.FindAccessKey;
                }

                return powerRestored ? RoundObjectivePhase.Escape : RoundObjectivePhase.RestoreMainPower;
            }
        }

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
            roundStartedAtNetworkTime = NetworkTime.time;

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
        public void ServerSetActiveRoute(RoundRouteKind routeKind)
        {
            activeRouteKind = routeKind;
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
            string directive = BuildDirectiveLine();
            string clue = BuildClueLine();
            string progress = BuildProgressLine();

            return string.IsNullOrWhiteSpace(progress)
                ? $"{directive}\n{clue}"
                : $"{directive}\n{clue}\n{progress}";
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
        private void OnRouteChanged(RoundRouteKind _, RoundRouteKind __) => ObjectivesChanged?.Invoke();
        private void OnRoundStartedChanged(double _, double __) => ObjectivesChanged?.Invoke();

        private void OnRoundOverSyncChanged(bool _, bool currentValue)
        {
            if (currentValue)
            {
                RoundEnded?.Invoke(didPlayersWin);
            }
        }

        private string BuildDirectiveLine()
        {
            return CurrentObjectivePhase switch
            {
                RoundObjectivePhase.RestoreAuxiliaryPower => "Directive: Restart auxiliary power",
                RoundObjectivePhase.FindAccessKey => "Directive: Find staff clearance",
                RoundObjectivePhase.RestoreMainPower => "Directive: Route power to the main lock",
                RoundObjectivePhase.Escape => "Directive: Get someone to the front breach",
                _ => "Directive: Stay alive"
            };
        }

        private string BuildClueLine()
        {
            return CurrentObjectivePhase switch
            {
                RoundObjectivePhase.RestoreAuxiliaryPower => $"Clue: {ResolveRouteStartClue()}",
                RoundObjectivePhase.FindAccessKey => $"Clue: {ResolveKeycardClue()}",
                RoundObjectivePhase.RestoreMainPower => $"Clue: {ResolvePowerClue()}",
                RoundObjectivePhase.Escape => "Clue: The route back north is open, but no space stays safe for long",
                _ => "Clue: Listen before you move"
            };
        }

        private string BuildProgressLine()
        {
            return CurrentObjectivePhase switch
            {
                RoundObjectivePhase.RestoreAuxiliaryPower => BuildGeneratorPressureLine(),
                RoundObjectivePhase.FindAccessKey => "Keep noise low. Closed sections still want the card.",
                RoundObjectivePhase.RestoreMainPower => "The final lock is still dead.",
                RoundObjectivePhase.Escape => "The breach is open. Moving first matters more than moving fast.",
                _ => string.Empty
            };
        }

        private string BuildGeneratorPressureLine()
        {
            int remaining = Mathf.Max(0, requiredGenerators - generatorsCompleted);
            return remaining switch
            {
                0 => "Auxiliary breakers are holding. The deeper lock can wake now.",
                1 => "One more breaker has to hold.",
                _ => "Most of the wing is still dead."
            };
        }

        private string ResolveRouteStartClue()
        {
            return activeRouteKind switch
            {
                RoundRouteKind.WestDescent => "Take the west offices and records wing first",
                RoundRouteKind.EastDescent => "Push through the east wards and operating wing",
                RoundRouteKind.CrossCurrent => "Split the central hall and wake both mid wings",
                _ => "Feel out the nearest live corridor"
            };
        }

        private string ResolveKeycardClue()
        {
            return activeRouteKind switch
            {
                RoundRouteKind.WestDescent => "Security clearance was abandoned somewhere along the west route",
                RoundRouteKind.EastDescent => "Look for clearance along the east medical offices",
                RoundRouteKind.CrossCurrent => "The master card should still be near the front watch post",
                _ => "The card was not left far from the staffed wing"
            };
        }

        private string ResolvePowerClue()
        {
            return activeRouteKind switch
            {
                RoundRouteKind.WestDescent => "Follow the west service lines to the breaker cage",
                RoundRouteKind.EastDescent => "The east maintenance switchboard still has life in it",
                RoundRouteKind.CrossCurrent => "The central relay sits past the southern cross-corridor",
                _ => "The final relay is deeper than the first locks"
            };
        }
    }
}
