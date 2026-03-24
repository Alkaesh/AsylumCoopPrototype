using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.Monster
{
    [RequireComponent(typeof(MonsterAI))]
    public class MonsterPresentation : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform eyeGlowRoot;
        [SerializeField] private float idleBreathAmplitude = 0.025f;
        [SerializeField] private float patrolBobAmplitude = 0.055f;
        [SerializeField] private float chaseBobAmplitude = 0.1f;
        [SerializeField] private float carryLean = 12f;
        [SerializeField] private float chaseLean = 8f;
        [SerializeField] private float swayAngle = 5.5f;
        [SerializeField] private float visualFollowSpeed = 8f;
        [SerializeField] private float grabLungeDistance = 0.34f;
        [SerializeField] private float grabLungeLift = 0.09f;
        [SerializeField] private float grabLungeLean = 30f;
        [SerializeField] private float grabLungeRoll = 6.5f;

        private MonsterAI monsterAI;
        private NavMeshAgent navMeshAgent;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 eyeBaseScale;
        private float cycle;
        private Vector3 lastWorldPosition;

        private void Awake()
        {
            monsterAI = GetComponent<MonsterAI>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            if (visualRoot == null)
            {
                Transform candidate = transform.Find("MonsterVisual");
                if (candidate != null)
                {
                    visualRoot = candidate;
                }
                else
                {
                    foreach (Transform child in transform)
                    {
                        if (child != null && child.GetComponent<Light>() == null)
                        {
                            visualRoot = child;
                            break;
                        }
                    }
                }
            }

            if (eyeGlowRoot == null)
            {
                eyeGlowRoot = transform.Find("EyeGlow");
            }

            if (visualRoot != null)
            {
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }

            if (eyeGlowRoot != null)
            {
                eyeBaseScale = eyeGlowRoot.localScale;
            }

            lastWorldPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (visualRoot == null || monsterAI == null)
            {
                return;
            }

            float speed = navMeshAgent != null ? navMeshAgent.velocity.magnitude : ((transform.position - lastWorldPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f));
            lastWorldPosition = transform.position;
            float move01 = Mathf.Clamp01(speed / 4.9f);

            float bobAmplitude = idleBreathAmplitude;
            float bobFrequency = 1.7f;
            float forwardLean = 0f;
            float sideSway = 0f;

            switch (monsterAI.CurrentState)
            {
                case MonsterState.Patrol:
                case MonsterState.InvestigateSound:
                case MonsterState.Search:
                    bobAmplitude = Mathf.Lerp(idleBreathAmplitude, patrolBobAmplitude, move01);
                    bobFrequency = Mathf.Lerp(1.8f, 4.5f, move01);
                    sideSway = Mathf.Sin(cycle * 0.5f) * swayAngle * move01;
                    break;
                case MonsterState.Chase:
                case MonsterState.Attack:
                    bobAmplitude = Mathf.Lerp(idleBreathAmplitude, chaseBobAmplitude, move01);
                    bobFrequency = Mathf.Lerp(2.6f, 6.8f, move01);
                    forwardLean = -chaseLean * Mathf.Clamp01(0.4f + move01);
                    sideSway = Mathf.Sin(cycle * 0.58f) * swayAngle * 1.15f * move01;
                    break;
                case MonsterState.Carry:
                    bobAmplitude = Mathf.Lerp(idleBreathAmplitude, patrolBobAmplitude * 0.6f, move01);
                    bobFrequency = Mathf.Lerp(1.4f, 3.6f, move01);
                    forwardLean = -carryLean;
                    sideSway = Mathf.Sin(cycle * 0.42f) * swayAngle * 0.5f;
                    break;
                case MonsterState.Suspicious:
                    bobAmplitude = idleBreathAmplitude;
                    bobFrequency = 1.1f;
                    sideSway = Mathf.Sin(Time.time * 2.6f) * 2.2f;
                    break;
            }

            cycle += Time.deltaTime * (bobFrequency * Mathf.Lerp(0.55f, 1.25f, move01));
            float bob = Mathf.Sin(cycle) * bobAmplitude;
            float roll = Mathf.Sin(cycle * 0.5f) * (move01 * 2.2f);

            Vector3 targetPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
            Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(forwardLean, sideSway, roll);

            if (monsterAI.GrabPresentationActive)
            {
                float grab01 = monsterAI.GrabPresentation01;
                float slam01 = grab01 < 0.18f
                    ? Mathf.SmoothStep(0f, 1f, grab01 / 0.18f)
                    : Mathf.SmoothStep(1f, 0f, (grab01 - 0.18f) / 0.82f);
                float side = (monsterAI.GrabPresentationVariant % 2 == 0) ? -1f : 1f;
                targetPosition += Vector3.forward * (grabLungeDistance * slam01) + Vector3.up * (grabLungeLift * slam01);
                targetRotation *= Quaternion.Euler(-grabLungeLean * slam01, side * swayAngle * 0.45f * slam01, side * grabLungeRoll * slam01);

                Transform victim = monsterAI.ResolveGrabPresentationVictimTransform();
                if (victim != null)
                {
                    Vector3 toVictim = victim.position - transform.position;
                    toVictim.y = 0f;
                    if (toVictim.sqrMagnitude > 0.02f)
                    {
                        Vector3 localDirection = transform.InverseTransformDirection(toVictim.normalized);
                        float victimYaw = Mathf.Clamp(Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg, -22f, 22f);
                        targetRotation *= Quaternion.Euler(0f, victimYaw * slam01, 0f);
                    }
                }
            }

            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, Time.deltaTime * visualFollowSpeed);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, Time.deltaTime * visualFollowSpeed);

            if (eyeGlowRoot != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * (monsterAI.CurrentState == MonsterState.Chase ? 7.5f : 4.2f)) * 0.08f;
                eyeGlowRoot.localScale = eyeBaseScale == Vector3.zero ? Vector3.one * pulse : eyeBaseScale * pulse;
            }
        }
    }
}
