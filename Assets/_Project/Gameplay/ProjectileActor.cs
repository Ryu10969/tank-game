using TankGame.Core;
using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class ProjectileActor : MonoBehaviour
    {
        public ProjectileRules Rules { get; private set; }
        public bool IsAlive { get; private set; }
        public Vector3 Direction { get; private set; }
        public TankActor Owner => owner;
        public float Speed { get; private set; }
        internal float Radius => settings.projectileRadius;
        internal float SurfaceSeparation => settings.surfaceSeparation;
        TankActor owner;
        GameplaySettings settings;
        float age;
        public void Initialize(TankActor owner, GameplaySettings configuration, Vector3 direction)
        {
            this.owner = owner; settings = configuration; Direction = direction.normalized;
            Speed = owner.ProjectileSpeed;
            Rules = new ProjectileRules(settings.maximumReflections); IsAlive = true;
        }
        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;
            float aliveTime = BeginSimulation(deltaTime);
            Advance(Speed * Mathf.Max(0, aliveTime));
            EndSimulation();
        }
        internal float BeginSimulation(float deltaTime)
        {
            if (!IsAlive) return 0;
            float aliveTime = Mathf.Min(Mathf.Max(0, deltaTime), Mathf.Max(0, settings.projectileLifetime - age));
            age += Mathf.Max(0, deltaTime);
            return aliveTime;
        }
        internal void Move(float seconds)
        {
            if (IsAlive && seconds > 0) transform.position += Direction * (Speed * seconds);
        }
        internal bool ResolveStaticHit(RaycastHit hit)
        {
            if (!IsAlive || hit.collider == null) return false;
            var destructibleWall = hit.collider.GetComponent<DestructibleWallActor>();
            if (destructibleWall != null) { destructibleWall.Hit(); Despawn(); return false; }
            var tank = CollisionQueries.TankFromCollider(hit.collider);
            if (tank != null) { tank.Hit(); Despawn(); return false; }
            if (!Rules.HitWall()) { Despawn(); return false; }
            Vector3 normal = hit.normal; normal.y = 0; normal.Normalize();
            ProjectileRules.Reflect(Direction.x, Direction.z, normal.x, normal.z, out float rx, out float rz);
            Direction = new Vector3(rx, 0, rz).normalized;
            transform.position += normal * settings.surfaceSeparation;
            return true;
        }
        internal void EndSimulation()
        {
            if (IsAlive && age >= settings.projectileLifetime) Despawn();
        }
        public void Advance(float distance)
        {
            while (IsAlive && distance > 0)
            {
                if (!CollisionQueries.FirstProjectileHit(transform.position, settings.projectileRadius, Direction, distance,
                    owner, this, Speed * CollisionQueries.TimeEpsilon, out var hit))
                { transform.position += Direction * distance; return; }
                transform.position += Direction * hit.distance;
                distance = ProjectileRules.RemainingDistance(distance, hit.distance);
                var projectile = hit.collider.GetComponent<ProjectileActor>();
                if (projectile != null) { Clash(projectile); return; }
                var destructibleWall = hit.collider.GetComponent<DestructibleWallActor>();
                if (destructibleWall != null) { destructibleWall.Hit(); Despawn(); return; }
                var tank = CollisionQueries.TankFromCollider(hit.collider);
                if (tank != null) { tank.Hit(); Despawn(); return; }
                if (!Rules.HitWall()) { Despawn(); return; }
                Vector3 normal = hit.normal; normal.y = 0; normal.Normalize();
                ProjectileRules.Reflect(Direction.x, Direction.z, normal.x, normal.z, out float rx, out float rz);
                Direction = new Vector3(rx, 0, rz).normalized;
                transform.position += normal * settings.surfaceSeparation;
                distance = ProjectileRules.RemainingDistance(distance, settings.surfaceSeparation);
            }
        }
        public void Clash(ProjectileActor other)
        {
            if (!IsAlive || other == null || !other.IsAlive || other == this) return;
            Despawn(); other.Despawn();
        }
        public void Despawn()
        {
            if (!IsAlive) return;
            IsAlive = false;
            owner.Slots.Release();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        void OnDestroy() { if (IsAlive && owner != null) { IsAlive = false; owner.Slots.Release(); } }
    }
}
