using UnityEngine;

namespace AsylumHorror.Monster
{
    public enum MonsterSearchProbeType
    {
        Intercept = 0,
        Doorway = 1,
        Corner = 2,
        Room = 3,
        Fallback = 4
    }

    public readonly struct MonsterSearchProbe
    {
        public MonsterSearchProbe(Vector3 position, Vector3 focusPoint, MonsterSearchProbeType type, float dwellSeconds)
        {
            Position = position;
            FocusPoint = focusPoint;
            Type = type;
            DwellSeconds = dwellSeconds;
        }

        public Vector3 Position { get; }
        public Vector3 FocusPoint { get; }
        public MonsterSearchProbeType Type { get; }
        public float DwellSeconds { get; }
    }
}
