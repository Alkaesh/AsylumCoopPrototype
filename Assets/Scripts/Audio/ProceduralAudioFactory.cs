using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsylumHorror.Audio
{
    public static class ProceduralAudioFactory
    {
        private const int SampleRate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip GetAmbientLoop()
        {
            return GetOrCreate("ambient_loop", () => CreateDrone("AmbientLoop", 12f, 46f, 58f, 0.14f));
        }

        public static AudioClip GetAmbientStinger(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, 5);
            return GetOrCreate($"ambient_stinger_{safeIndex}", () =>
                CreateSweep($"AmbientStinger_{safeIndex}", 1.4f, 320f + safeIndex * 28f, 70f, 0.22f));
        }

        public static AudioClip GetHeartbeatLoop()
        {
            return GetOrCreate("heartbeat_loop", () => CreateHeartbeat("HeartbeatLoop", 2f, 0.34f));
        }

        public static AudioClip GetBreathingLoop()
        {
            return GetOrCreate("breathing_loop", () => CreateBreathing("BreathingLoop", 3.8f, 0.3f));
        }

        public static AudioClip GetChaseLoop()
        {
            return GetOrCreate("chase_loop", () => CreateDrone("ChaseLoop", 8f, 70f, 92f, 0.2f));
        }

        public static AudioClip GetMonsterPatrolLoop()
        {
            return GetOrCreate("monster_patrol", () => CreateDrone("MonsterPatrol", 4.5f, 38f, 51f, 0.17f));
        }

        public static AudioClip GetMonsterChaseLoop()
        {
            return GetOrCreate("monster_chase", () => CreateDrone("MonsterChase", 4.5f, 58f, 73f, 0.24f));
        }

        public static AudioClip GetGeneratorStartClip()
        {
            return GetOrCreate("generator_start", () => CreatePulse("GeneratorStart", 0.9f, 62f, 0.6f));
        }

        public static AudioClip GetPowerRestoreClip()
        {
            return GetOrCreate("power_restore", () => CreateSweep("PowerRestore", 1.1f, 190f, 620f, 0.26f));
        }

        public static AudioClip GetKeycardPickupClip()
        {
            return GetOrCreate("keycard_pickup", () => CreatePulse("KeycardPickup", 0.18f, 880f, 0.33f));
        }

        public static AudioClip GetDoorOpenClip()
        {
            return GetOrCreate("door_open", () => CreatePulse("DoorOpen", 0.45f, 180f, 0.4f));
        }

        public static AudioClip GetRescueClip()
        {
            return GetOrCreate("rescue_clip", () => CreatePulse("Rescue", 0.42f, 260f, 0.42f));
        }

        public static AudioClip GetFlashlightOnClip()
        {
            return GetOrCreate("flashlight_on", () => CreatePulse("FlashlightOn", 0.12f, 980f, 0.28f));
        }

        public static AudioClip GetFlashlightOffClip()
        {
            return GetOrCreate("flashlight_off", () => CreatePulse("FlashlightOff", 0.14f, 420f, 0.26f));
        }

        public static AudioClip GetFootstepClip(string key, float pitchBase, float volume = 0.6f)
        {
            return GetOrCreate($"footstep_{key}", () => CreateFootstep($"Footstep_{key}", 0.17f, pitchBase, volume));
        }

        private static AudioClip GetOrCreate(string key, Func<AudioClip> factory)
        {
            if (Cache.TryGetValue(key, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            AudioClip created = factory.Invoke();
            Cache[key] = created;
            return created;
        }

        private static AudioClip CreateDrone(string clipName, float durationSeconds, float toneA, float toneB, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float slowLfo = Mathf.Sin(t * 0.35f) * 0.25f + 0.75f;
                float wave = Mathf.Sin(2f * Mathf.PI * toneA * t) * 0.6f +
                             Mathf.Sin(2f * Mathf.PI * toneB * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * (toneA * 0.5f) * t) * 0.2f;
                float noise = (Mathf.PerlinNoise(t * 2.5f, 0.37f) - 0.5f) * 0.4f;
                samples[i] = Mathf.Clamp((wave + noise) * gain * slowLfo, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateHeartbeat(string clipName, float durationSeconds, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float pulseA = Mathf.Exp(-Mathf.Pow((t - 0.18f) * 28f, 2f));
                float pulseB = Mathf.Exp(-Mathf.Pow((t - 0.42f) * 24f, 2f));
                float tone = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.7f +
                             Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f;
                float envelope = pulseA + pulseB;
                samples[i] = Mathf.Clamp(tone * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateBreathing(string clipName, float durationSeconds, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float cycle = Mathf.Sin(t * Mathf.PI * 2f / durationSeconds) * 0.5f + 0.5f;
                float envelope = Mathf.SmoothStep(0.08f, 1f, cycle) * Mathf.SmoothStep(0.35f, 0f, cycle);
                float noise = (Mathf.PerlinNoise(t * 8f, 0.53f) - 0.5f) * 2f;
                float tone = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.22f;
                samples[i] = Mathf.Clamp((noise * 0.45f + tone) * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateFootstep(string clipName, float durationSeconds, float baseFrequency, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 24f);
                float noise = (Mathf.PerlinNoise(t * 210f, 0.91f) - 0.5f) * 2f;
                float tone = Mathf.Sin(2f * Mathf.PI * baseFrequency * t) * 0.25f;
                samples[i] = Mathf.Clamp((noise * 0.85f + tone) * env * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSweep(string clipName, float durationSeconds, float startFrequency, float endFrequency, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float lerp = Mathf.Clamp01(t / durationSeconds);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, lerp);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float envelope = Mathf.SmoothStep(1f, 0f, lerp);
                float noise = (Mathf.PerlinNoise(t * 26f, 0.2f) - 0.5f) * 0.3f;
                samples[i] = Mathf.Clamp((wave * 0.8f + noise) * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreatePulse(string clipName, float durationSeconds, float baseFrequency, float gain)
        {
            int sampleCount = Mathf.CeilToInt(durationSeconds * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float lerp = Mathf.Clamp01(t / durationSeconds);
                float envelope = Mathf.Exp(-lerp * 8f);
                float wobble = Mathf.Sin(2f * Mathf.PI * (baseFrequency * (1f + lerp * 0.2f)) * t);
                float texture = (Mathf.PerlinNoise(t * 48f, 0.77f) - 0.5f) * 0.2f;
                samples[i] = Mathf.Clamp((wobble * 0.85f + texture) * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
