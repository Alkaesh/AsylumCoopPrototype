using UnityEngine;

namespace AsylumHorror.World
{
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float randomAmplitude = 0.35f;
        [SerializeField] private float randomSpeed = 12f;

        private Light targetLight;
        private float baseIntensity = 1f;
        private float randomSeed;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseIntensity = targetLight.intensity;
            randomSeed = Random.Range(0f, 999f);
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            float noise = Mathf.PerlinNoise(randomSeed, Time.time * randomSpeed);
            float nextIntensity = baseIntensity + (noise - 0.5f) * randomAmplitude;
            targetLight.intensity = Mathf.Max(0.05f, nextIntensity);
        }

        public void SetBaseIntensity(float value)
        {
            baseIntensity = Mathf.Max(0.01f, value);
            if (targetLight != null)
            {
                targetLight.intensity = baseIntensity;
            }
        }
    }
}
