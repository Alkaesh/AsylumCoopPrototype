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
                string traumaTag = localStatus.HasRescueTrauma ? " | Shocked" : string.Empty;
                string focusTag = localStatus.FocusAbilityActive ? " | Focused" : string.Empty;
                string hiddenTag = localStatus.IsHidden ? " | Hidden" : string.Empty;
                statusText.text = $"State: {localStatus.Condition}{traumaTag}{focusTag}{hiddenTag}";
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

            if (abilityText != null)
            {
                if (localStatus.Condition == PlayerCondition.Hooked)
                {
                    abilityText.text = localStatus.HookSelfEscapeAvailable
                        ? "F: Tempt Fate"
                        : "Fate chance spent";
                }
                else
                {
                    float cooldown = localStatus.FocusAbilityCooldownRemaining;
                    abilityText.text = cooldown <= 0.01f
                        ? "Q: Steady Hands ready"
                        : $"Q: Steady Hands {cooldown:0}s";
                }
            }

            float stress01 = localStress != null ? localStress.CurrentStress01 : 0f;
            float reveal01 = localStress != null ? localStress.RevealFlash01 : 0f;
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
                        ? "F: SPIN FOR 25% ESCAPE"
                        : "NO MORE LUCK THIS HOOK";
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
                builder.Append(marker).Append(": ").Append(player.Condition).Append('\n');
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
    }
}
