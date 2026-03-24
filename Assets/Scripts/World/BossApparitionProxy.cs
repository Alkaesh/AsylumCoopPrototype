using System.Collections.Generic;
using UnityEngine;

namespace AsylumHorror.World
{
    public class BossApparitionProxy : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Color> baseColors = new List<Color>();
        private Light[] lights;
        private float[] baseLightIntensities;
        private bool initialized;

        public void Initialize(Color tint, float lightIntensityMultiplier = 0.45f)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CacheMaterials(tint);
            CacheLights(lightIntensityMultiplier);
            SetOpacity(0.7f);
        }

        public void SetOpacity(float opacity)
        {
            float clampedOpacity = Mathf.Clamp01(opacity);
            for (int index = 0; index < materials.Count; index++)
            {
                Material material = materials[index];
                if (material == null)
                {
                    continue;
                }

                Color color = baseColors[index];
                color.a *= clampedOpacity;
                SetMaterialColor(material, color);
            }

            if (lights == null || baseLightIntensities == null)
            {
                return;
            }

            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index] == null)
                {
                    continue;
                }

                lights[index].intensity = baseLightIntensities[index] * clampedOpacity;
            }
        }

        private void CacheMaterials(Color tint)
        {
            Shader texturedShader = Shader.Find("Sprites/Default") ??
                                    Shader.Find("Legacy Shaders/Transparent/Diffuse") ??
                                    Shader.Find("Standard");
            Shader colorShader = Shader.Find("Sprites/Default") ??
                                 Shader.Find("Unlit/Color") ??
                                 Shader.Find("Standard");

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                Material[] apparitionMaterials = new Material[sourceMaterials.Length];
                for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    Material source = sourceMaterials[materialIndex];
                    Texture texture = ExtractTexture(source);
                    Shader shader = texture != null ? texturedShader : colorShader;
                    Material apparitionMaterial = new Material(shader);
                    if (texture != null)
                    {
                        if (apparitionMaterial.HasProperty("_MainTex"))
                        {
                            apparitionMaterial.SetTexture("_MainTex", texture);
                        }

                        if (apparitionMaterial.HasProperty("_BaseMap"))
                        {
                            apparitionMaterial.SetTexture("_BaseMap", texture);
                        }
                    }

                    Color sourceColor = ExtractColor(source);
                    Color finalColor = new Color(
                        sourceColor.r * tint.r,
                        sourceColor.g * tint.g,
                        sourceColor.b * tint.b,
                        1f);
                    SetMaterialColor(apparitionMaterial, finalColor);
                    apparitionMaterials[materialIndex] = apparitionMaterial;
                    materials.Add(apparitionMaterial);
                    baseColors.Add(finalColor);
                }

                renderer.materials = apparitionMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void CacheLights(float intensityMultiplier)
        {
            lights = GetComponentsInChildren<Light>(true);
            baseLightIntensities = new float[lights.Length];
            for (int index = 0; index < lights.Length; index++)
            {
                Light light = lights[index];
                if (light == null)
                {
                    continue;
                }

                baseLightIntensities[index] = light.intensity * Mathf.Max(0f, intensityMultiplier);
                light.intensity = baseLightIntensities[index];
                light.shadows = LightShadows.None;
                light.range *= 0.8f;
            }
        }

        private static Texture ExtractTexture(Material source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.HasProperty("_BaseMap"))
            {
                Texture texture = source.GetTexture("_BaseMap");
                if (texture != null)
                {
                    return texture;
                }
            }

            return source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
        }

        private static Color ExtractColor(Material source)
        {
            if (source == null)
            {
                return Color.white;
            }

            if (source.HasProperty("_BaseColor"))
            {
                return source.GetColor("_BaseColor");
            }

            if (source.HasProperty("_Color"))
            {
                return source.GetColor("_Color");
            }

            return Color.white;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }
    }
}
