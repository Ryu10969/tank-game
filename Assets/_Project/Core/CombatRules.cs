using System;

namespace TankGame.Core
{
    public sealed class ShotSlots
    {
        public int Capacity { get; }
        public int Active { get; private set; }
        public int Available => Capacity - Active;
        public ShotSlots(int capacity) { Capacity = Math.Max(1, capacity); }
        public bool TryAcquire()
        {
            if (Active >= Capacity) return false;
            Active++;
            return true;
        }
        public void Release() { if (Active > 0) Active--; }
    }

    public sealed class TankLife
    {
        public bool IsAlive { get; private set; } = true;
        public bool Hit()
        {
            if (!IsAlive) return false;
            IsAlive = false;
            return true;
        }
    }

    public enum MatchState { Playing, Victory, Defeat }

    public sealed class MatchRules
    {
        public MatchState State { get; private set; } = MatchState.Playing;
        public int EnemiesRemaining { get; private set; }
        public MatchRules(int enemies) { EnemiesRemaining = enemies; }
        public void TankDestroyed(bool player)
        {
            if (State != MatchState.Playing) return;
            if (player) State = MatchState.Defeat;
            else if (--EnemiesRemaining == 0) State = MatchState.Victory;
        }
    }

    public sealed class ProjectileRules
    {
        readonly int maximumReflections;
        public int Reflections { get; private set; }
        public bool Alive { get; private set; } = true;
        public ProjectileRules(int maximumReflections) { this.maximumReflections = maximumReflections; }
        public bool HitWall()
        {
            if (!Alive) return false;
            if (Reflections >= maximumReflections) { Alive = false; return false; }
            Reflections++;
            return true;
        }
        public static void Reflect(float dx, float dz, float nx, float nz, out float rx, out float rz)
        {
            float dot = dx * nx + dz * nz;
            rx = dx - 2 * dot * nx;
            rz = dz - 2 * dot * nz;
        }
        public static float RemainingDistance(float distance, float traveled) => Math.Max(0, distance - Math.Max(0, traveled));
    }
}
