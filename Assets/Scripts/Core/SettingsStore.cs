using System;
using UnityEngine;

namespace AsylumHorror.Core
{
    public static class SettingsStore
    {
        private const string MasterVolumeKey = "asylum.settings.masterVolume";
        private const string SensitivityKey = "asylum.settings.sensitivity";
        private const string FovKey = "asylum.settings.fov";
        private const string SubtitlesKey = "asylum.settings.subtitles";

        private static bool loaded;
        private static float masterVolume = 0.9f;
        private static float lookSensitivityMultiplier = 1f;
        private static float fieldOfView = 75f;
        private static bool subtitlesEnabled = true;

        public static event Action SettingsChanged;

        public static float MasterVolume => masterVolume;
        public static float LookSensitivityMultiplier => lookSensitivityMultiplier;
        public static float FieldOfView => fieldOfView;
        public static bool SubtitlesEnabled => subtitlesEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureLoaded();
            ApplyRuntimeValues();
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume));
            lookSensitivityMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, lookSensitivityMultiplier), 0.35f, 2.5f);
            fieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat(FovKey, fieldOfView), 60f, 100f);
            subtitlesEnabled = PlayerPrefs.GetInt(SubtitlesKey, subtitlesEnabled ? 1 : 0) != 0;
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            SaveAndBroadcast();
        }

        public static void SetLookSensitivityMultiplier(float value)
        {
            EnsureLoaded();
            lookSensitivityMultiplier = Mathf.Clamp(value, 0.35f, 2.5f);
            PlayerPrefs.SetFloat(SensitivityKey, lookSensitivityMultiplier);
            SaveAndBroadcast();
        }

        public static void SetFieldOfView(float value)
        {
            EnsureLoaded();
            fieldOfView = Mathf.Clamp(value, 60f, 100f);
            PlayerPrefs.SetFloat(FovKey, fieldOfView);
            SaveAndBroadcast();
        }

        public static void SetSubtitlesEnabled(bool enabled)
        {
            EnsureLoaded();
            subtitlesEnabled = enabled;
            PlayerPrefs.SetInt(SubtitlesKey, subtitlesEnabled ? 1 : 0);
            SaveAndBroadcast();
        }

        public static void ApplyRuntimeValues()
        {
            AudioListener.volume = masterVolume;
        }

        private static void SaveAndBroadcast()
        {
            PlayerPrefs.Save();
            ApplyRuntimeValues();
            SettingsChanged?.Invoke();
        }
    }
}
