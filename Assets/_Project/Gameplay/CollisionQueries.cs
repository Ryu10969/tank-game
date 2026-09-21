using UnityEngine;
namespace TankGame.Gameplay
{
    public static class CollisionQueries
    {
        public const int WallLayer = 8;
        public const int TankLayer = 9;
        public const int Mask = (1 << WallLayer) | (1 << TankLayer);
        public static bool FirstHit(Vector3 origin, float radius, Vector3 direction, float distance, TankActor ignore, out RaycastHit closest)
        {
            closest = default;
            float nearest = float.PositiveInfinity;
            // All hits are needed to filter the shooter without changing global collision state.
            foreach (var hit in Physics.SphereCastAll(origin, radius, direction, distance, Mask, QueryTriggerInteraction.Ignore))
            {
                if (ignore != null && hit.collider.GetComponent<TankActor>() == ignore) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                closest = hit;
            }
            return nearest < float.PositiveInfinity;
        }
    }
}
