using System;
using AsylumHorror.Audio;
using AsylumHorror.Monster;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(NetworkPlayerStatus))]
    public class PlayerFlashlight : NetworkBehaviour
    {
        [SerializeField] private Light[] flashlightLights;
        [SerializeField] private float maxBatterySeconds = 180f;
        [SerializeField] private float drainPerSecond = 1f;
        [SerializeField] private float rechargePerSecond = 0.15f;
        [SerializeField] private float threatFlickerDistance = 14f;
        [SerializeField] private float dangerFlickerDistance = 8f;
        [SerializeField] private AudioSource toggleAudioSource;
        [SerializeField] private AudioClip toggleOnClip;
        [SerializeField] private AudioClip toggleOffClip;

        [SyncVar(hook = nameof(OnFlashlightStateChanged))] private bool isOn = true;
        [SyncVar(hook = nameof(OnBatteryValueChanged))] private float batterySeconds = 180f;

        private NetworkPlayerStatus playerStatus;
        private PlayerStressController stressController;
        private float[] baseLightIntensities;
        private float flickerSeed;

        public event Action<float> BatteryChanged;
        public event Action<bool> FlashlightStateChanged;

        public bool IsOn => isOn;
        public float Battery01 => maxBatterySeconds <= 0f ? 0f : Mathf.Clamp01(batterySeconds / maxBatterySeconds);
        public float BatterySeconds => batterySeconds;

        private void Awake()
        {
            playerStatus = GetComponent<NetworkPlayerStatus>();
            stressController = GetComponent<PlayerStressController>();
            if (flashlightLights == null || flashlightLights.Length == 0)
            {
                flashlightLights = GetComponentsInChildren<Light>(true);
            }

            if (flashlightLights != null && flashlightLights.Length > 0)
            {
                baseLightIntensities = new float[flashlightLights.Length];
                for (int i = 0; i < flashlightLights.Length; i++)
                {
                    baseLightIntensities[i] = flashlightLights[i] != null ? flashlightLights[i].intensity : 1f;
                }
            }

            flickerSeed = UnityEngine.Random.Range(0f, 999f);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            batterySeconds = maxBatterySeconds;
        }

        private void Start()
        {
            ApplyVisualState(isOn);
        }

        [ServerCallback]
        private void Update()
        {
            if (isOn)
            {
                batterySeconds = Mathf.Max(0f, batterySeconds - drainPerSecond * Time.deltaTime);
                if (batterySeconds <= 0f)
                {
                    isOn = false;
                }
            }
            else
            {
                batterySeconds = Mathf.Min(maxBatterySeconds, batterySeconds + rechargePerSecond * Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                CmdToggleFlashlight();
            }

            UpdateThreatFlicker();
        }

        [Command]
        private void CmdToggleFlashlight()
        {
            if (!playerStatus.CanControlCharacter)
            {
                return;
            }

            if (!isOn && batterySeconds <= 0f)
            {
                return;
            }

            isOn = !isOn;
        }

        [Server]
        public void ServerRechargeBattery(float seconds)
        {
            float safeSeconds = Mathf.Max(0f, seconds);
            if (safeSeconds <= 0f)
            {
                return;
            }

            batterySeconds = Mathf.Min(maxBatterySeconds, batterySeconds + safeSeconds);
            if (batterySeconds > 0.2f)
            {
                isOn = true;
            }
        }

        private void OnFlashlightStateChanged(bool previousState, bool nextState)
        {
            ApplyVisualState(nextState);
            FlashlightStateChanged?.Invoke(nextState);
            PlayToggleSound(nextState, previousState);
        }

        private void OnBatteryValueChanged(float _, float __)
        {
            BatteryChanged?.Invoke(Battery01);
        }

        private void ApplyVisualState(bool enabledState)
        {
            if (flashlightLights == null)
            {
                return;
            }

            for (int i = 0; i < flashlightLights.Length; i++)
            {
                Light flashlight = flashlightLights[i];
                if (flashlight != null)
                {
                    flashlight.enabled = enabledState;
                    if (enabledState && baseLightIntensities != null && i < baseLightIntensities.Length)
                    {
                        flashlight.intensity = baseLightIntensities[i];
                    }
                }
            }
        }

        private void PlayToggleSound(bool nextState, bool previousState)
        {
            if (toggleAudioSource == null || nextState == previousState)
            {
                return;
            }

            AudioClip clip = nextState ? toggleOnClip : toggleOffClip;
            if (clip != null)
            {
                toggleAudioSource.PlayOneShot(clip);
            }
        }

        private void UpdateThreatFlicker()
        {
            if (!isOn || flashlightLights == null || flashlightLights.Length == 0)
            {
                return;
            }

            MonsterAI monster = MonsterAI.Instance;
            float stress01 = stressController != null ? stressController.FlashlightInstability01 : 0f;
            float traumaMultiplier = playerStatus != null ? playerStatus.FlashlightInstabilityMultiplier : 1f;
            float distance = monster != null
                ? Vector3.Distance(transform.position, monster.transform.position)
                : threatFlickerDistance + 1f;
            if (distance > threatFlickerDistance && stress01 <= 0.01f)
            {
                RestoreBaseLightIntensity();
                return;
            }

            float threat01 = 1f - Mathf.InverseLerp(threatFlickerDistance, dangerFlickerDistance, distance);
            if (monster != null && (monster.CurrentState == MonsterState.Chase || monster.CurrentState == MonsterState.Attack))
            {
                threat01 = Mathf.Clamp01(threat01 + 0.32f);
            }

            threat01 = Mathf.Clamp01(Mathf.Max(threat01, stress01 * 0.82f) * traumaMultiplier);

            for (int i = 0; i < flashlightLights.Length; i++)
            {
                Light flashlight = flashlightLights[i];
                if (flashlight == null)
                {
                    continue;
                }

                float baseIntensity = baseLightIntensities != null && i < baseLightIntensities.Length
                    ? baseLightIntensities[i]
                    : flashlight.intensity;

                float noise = Mathf.PerlinNoise(flickerSeed + i * 1.7f, Time.time * (11f + threat01 * 24f));
                float harshDrop = noise < Mathf.Lerp(0.08f, 0.36f, threat01) ? 0.1f : 1f;
                float flutter = Mathf.Lerp(0.92f, 0.28f + noise * 0.68f, threat01);
                flashlight.intensity = Mathf.Max(0.05f, baseIntensity * flutter * harshDrop);
            }
        }

        private void RestoreBaseLightIntensity()
        {
            if (flashlightLights == null || baseLightIntensities == null)
            {
                return;
            }

            for (int i = 0; i < flashlightLights.Length && i < baseLightIntensities.Length; i++)
            {
                if (flashlightLights[i] != null)
                {
                    flashlightLights[i].intensity = baseLightIntensities[i];
                }
            }
        }
    }
}
