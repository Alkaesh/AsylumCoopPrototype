using System.Collections;
using AsylumHorror.Player;
using UnityEngine;

namespace AsylumHorror.World
{
    public enum ScriptedHorrorMomentType
    {
        FlashSilhouette = 0,
        RevealCorpse = 1,
        ShadowPass = 2,
        DoorSlam = 3
    }

    [RequireComponent(typeof(BoxCollider))]
    public class ScriptedHorrorMoment : MonoBehaviour
    {
        [SerializeField] private ScriptedHorrorMomentType momentType;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip triggerClip;
        [SerializeField] private Light linkedLight;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform secondaryPoint;
        [SerializeField] private GameObject revealObject;
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Vector3 doorClosedEuler = Vector3.zero;
        [SerializeField] private Vector3 doorOpenedEuler = new Vector3(0f, 95f, 0f);
        [SerializeField] private bool triggerOnlyOnce = true;

        private bool fired;

        private void Awake()
        {
            BoxCollider triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;

            if (revealObject != null)
            {
                revealObject.SetActive(false);
            }

            if (doorPanel != null)
            {
                doorPanel.localRotation = Quaternion.Euler(doorOpenedEuler);
            }
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

            switch (momentType)
            {
                case ScriptedHorrorMomentType.FlashSilhouette:
                    StartCoroutine(FlashSilhouetteMoment());
                    break;
                case ScriptedHorrorMomentType.RevealCorpse:
                    StartCoroutine(RevealCorpseMoment());
                    break;
                case ScriptedHorrorMomentType.ShadowPass:
                    StartCoroutine(ShadowPassMoment());
                    break;
                case ScriptedHorrorMomentType.DoorSlam:
                    StartCoroutine(DoorSlamMoment());
                    break;
            }
        }

        private IEnumerator FlashSilhouetteMoment()
        {
            Vector3 silhouettePosition = secondaryPoint != null ? secondaryPoint.position : transform.position;
            Quaternion silhouetteRotation = secondaryPoint != null ? secondaryPoint.rotation : transform.rotation;
            GameObject silhouette = BossApparitionFactory.Create(silhouettePosition, silhouetteRotation, BossApparitionStyle.FlashReveal);
            BossApparitionProxy proxy = silhouette != null ? silhouette.GetComponent<BossApparitionProxy>() : null;
            proxy?.SetOpacity(0.78f);
            yield return PulseLight(0.9f, 2.2f);
            yield return FadeApparition(silhouette, proxy, 0.9f);
        }

        private IEnumerator RevealCorpseMoment()
        {
            if (revealObject != null)
            {
                revealObject.SetActive(true);
            }

            yield return PulseLight(1.2f, 1.8f);
        }

        private IEnumerator ShadowPassMoment()
        {
            Vector3 start = startPoint != null ? startPoint.position : transform.position;
            Quaternion startRotation = startPoint != null ? startPoint.rotation : transform.rotation;
            GameObject silhouette = BossApparitionFactory.Create(start, startRotation, BossApparitionStyle.DoorwayShadow);
            if (silhouette == null)
            {
                yield break;
            }

            BossApparitionProxy proxy = silhouette.GetComponent<BossApparitionProxy>();
            Vector3 end = secondaryPoint != null ? secondaryPoint.position : transform.position + transform.right * 2.2f;
            float duration = 0.7f;
            float endTime = Time.time + duration;

            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                silhouette.transform.position = Vector3.Lerp(start, end, t);
                proxy?.SetOpacity(Mathf.Lerp(0.65f, 0.05f, t));
                yield return null;
            }

            Destroy(silhouette);
        }

        private IEnumerator DoorSlamMoment()
        {
            if (doorPanel == null)
            {
                yield break;
            }

            Quaternion openRotation = Quaternion.Euler(doorOpenedEuler);
            Quaternion closedRotation = Quaternion.Euler(doorClosedEuler);
            float duration = 0.18f;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                doorPanel.localRotation = Quaternion.Slerp(openRotation, closedRotation, t);
                yield return null;
            }

            doorPanel.localRotation = closedRotation;
            yield return PulseLight(0.45f, 1.7f);
        }

        private IEnumerator PulseLight(float duration, float multiplier)
        {
            if (linkedLight == null)
            {
                yield break;
            }

            float baseIntensity = linkedLight.intensity;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                linkedLight.intensity = Mathf.Lerp(baseIntensity, baseIntensity * multiplier, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }

            linkedLight.intensity = baseIntensity;
        }

        private IEnumerator FadeApparition(GameObject silhouette, BossApparitionProxy proxy, float duration)
        {
            if (silhouette == null)
            {
                yield break;
            }

            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                proxy?.SetOpacity(Mathf.Lerp(0.7f, 0f, t));
                yield return null;
            }

            Destroy(silhouette);
        }
    }
}
