using System.Collections.Generic;
using TankGame.Core;
using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class GameSession : MonoBehaviour
    {
        public GameplaySettings Settings { get; private set; }
        public MatchRules Rules { get; private set; }
        public TankActor Player { get; private set; }
        public IReadOnlyList<TankActor> Tanks => tanks;
        public IReadOnlyList<ProjectileActor> Projectiles => projectiles;
        public bool AutoTick { get; set; } = true;
        readonly List<TankActor> tanks = new List<TankActor>();
        readonly List<ProjectileActor> projectiles = new List<ProjectileActor>();
        Material projectileMaterial;
        public void Initialize(GameplaySettings settings, int enemyCount, Material material)
        { Settings = settings; Rules = new MatchRules(enemyCount); projectileMaterial = material; }
        public void Register(TankActor tank) { tanks.Add(tank); if (tank.IsPlayer) Player = tank; }
        public ProjectileActor SpawnProjectile(TankActor owner, Vector3 direction)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile"; go.transform.SetParent(transform);
            go.transform.position = owner.transform.position;
            go.transform.localScale = Vector3.one * (Settings.projectileRadius * 2);
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = projectileMaterial;
            var projectile = go.AddComponent<ProjectileActor>();
            projectile.Initialize(owner, Settings, direction);
            projectiles.Add(projectile);
            projectile.Advance(Settings.muzzleDistance);
            return projectile;
        }
        void Update() { if (AutoTick) Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if (Rules == null || Rules.State != MatchState.Playing) return;
            foreach (var tank in tanks)
            {
                if (Rules.State != MatchState.Playing) break;
                tank.Tick(deltaTime);
                Physics.SyncTransforms();
            }
            foreach (var projectile in projectiles)
            {
                if (Rules.State != MatchState.Playing) break;
                projectile.Tick(deltaTime);
            }
            projectiles.RemoveAll(p => p == null || !p.IsAlive);
        }
    }
}
