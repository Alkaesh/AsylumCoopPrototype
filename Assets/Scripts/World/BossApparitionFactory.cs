using AsylumHorror.Monster;
using UnityEngine;

namespace AsylumHorror.World
{
    public enum BossApparitionStyle
    {
        FlashReveal = 0,
        DoorwayShadow = 1
    }

    public static class BossApparitionFactory
    {
        public static GameObject Create(Vector3 position, Quaternion rotation, BossApparitionStyle style)
        {
            if (!TryResolveMonsterVisual(out Transform visualRoot, out Transform eyeGlowRoot))
            {
                return null;
            }

            GameObject root = new GameObject("BossApparition");
            root.transform.SetPositionAndRotation(position, rotation);

            GameObject visualClone = Object.Instantiate(visualRoot.gameObject, root.transform);
            visualClone.name = "BossVisual";
            visualClone.transform.localPosition = visualRoot.localPosition;
            visualClone.transform.localRotation = visualRoot.localRotation;
            visualClone.transform.localScale = visualRoot.localScale;
            StripNonVisualComponents(visualClone);

            if (eyeGlowRoot != null)
            {
                GameObject eyeClone = Object.Instantiate(eyeGlowRoot.gameObject, root.transform);
                eyeClone.name = "EyeGlow";
                eyeClone.transform.localPosition = eyeGlowRoot.localPosition;
                eyeClone.transform.localRotation = eyeGlowRoot.localRotation;
                eyeClone.transform.localScale = eyeGlowRoot.localScale;
                StripNonVisualComponents(eyeClone, preserveLights: true);
            }

            BossApparitionProxy proxy = root.AddComponent<BossApparitionProxy>();
            proxy.Initialize(ResolveTint(style), style == BossApparitionStyle.FlashReveal ? 0.56f : 0.3f);
            return root;
        }

        private static bool TryResolveMonsterVisual(out Transform visualRoot, out Transform eyeGlowRoot)
        {
            visualRoot = null;
            eyeGlowRoot = null;

            MonsterAI monster = MonsterAI.Instance;
            if (monster == null)
            {
                return false;
            }

            MonsterPresentation presentation = monster.GetComponent<MonsterPresentation>();
            if (presentation != null)
            {
                Transform explicitVisual = monster.transform.Find("MonsterVisual");
                if (explicitVisual != null)
                {
                    visualRoot = explicitVisual;
                }

                Transform explicitGlow = monster.transform.Find("EyeGlow");
                if (explicitGlow != null)
                {
                    eyeGlowRoot = explicitGlow;
                }
            }

            if (visualRoot == null)
            {
                visualRoot = monster.transform.Find("MonsterVisual");
            }

            if (visualRoot == null)
            {
                foreach (Transform child in monster.transform)
                {
                    if (child == null || child.GetComponent<Renderer>() == null)
                    {
                        continue;
                    }

                    visualRoot = child;
                    break;
                }
            }

            if (eyeGlowRoot == null)
            {
                eyeGlowRoot = monster.transform.Find("EyeGlow");
            }

            return visualRoot != null;
        }

        private static Color ResolveTint(BossApparitionStyle style)
        {
            return style == BossApparitionStyle.FlashReveal
                ? new Color(0.52f, 0.54f, 0.5f, 1f)
                : new Color(0.2f, 0.2f, 0.22f, 1f);
        }

        private static void StripNonVisualComponents(GameObject root, bool preserveLights = false)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Object.Destroy(collider);
            }

            foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.Destroy(rigidbody);
            }

            foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
            {
                Object.Destroy(audioSource);
            }

            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Object.Destroy(behaviour);
            }

            if (!preserveLights)
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    Object.Destroy(light);
                }
            }
        }
    }
}
