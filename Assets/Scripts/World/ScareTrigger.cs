using System.Collections;
using AsylumHorror.Player;
using UnityEngine;

namespace AsylumHorror.World
{
    [RequireComponent(typeof(BoxCollider))]
    public class ScareTrigger : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip triggerClip;
        [SerializeField] private bool triggerOnlyOnce = true;
        [SerializeField] private float lightPulseRadius = 16f;
        [SerializeField] private float lightPulseDuration = 1.1f;

        private bool fired;

        private void Awake()
        {
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired && triggerOnlyOnce)
            {
                return;
            }

            NetworkPlayerController player = other.GetComponentInParent<NetworkPlayerController>();
            if (player == null || !player.isLocalPlayer)
            {
                return;
            }

            fired = true;
            if (audioSource != null && triggerClip != null)
            {
                audioSource.PlayOneShot(triggerClip);
            }

            StartCoroutine(PulseNearbyLights());
        }

        private IEnumerator PulseNearbyLights()
        {
            Light[] allLights = FindObjectsByType<Light>();
            var affected = new System.Collections.Generic.List<(Light light, float baseIntensity)>();
            foreach (Light light in allLights)
            {
                if (light == null)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, light.transform.position) <= lightPulseRadius)
                {
                    affected.Add((light, light.intensity));
                }
            }

            float endTime = Time.time + lightPulseDuration;
            while (Time.time < endTime)
            {
                float t = Mathf.InverseLerp(endTime, endTime - lightPulseDuration, Time.time);
                float pulse = Mathf.Lerp(0.35f, 1.6f, Mathf.PingPong(t * 2.8f, 1f));
                foreach ((Light light, float baseIntensity) in affected)
                {
                    if (light != null)
                    {
                        light.intensity = baseIntensity * pulse;
                    }
                }

                yield return null;
            }

            foreach ((Light light, float baseIntensity) in affected)
            {
                if (light != null)
                {
                    light.intensity = baseIntensity;
                }
            }
        }
    }
}
