using AsylumHorror.Core;
using AsylumHorror.Player;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace AsylumHorror.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField] private Text objectivesText;
        [SerializeField] private Text roundResultText;
        [SerializeField] private Text roundSummaryText;

        [Header("Interaction")]
        [SerializeField] private Text interactionText;
        [SerializeField] private Slider interactionHoldSlider;
        [SerializeField] private Text abilityText;

        [Header("Player")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Slider batterySlider;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider hookTimerSlider;
        [SerializeField] private RectTransform hookWheelRoot;
        [SerializeField] private RectTransform hookWheelPointer;
        [SerializeField] private Text hookWheelText;
        [SerializeField] private Text teammatesText;
        [SerializeField] private Image stressOverlay;
        [SerializeField] private Image revealFlashOverlay;

        private NetworkPlayerController localController;
        private NetworkPlayerStatus localStatus;
        private PlayerFlashlight localFlashlight;
        private PlayerStressController localStress;
        private float nextTeamRefreshTime;

        private void OnEnable()
        {
            GameStateManager.ObjectivesChanged += RefreshObjectives;
            GameStateManager.RoundEnded += OnRoundEnded;
        }

        private void OnDisable()
        {
            GameStateManager.ObjectivesChanged -= RefreshObjectives;
            GameStateManager.RoundEnded -= OnRoundEnded;
        }

        private void Start()
        {
            RefreshObjectives();
            SetInteractionPrompt(string.Empty, 0f);
            if (roundResultText != null)
            {
                roundResultText.text = string.Empty;
            }

            if (roundSummaryText != null)
            {
                roundSummaryText.text = string.Empty;
            }

            if (hookTimerSlider != null)
            {
                hookTimerSlider.gameObject.SetActive(false);
            }

            if (hookWheelRoot != null)
            {
                hookWheelRoot.gameObject.SetActive(false);
            }

            ApplyStressVisuals(0f, 0f);
        }

        private void Update()
        {
            TryBindLocalPlayer();
            RefreshLocalPlayerPanels();

            if (Time.unscaledTime >= nextTeamRefreshTime)
            {
                nextTeamRefreshTime = Time.unscaledTime + 0.25f;
                RefreshTeammatesPanel();
            }
        }

        public void SetInteractionPrompt(string prompt, float hold01)
        {
            if (interactionText != null)
            {
                interactionText.text = prompt;
            }

            if (interactionHoldSlider != null)
            {
                bool showHold = !string.IsNullOrWhiteSpace(prompt) && hold01 > 0.001f;
                interactionHoldSlider.gameObject.SetActive(showHold);
                interactionHoldSlider.value = hold01;
            }
        }

        private void RefreshObjectives()
        {
            if (objectivesText == null)
            {
                return;
            }

            GameStateManager gameState = GameStateManager.Instance;
            objectivesText.text = gameState != null
                ? gameState.BuildObjectivesText()
                : "Waiting for game state...";
        }

        private void RefreshLocalPlayerPanels()
        {
            if (localController == null || localStatus == null)
            {
                ApplyStressVisuals(0f, 0f);
                return;
            }

            if (staminaSlider != null)
            {
                staminaSlider.value = localController.Stamina01;
            }

            if (batterySlider != null && localFlashlight != null)
            {
                batterySlider.value = localFlashlight.Battery01;
            }

            if (statusText != null)
            {
                statusText.text = BuildLocalStatusText();
            }

            if (hookTimerSlider != null)
            {
                bool showHook = localStatus.Condition == PlayerCondition.Hooked;
                hookTimerSlider.gameObject.SetActive(showHook);
                if (showHook)
                {
                    hookTimerSlider.value = Mathf.Clamp01(localStatus.HookRemainingTime / 45f);
                }
            }

            if (hookWheelRoot != null)
            {
                bool showWheel = localStatus.Condition == PlayerCondition.Hooked;
                hookWheelRoot.gameObject.SetActive(showWheel);
                if (showWheel)
                {
                    UpdateHookWheel(localStatus);
                }
            }

            float stress01 = localStress != null ? localStress.CurrentStress01 : 0f;
            float reveal01 = localStress != null ? localStress.RevealFlash01 : 0f;

            if (abilityText != null)
            {
                abilityText.text = BuildAbilityText(stress01);
            }
            ApplyStressVisuals(stress01, reveal01);
        }

        private void TryBindLocalPlayer()
        {
            if (localController != null && localController.isLocalPlayer)
            {
                return;
            }

            foreach (NetworkPlayerController controller in FindObjectsByType<NetworkPlayerController>())
            {
                if (!controller.isLocalPlayer)
                {
                    continue;
                }

                localController = controller;
                localStatus = controller.GetComponent<NetworkPlayerStatus>();
                localFlashlight = controller.GetComponent<PlayerFlashlight>();
                localStress = controller.GetComponent<PlayerStressController>();
                return;
            }
        }

        private void ApplyStressVisuals(float stress01, float reveal01)
        {
            if (stressOverlay != null)
            {
                Color color = stressOverlay.color;
                color.a = Mathf.Lerp(0f, 0.24f, Mathf.Clamp01(stress01));
                stressOverlay.color = color;
            }

            if (revealFlashOverlay != null)
            {
                Color color = revealFlashOverlay.color;
                color.a = Mathf.Lerp(0f, 0.42f, Mathf.Clamp01(reveal01));
                revealFlashOverlay.color = color;
            }
        }

        private void UpdateHookWheel(NetworkPlayerStatus status)
        {
            if (hookWheelPointer != null)
            {
                float speed = status.HookSelfEscapeRolling ? 720f : 210f;
                hookWheelPointer.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * speed);
            }

            if (hookWheelText == null)
            {
                return;
            }

            if (status.HookSelfEscapeRolling)
            {
                hookWheelText.text = $"FATE TURNING...\n{status.HookSelfEscapeResolveRemaining:0.0}s";
                return;
            }

            switch (status.CurrentHookSelfEscapeOutcome)
            {
                case HookSelfEscapeOutcome.Success:
                    hookWheelText.text = "THE HOOK BROKE";
                    break;
                case HookSelfEscapeOutcome.Failed:
                    hookWheelText.text = "FATE FAILED";
                    break;
                default:
                    hookWheelText.text = status.HookSelfEscapeAvailable
                        ? "F: TEMPT FATE"
                        : "FATE IS SPENT";
                    break;
            }
        }

        private void OnRoundEnded(bool playersWon)
        {
            if (roundResultText != null)
            {
                roundResultText.text = playersWon ? "YOU SURVIVED" : "ALL PLAYERS LOST";
            }

            string summary = BuildRoundSummary();
            if (roundSummaryText != null)
            {
                roundSummaryText.text = summary;
            }

            RoundPresentationMemory.Store(playersWon ? "SURVIVORS BREACHED THE EXIT" : "FACILITY CLAIMED THE TEAM", summary);
        }

        private void RefreshTeammatesPanel()
        {
            if (teammatesText == null)
            {
                return;
            }

            NetworkPlayerStatus[] players = FindObjectsByType<NetworkPlayerStatus>();
            if (players == null || players.Length == 0)
            {
                teammatesText.text = string.Empty;
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("Team\n");
            int index = 1;
            foreach (NetworkPlayerStatus player in players)
            {
                string marker = player == localStatus ? "You" : $"P{index}";
                builder.Append(marker).Append(": ").Append(BuildTeammateStateText(player)).Append('\n');
                index++;
            }

            teammatesText.text = builder.ToString().TrimEnd();
        }

        private string BuildRoundSummary()
        {
            NetworkPlayerStatus[] players = FindObjectsByType<NetworkPlayerStatus>();
            if (players == null || players.Length == 0)
            {
                return "No survivor data available.";
            }

            System.Array.Sort(players, (a, b) => a.netId.CompareTo(b.netId));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("Round Summary\n");
            int index = 1;
            foreach (NetworkPlayerStatus player in players)
            {
                string label = player == localStatus ? "YOU" : $"P{index}";
                string outcome = player.Condition switch
                {
                    PlayerCondition.Escaped => "Escaped",
                    PlayerCondition.Dead => "Killed",
                    PlayerCondition.Hooked => "Left On Hook",
                    _ => "Alive"
                };

                builder
                    .Append(label)
                    .Append(" | ")
                    .Append(outcome)
                    .Append(" | Hooked ")
                    .Append(player.TimesHooked)
                    .Append(" | Saved ")
                    .Append(player.TimesSavedAllies)
                    .Append('\n');
                index++;
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildLocalStatusText()
        {
            if (localStatus == null)
            {
                return string.Empty;
            }

            if (localStatus.IsHidden)
            {
                return "Status: Holding breath";
            }

            return localStatus.Condition switch
            {
                PlayerCondition.Healthy when localStatus.FocusAbilityActive => "Status: Hands steady",
                PlayerCondition.Healthy => "Status: Stable",
                PlayerCondition.Injured when localStatus.HasRescueTrauma => "Status: Wounded and shaking",
                PlayerCondition.Injured => "Status: Wounded",
                PlayerCondition.Knocked => "Status: Down",
                PlayerCondition.Carried => "Status: Taken",
                PlayerCondition.Hooked => "Status: On the hook",
                PlayerCondition.Dead => "Status: Lost",
                PlayerCondition.Escaped => "Status: Out",
                _ => "Status: Strained"
            };
        }

        private string BuildAbilityText(float stress01)
        {
            if (localStatus == null)
            {
                return string.Empty;
            }

            if (localStatus.Condition == PlayerCondition.Hooked)
            {
                return localStatus.HookSelfEscapeAvailable
                    ? "F: Tempt fate"
                    : "Fate spent";
            }

            if (!localStatus.CanControlCharacter)
            {
                return string.Empty;
            }

            float cooldown = localStatus.FocusAbilityCooldownRemaining;
            bool pressureWindow = stress01 > 0.25f || localStatus.HasRescueTrauma;

            if (localStatus.FocusAbilityActive)
            {
                return "Q: Steady hands";
            }

            if (cooldown <= 0.01f)
            {
                return pressureWindow ? "Q: Steady yourself" : string.Empty;
            }

            return pressureWindow ? "Steady hands recovering" : string.Empty;
        }

        private static string BuildTeammateStateText(NetworkPlayerStatus player)
        {
            if (player == null)
            {
                return "Lost";
            }

            return player.Condition switch
            {
                PlayerCondition.Healthy => "Active",
                PlayerCondition.Injured => "Wounded",
                PlayerCondition.Knocked => "Down",
                PlayerCondition.Carried => "Taken",
                PlayerCondition.Hooked => "Hooked",
                PlayerCondition.Dead => "Lost",
                PlayerCondition.Escaped => "Out",
                _ => "Unknown"
            };
        }
    }
}
