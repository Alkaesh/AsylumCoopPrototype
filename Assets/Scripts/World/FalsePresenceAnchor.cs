using UnityEngine;

namespace AsylumHorror.World
{
    public enum FalsePresenceEventType
    {
        DistantFootsteps = 0,
        FlashSilhouette = 1,
        DoorwayShadow = 2,
        HideoutNoise = 3
    }

    public class FalsePresenceAnchor : MonoBehaviour
    {
        [SerializeField] private FalsePresenceEventType eventType;
        [SerializeField] private Transform secondaryPoint;
        [SerializeField] private Light linkedLight;
        [SerializeField] private float preferredPlayerDistance = 14f;

        public FalsePresenceEventType EventType => eventType;
        public Transform SecondaryPoint => secondaryPoint;
        public Light LinkedLight => linkedLight;
        public float PreferredPlayerDistance => preferredPlayerDistance;
    }
}
