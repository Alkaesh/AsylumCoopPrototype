using AsylumHorror.Network;
using AsylumHorror.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AsylumHorror.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Text settingsSummaryText;

        private HorrorNetworkManager networkManager;

        private void OnEnable()
        {
            if (networkManager == null)
            {
                networkManager = ResolveNetworkManager();
            }

            if (networkManager != null)
            {
                networkManager.ConnectionHintsChanged += RefreshNetworkHints;
            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.ConnectionHintsChanged -= RefreshNetworkHints;
            }
        }

        private void Awake()
        {
            networkManager = ResolveNetworkManager();
        }

        private void Start()
        {
            if (networkManager == null)
            {
                networkManager = ResolveNetworkManager();
            }

            if (networkManager != null && portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            {
                portInput.text = networkManager.DefaultPort.ToString();
            }

            if (networkManager != null)
            {
                RefreshNetworkHints();
            }

            SettingsStore.EnsureLoaded();
            ApplySettingsToWidgets();
        }

        public void OnHostClicked()
        {
            if (networkManager == null)
            {
                networkManager = ResolveNetworkManager();
            }

            if (networkManager == null)
            {
                SetStatus("NetworkManager not found.");
                return;
            }

            ushort port = ParsePortOrDefault(networkManager.DefaultPort);
            networkManager.StartHostFromMenu(port);
            RefreshNetworkHints();
        }

        public void OnJoinClicked()
        {
            if (networkManager == null)
            {
                networkManager = HorrorNetworkManager.Instance;
            }

            if (networkManager == null)
            {
                SetStatus("NetworkManager not found.");
                return;
            }

            string address = addressInput != null ? addressInput.text : "localhost";
            ushort? optionalPort = ParsePortNullable();
            if (optionalPort.HasValue)
            {
                networkManager.StartClientFromMenu(address, optionalPort.Value);
                SetStatus($"Connecting to {address}:{optionalPort.Value}...");
                return;
            }

            networkManager.StartClientFromMenu(address);
            SetStatus($"Connecting to {address}...");
        }

        public void OnQuitClicked()
        {
            Application.Quit();
        }

        public void OnToggleSettingsClicked()
        {
            if (settingsPanel == null)
            {
                return;
            }

            settingsPanel.SetActive(!settingsPanel.activeSelf);
            ApplySettingsToWidgets();
        }

        public void OnMasterVolumeChanged(float value)
        {
            SettingsStore.SetMasterVolume(value);
            RefreshSettingsSummary();
        }

        public void OnSensitivityChanged(float value)
        {
            SettingsStore.SetLookSensitivityMultiplier(value);
            RefreshSettingsSummary();
        }

        public void OnFovChanged(float value)
        {
            SettingsStore.SetFieldOfView(value);
            RefreshSettingsSummary();
        }

        public void OnSubtitlesChanged(bool enabled)
        {
            SettingsStore.SetSubtitlesEnabled(enabled);
            RefreshSettingsSummary();
        }

        public void OnOpenMenuSceneClicked(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
        }

        private void RefreshNetworkHints()
        {
            if (networkManager == null)
            {
                networkManager = ResolveNetworkManager();
            }

            if (networkManager != null)
            {
                SetStatus(networkManager.BuildConnectionHint());
            }
        }

        private void ApplySettingsToWidgets()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = SettingsStore.MasterVolume;
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = SettingsStore.LookSensitivityMultiplier;
            }

            if (fovSlider != null)
            {
                fovSlider.value = SettingsStore.FieldOfView;
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.isOn = SettingsStore.SubtitlesEnabled;
            }

            RefreshSettingsSummary();
        }

        private void RefreshSettingsSummary()
        {
            if (settingsSummaryText == null)
            {
                return;
            }

            settingsSummaryText.text =
                $"Volume {Mathf.RoundToInt(SettingsStore.MasterVolume * 100f)}% | " +
                $"Sensitivity x{SettingsStore.LookSensitivityMultiplier:0.00} | " +
                $"FOV {SettingsStore.FieldOfView:0} | " +
                $"Subtitles {(SettingsStore.SubtitlesEnabled ? "On" : "Off")}";
        }

        private ushort ParsePortOrDefault(ushort fallback)
        {
            ushort? parsed = ParsePortNullable();
            return parsed ?? fallback;
        }

        private ushort? ParsePortNullable()
        {
            if (portInput == null || string.IsNullOrWhiteSpace(portInput.text))
            {
                return null;
            }

            if (ushort.TryParse(portInput.text.Trim(), out ushort port))
            {
                return port;
            }

            return null;
        }

        private static HorrorNetworkManager ResolveNetworkManager()
        {
            HorrorNetworkManager manager = HorrorNetworkManager.Instance;
            if (manager != null)
            {
                return manager;
            }

            return FindAnyObjectByType<HorrorNetworkManager>();
        }
    }
}
