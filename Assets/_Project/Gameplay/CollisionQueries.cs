using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
[assembly: InternalsVisibleTo("TankGame.Tests.PlayMode")]
namespace TankGame.Gameplay
{
    public static class CollisionQueries
    {
        internal readonly struct SelectionCandidate
        {
            public readonly float Time;
            public readonly int Priority;
            public readonly ulong StableKey;
            public readonly int SourceIndex;
            public SelectionCandidate(float time, int priority, ulong stableKey, int sourceIndex = 0)
            { Time = time; Priority = priority; StableKey = stableKey; SourceIndex = sourceIndex; }
        }
        public const float TimeEpsilon = 0.000001f;
        public const int WallLayer = 8;
        public const int TankLayer = 9;
        public const int ProjectileLayer = 10;
        public const int Mask = (1 << WallLayer) | (1 << TankLayer);
        public const int ProjectileMask = Mask | (1 << ProjectileLayer);
        internal const int ProjectilePriority = 0;
        internal const int DestructibleWallPriority = 1;
        internal const int TankPriority = 2;
        internal const int NormalWallPriority = 3;
        public static bool FirstHit(Vector3 origin, float radius, Vector3 direction, float distance, TankActor ignore, out RaycastHit closest)
        { return FirstHit(origin, radius, direction, distance, ignore, TimeEpsilon, out closest, out _); }
        internal static bool FirstHit(Vector3 origin, float radius, Vector3 direction, float distance,
            TankActor ignore, float comparisonEpsilon, out RaycastHit closest, out float minimumDistance)
        { return SelectHit(Physics.SphereCastAll(origin, radius, direction, distance, Mask, QueryTriggerInteraction.Ignore), ignore, null, comparisonEpsilon, out closest, out minimumDistance); }
        internal static void CollectStaticHits(Vector3 origin, float radius, Vector3 direction, float distance,
            TankActor ignore, List<RaycastHit> results)
        {
            results.Clear();
            foreach (var hit in Physics.SphereCastAll(origin, radius, direction, distance, Mask, QueryTriggerInteraction.Ignore))
            {
                if (ignore != null && hit.collider.GetComponent<TankActor>() == ignore) continue;
                results.Add(hit);
            }
        }
        public static bool FirstProjectileHit(Vector3 origin, float radius, Vector3 direction, float distance,
            TankActor ignoreTank, ProjectileActor ignoreProjectile, float comparisonEpsilon, out RaycastHit closest)
        { return SelectHit(Physics.SphereCastAll(origin, radius, direction, distance, ProjectileMask, QueryTriggerInteraction.Collide), ignoreTank, ignoreProjectile, comparisonEpsilon, out closest, out _); }
        static bool SelectHit(RaycastHit[] hits, TankActor ignoreTank, ProjectileActor ignoreProjectile,
            float comparisonEpsilon, out RaycastHit closest, out float minimumDistance)
        {
            closest = default;
            minimumDistance = float.PositiveInfinity;
            var candidates = new List<SelectionCandidate>(hits.Length);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                var projectile = hit.collider.GetComponent<ProjectileActor>();
                if ((ignoreProjectile != null && projectile == ignoreProjectile) || (projectile != null && !projectile.IsAlive)) continue;
                if (ignoreTank != null && hit.collider.GetComponent<TankActor>() == ignoreTank) continue;
                candidates.Add(new SelectionCandidate(hit.distance, HitPriority(hit.collider), StableKey(hit.collider), i));
            }
            int selected = SelectCandidateIndex(candidates, comparisonEpsilon);
            if (selected < 0) return false;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Time < minimumDistance) minimumDistance = candidates[i].Time;
            closest = hits[candidates[selected].SourceIndex];
            return true;
        }
        internal static int HitPriority(Collider collider)
        {
            if (collider.GetComponent<ProjectileActor>() != null) return ProjectilePriority;
            if (collider.GetComponent<DestructibleWallActor>() != null) return DestructibleWallPriority;
            if (collider.GetComponent<TankActor>() != null) return TankPriority;
            return NormalWallPriority;
        }
        internal static int SelectCandidateIndex(IReadOnlyList<SelectionCandidate> candidates, float epsilon = TimeEpsilon)
        {
            if (candidates == null || candidates.Count == 0) return -1;
            float minimumTime = float.PositiveInfinity;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Time < minimumTime) minimumTime = candidates[i].Time;
            int selected = -1;
            int priority = int.MaxValue;
            ulong stableKey = ulong.MaxValue;
            float limit = minimumTime + epsilon;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Time > limit) continue;
                if (candidate.Priority > priority || (candidate.Priority == priority && candidate.StableKey >= stableKey)) continue;
                selected = i; priority = candidate.Priority; stableKey = candidate.StableKey;
            }
            return selected;
        }
        internal static ulong StableKey(Object actor)
        { return actor == null ? ulong.MaxValue : unchecked((uint)actor.GetInstanceID()); }
        internal static ulong StablePairKey(Object first, Object second)
        {
            uint a = first == null ? uint.MaxValue : unchecked((uint)first.GetInstanceID());
            uint b = second == null ? uint.MaxValue : unchecked((uint)second.GetInstanceID());
            uint lower = a < b ? a : b;
            uint upper = a < b ? b : a;
            return ((ulong)lower << 32) | upper;
        }
    }
}
