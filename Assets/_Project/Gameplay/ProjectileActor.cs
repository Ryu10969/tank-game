using TankGame.Core;
using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class ProjectileActor : MonoBehaviour
    {
        public ProjectileRules Rules { get; private set; }
        public bool IsAlive { get; private set; }
        public Vector3 Direction { get; private set; }
        TankActor shooter;
        GameplaySettings settings;
        float age;
        public void Initialize(TankActor owner, GameplaySettings configuration, Vector3 direction)
        {
            shooter = owner; settings = configuration; Direction = direction.normalized;
            Rules = new ProjectileRules(settings.maximumReflections); IsAlive = true;
        }
        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;
            float aliveTime = Mathf.Min(deltaTime, settings.projectileLifetime - age);
            age += deltaTime;
            Advance(settings.projectileSpeed * Mathf.Max(0, aliveTime));
            if (age >= settings.projectileLifetime) Despawn();
        }
        public void Advance(float distance)
        {
            while (IsAlive && distance > 0)
            {
                var ignore = Rules.Reflections == 0 ? shooter : null;
                if (!CollisionQueries.FirstHit(transform.position, settings.projectileRadius, Direction, distance, ignore, out var hit))
                { transform.position += Direction * distance; return; }
                transform.position += Direction * hit.distance;
                distance = ProjectileRules.RemainingDistance(distance, hit.distance);
                var tank = hit.collider.GetComponent<TankActor>();
                if (tank != null) { tank.Hit(); Despawn(); return; }
                if (!Rules.HitWall()) { Despawn(); return; }
                Vector3 normal = hit.normal; normal.y = 0; normal.Normalize();
                ProjectileRules.Reflect(Direction.x, Direction.z, normal.x, normal.z, out float rx, out float rz);
                Direction = new Vector3(rx, 0, rz).normalized;
                transform.position += normal * settings.surfaceSeparation;
                distance = ProjectileRules.RemainingDistance(distance, settings.surfaceSeparation);
            }
        }
        public void Despawn()
        {
            if (!IsAlive) return;
            IsAlive = false;
            shooter.Slots.Release();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        void OnDestroy() { if (IsAlive && shooter != null) { IsAlive = false; shooter.Slots.Release(); } }
    }
}
