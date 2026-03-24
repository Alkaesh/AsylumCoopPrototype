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
        private Material silhouetteMaterial;

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
            GameObject silhouette = CreateSilhouette(silhouettePosition, silhouetteRotation);
            yield return PulseLight(0.9f, 2.2f);
            yield return FadeSilhouette(silhouette, 0.9f);
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
            GameObject silhouette = CreateSilhouette(start, startRotation);
            Vector3 end = secondaryPoint != null ? secondaryPoint.position : transform.position + transform.right * 2.2f;
            float duration = 0.7f;
            float endTime = Time.time + duration;

            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                silhouette.transform.position = Vector3.Lerp(start, end, t);
                SetSilhouetteAlpha(silhouette, Mathf.Lerp(0.65f, 0.05f, t));
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

        private IEnumerator FadeSilhouette(GameObject silhouette, float duration)
        {
            if (silhouette == null)
            {
                yield break;
            }

            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                float t = 1f - Mathf.Clamp01((endTime - Time.time) / duration);
                SetSilhouetteAlpha(silhouette, Mathf.Lerp(0.7f, 0f, t));
                yield return null;
            }

            Destroy(silhouette);
        }

        private GameObject CreateSilhouette(Vector3 position, Quaternion rotation)
        {
            if (silhouetteMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                silhouetteMaterial = new Material(shader);
                silhouetteMaterial.color = new Color(0.02f, 0.02f, 0.025f, 0.72f);
            }

            GameObject root = new GameObject("ScriptedSilhouette");
            root.transform.SetPositionAndRotation(position, rotation);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            torso.transform.localScale = new Vector3(0.7f, 1.12f, 0.7f);
            Destroy(torso.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.06f, 0.05f);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Destroy(head.GetComponent<Collider>());

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = silhouetteMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return root;
        }

        private void SetSilhouetteAlpha(GameObject silhouette, float alpha)
        {
            if (silhouette == null)
            {
                return;
            }

            foreach (Renderer renderer in silhouette.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || renderer.material == null)
                {
                    continue;
                }

                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }
}
