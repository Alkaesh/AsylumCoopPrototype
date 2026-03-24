using AsylumHorror.Player;
using UnityEngine;

namespace AsylumHorror.World
{
    [RequireComponent(typeof(Camera))]
    public class HorrorScreenFX : MonoBehaviour
    {
        [SerializeField] private bool menuProfile;
        [SerializeField] private float exposure = 0.93f;
        [SerializeField] private float contrast = 1.08f;
        [SerializeField] private float saturation = 0.84f;
        [SerializeField] private Color tint = new Color(0.86f, 0.94f, 0.91f, 1f);
        [SerializeField] private float vignette = 0.26f;
        [SerializeField] private float grain = 0.025f;
        [SerializeField] private float bloomThreshold = 0.66f;
        [SerializeField] private float bloomStrength = 0.04f;
        [SerializeField] private float stressVignetteBoost = 0.14f;
        [SerializeField] private float stressGrainBoost = 0.02f;
        [SerializeField] private float shockVignetteBoost = 0.22f;
        [SerializeField] private float shockGrainBoost = 0.05f;
        [SerializeField] private float shockExposureDrop = 0.18f;
        [SerializeField] private float shockBloomBoost = 0.05f;
        [SerializeField] private Color shockTint = new Color(0.95f, 0.84f, 0.84f, 1f);

        private const string ShaderName = "Hidden/AsylumHorror/HorrorScreenFX";

        [SerializeField] private Shader effectShader;
        private Material runtimeMaterial;
        private PlayerStressController stressController;
        private float nextStressLookupAt;
        private float shockStartTime;
        private float shockEndTime;
        private float shockStrength;

        private void OnEnable()
        {
            effectShader = Shader.Find(ShaderName);
            EnsureMaterial();
        }

        private void OnDisable()
        {
            if (runtimeMaterial != null)
            {
                DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!EnsureMaterial())
            {
                Graphics.Blit(source, destination);
                return;
            }

            float stress01 = menuProfile ? 0f : ResolveStress01();
            float shock01 = ResolveShock01();
            runtimeMaterial.SetColor("_GradeTint", Color.Lerp(tint, shockTint, shock01));
            runtimeMaterial.SetFloat("_Exposure", exposure - shock01 * shockExposureDrop);
            runtimeMaterial.SetFloat("_Contrast", contrast);
            runtimeMaterial.SetFloat("_Saturation", saturation);
            runtimeMaterial.SetFloat("_Vignette", vignette + stress01 * stressVignetteBoost + shock01 * shockVignetteBoost);
            runtimeMaterial.SetFloat("_Grain", grain + stress01 * stressGrainBoost + shock01 * shockGrainBoost);
            runtimeMaterial.SetFloat("_BloomThreshold", bloomThreshold);
            runtimeMaterial.SetFloat("_BloomStrength", bloomStrength + shock01 * shockBloomBoost);
            runtimeMaterial.SetFloat("_NoiseTime", Time.unscaledTime);
            Graphics.Blit(source, destination, runtimeMaterial);
        }

        public void TriggerShock(float strength01, float durationSeconds)
        {
            shockStartTime = Time.unscaledTime;
            shockEndTime = shockStartTime + Mathf.Max(0.05f, durationSeconds);
            shockStrength = Mathf.Clamp01(strength01);
        }

        private bool EnsureMaterial()
        {
            if (runtimeMaterial != null)
            {
                return true;
            }

            if (effectShader == null)
            {
                effectShader = Shader.Find(ShaderName);
            }

            if (effectShader == null || !effectShader.isSupported)
            {
                return false;
            }

            runtimeMaterial = new Material(effectShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }

        private float ResolveStress01()
        {
            if (stressController == null && Time.unscaledTime >= nextStressLookupAt)
            {
                nextStressLookupAt = Time.unscaledTime + 0.75f;
                foreach (PlayerStressController candidate in FindObjectsByType<PlayerStressController>())
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    NetworkPlayerController controller = candidate.GetComponent<NetworkPlayerController>();
                    if (controller != null && controller.isLocalPlayer)
                    {
                        stressController = candidate;
                        break;
                    }
                }
            }

            return stressController != null ? stressController.CurrentStress01 : 0f;
        }

        private float ResolveShock01()
        {
            if (Time.unscaledTime >= shockEndTime || shockEndTime <= shockStartTime)
            {
                return 0f;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - shockStartTime) / Mathf.Max(0.01f, shockEndTime - shockStartTime));
            float spike = progress < 0.18f
                ? Mathf.SmoothStep(0f, 1f, progress / 0.18f)
                : Mathf.SmoothStep(1f, 0f, (progress - 0.18f) / 0.82f);
            return spike * shockStrength;
        }
    }
}
