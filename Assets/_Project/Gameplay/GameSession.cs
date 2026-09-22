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
        public IReadOnlyList<DestructibleWallActor> DestructibleWalls => destructibleWalls;
        public IReadOnlyList<MineActor> Mines => mines;
        public bool AutoTick { get; set; } = true;
        readonly List<TankActor> tanks = new List<TankActor>();
        readonly List<ProjectileActor> projectiles = new List<ProjectileActor>();
        readonly List<DestructibleWallActor> destructibleWalls = new List<DestructibleWallActor>();
        readonly List<MineActor> mines = new List<MineActor>();
        internal sealed class ProjectileStep
        {
            public ProjectileActor Projectile;
            public float RemainingTime;
        }
        internal enum InteractionKind { None, Static, Clash }
        internal struct Interaction
        {
            public InteractionKind Kind;
            public float Time;
            public ProjectileStep First;
            public ProjectileStep Second;
            public RaycastHit Hit;
            public int Priority;
            public ulong StableKey;
        }
        Material projectileMaterial;
        public void Initialize(GameplaySettings settings, int enemyCount, Material material)
        { Settings = settings; Rules = new MatchRules(enemyCount); projectileMaterial = material; }
        public void Register(TankActor tank) { tanks.Add(tank); if (tank.IsPlayer) Player = tank; }
        public ProjectileActor SpawnProjectile(TankActor owner, Vector3 direction)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile"; go.layer = CollisionQueries.ProjectileLayer; go.transform.SetParent(transform);
            go.transform.position = owner.transform.position;
            go.transform.localScale = Vector3.one * (Settings.projectileRadius * 2);
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().sharedMaterial = projectileMaterial;
            var projectile = go.AddComponent<ProjectileActor>();
            projectile.Initialize(owner, Settings, direction);
            projectiles.Add(projectile);
            Physics.SyncTransforms();
            projectile.Advance(Settings.muzzleDistance);
            return projectile;
        }
        public void Register(DestructibleWallActor wall) { destructibleWalls.Add(wall); }
        public void Register(MineActor mine) { mines.Add(mine); }
        void Update() { if (AutoTick) Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if (Rules == null || Rules.State != MatchState.Playing) return;
            foreach (var tank in tanks)
            {
                if (Rules.State != MatchState.Playing) break;
                Vector3 start = tank.transform.position;
                tank.Tick(deltaTime);
                Physics.SyncTransforms();
                foreach (var mine in mines)
                    if (mine != null && mine.IsArmed && mine.TryTrigger(tank, start, tank.transform.position)) break;
                if (Rules.State != MatchState.Playing) break;
            }
            if (Rules.State == MatchState.Playing) SimulateProjectiles(deltaTime);
            projectiles.RemoveAll(p => p == null || !p.IsAlive);
            destructibleWalls.RemoveAll(w => w == null || !w.IsAlive);
            mines.RemoveAll(m => m == null || !m.IsArmed);
        }
        void SimulateProjectiles(float deltaTime)
        {
            var steps = new List<ProjectileStep>(projectiles.Count);
            foreach (var projectile in projectiles)
                if (projectile != null && projectile.IsAlive)
                    steps.Add(new ProjectileStep { Projectile = projectile, RemainingTime = projectile.BeginSimulation(deltaTime) });

            int interactionCount = 0;
            while (Rules.State == MatchState.Playing && HasMovement(steps) && interactionCount++ < 128)
            {
                Physics.SyncTransforms();
                Interaction interaction = FindFirstInteraction(steps);
                if (interaction.Kind == InteractionKind.None)
                {
                    MoveAll(steps, float.PositiveInfinity);
                    break;
                }

                MoveAll(steps, interaction.Time);
                Physics.SyncTransforms();
                if (interaction.Kind == InteractionKind.Clash)
                {
                    if (interaction.First.Projectile.IsAlive && interaction.Second.Projectile.IsAlive)
                        interaction.First.Projectile.Clash(interaction.Second.Projectile);
                }
                else if (interaction.First.Projectile.IsAlive)
                {
                    bool reflected = interaction.First.Projectile.ResolveStaticHit(interaction.Hit);
                    if (reflected)
                        interaction.First.RemainingTime = Mathf.Max(0,
                            interaction.First.RemainingTime - interaction.First.Projectile.SurfaceSeparation / interaction.First.Projectile.Speed);
                }
            }

            foreach (var step in steps) step.Projectile.EndSimulation();
        }
        static bool HasMovement(List<ProjectileStep> steps)
        {
            foreach (var step in steps)
                if (step.Projectile.IsAlive && step.RemainingTime > CollisionQueries.TimeEpsilon) return true;
            return false;
        }
        static void MoveAll(List<ProjectileStep> steps, float elapsed)
        {
            foreach (var step in steps)
            {
                if (!step.Projectile.IsAlive || step.RemainingTime <= 0) continue;
                float movementTime = Mathf.Min(step.RemainingTime, elapsed);
                step.Projectile.Move(movementTime);
                step.RemainingTime = Mathf.Max(0, step.RemainingTime - movementTime);
            }
        }
        internal static Interaction FindFirstInteraction(List<ProjectileStep> steps)
        {
            var interactions = new List<Interaction>();
            var staticHits = new List<RaycastHit>();
            foreach (var step in steps)
            {
                var projectile = step.Projectile;
                if (!projectile.IsAlive || step.RemainingTime <= CollisionQueries.TimeEpsilon) continue;
                var ignore = projectile.Rules.Reflections == 0 ? projectile.Shooter : null;
                float distance = projectile.Speed * step.RemainingTime;
                CollisionQueries.CollectStaticHits(projectile.transform.position, projectile.Radius,
                    projectile.Direction, distance, ignore, staticHits);
                foreach (var hit in staticHits)
                {
                    float time = hit.distance / projectile.Speed;
                    int priority = CollisionQueries.HitPriority(hit.collider);
                    ulong stableKey = CollisionQueries.StablePairKey(projectile, hit.collider);
                    interactions.Add(new Interaction
                    {
                        Kind = InteractionKind.Static, Time = time, First = step, Hit = hit,
                        Priority = priority, StableKey = stableKey
                    });
                }
            }
            for (int i = 0; i < steps.Count; i++)
            {
                var a = steps[i];
                if (!a.Projectile.IsAlive || a.RemainingTime <= CollisionQueries.TimeEpsilon) continue;
                for (int j = i + 1; j < steps.Count; j++)
                {
                    var b = steps[j];
                    if (!b.Projectile.IsAlive || b.RemainingTime <= CollisionQueries.TimeEpsilon) continue;
                    float horizon = Mathf.Min(a.RemainingTime, b.RemainingTime);
                    if (SweptSphereTime(a.Projectile, b.Projectile, horizon, out float time))
                    {
                        interactions.Add(new Interaction
                        {
                            Kind = InteractionKind.Clash, Time = time, First = a, Second = b,
                            Priority = CollisionQueries.ProjectilePriority,
                            StableKey = CollisionQueries.StablePairKey(a.Projectile, b.Projectile)
                        });
                    }
                }
            }
            var candidates = new List<CollisionQueries.SelectionCandidate>(interactions.Count);
            for (int i = 0; i < interactions.Count; i++)
                candidates.Add(new CollisionQueries.SelectionCandidate(
                    interactions[i].Time, interactions[i].Priority, interactions[i].StableKey, i));
            int selected = CollisionQueries.SelectCandidateIndex(candidates);
            return selected < 0 ? new Interaction { Kind = InteractionKind.None } : interactions[candidates[selected].SourceIndex];
        }
        static bool SweptSphereTime(ProjectileActor a, ProjectileActor b, float horizon, out float time)
        {
            Vector3 relativeStart = a.transform.position - b.transform.position;
            relativeStart.y = 0;
            Vector3 relativeVelocity = a.Direction * a.Speed - b.Direction * b.Speed;
            relativeVelocity.y = 0;
            float radius = a.Radius + b.Radius;
            double startX = relativeStart.x;
            double startZ = relativeStart.z;
            double velocityX = relativeVelocity.x;
            double velocityZ = relativeVelocity.z;
            double c = startX * startX + startZ * startZ - (double)radius * radius;
            if (c <= 0) { time = 0; return true; }
            double velocitySquared = velocityX * velocityX + velocityZ * velocityZ;
            if (velocitySquared <= CollisionQueries.TimeEpsilon) { time = 0; return false; }
            double halfB = startX * velocityX + startZ * velocityZ;
            if (halfB >= 0) { time = 0; return false; }
            double firstTerm = halfB * halfB;
            double secondTerm = velocitySquared * c;
            double discriminant = firstTerm - secondTerm;
            double tolerance = 1e-12 * System.Math.Max(1, System.Math.Abs(firstTerm) + System.Math.Abs(secondTerm));
            if (discriminant < -tolerance) { time = 0; return false; }
            if (discriminant < 0) discriminant = 0;
            time = (float)((-halfB - System.Math.Sqrt(discriminant)) / velocitySquared);
            return time >= 0 && time <= horizon + CollisionQueries.TimeEpsilon;
        }
    }
}
