using System;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Monster
{
    public enum NoiseCategory
    {
        PlayerMovement = 0,
        Task = 1,
        Attack = 2
    }

    public readonly struct NoiseEvent
    {
        public NoiseEvent(Vector3 position, float radius, float priority, NoiseCategory category, double timestamp)
        {
            Position = position;
            Radius = radius;
            Priority = priority;
            Category = category;
            Timestamp = timestamp;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
        public float Priority { get; }
        public NoiseCategory Category { get; }
        public double Timestamp { get; }
    }

    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> ServerNoiseEmitted;

        public static void Emit(Vector3 position, float radius, float priority, NoiseCategory category)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            ServerNoiseEmitted?.Invoke(new NoiseEvent(position, radius, priority, category, NetworkTime.time));
        }
    }
}
