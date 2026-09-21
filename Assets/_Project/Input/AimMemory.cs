using UnityEngine;
namespace TankGame.Input
{
    public sealed class AimMemory
    {
        Vector3 direction;
        public AimMemory(Vector3 initialDirection) { direction = initialDirection.normalized; }
        public Vector3 Resolve(Vector3 tankPosition, Vector3? validHitPoint)
        {
            if (validHitPoint.HasValue)
            {
                Vector3 candidate = validHitPoint.Value - tankPosition;
                candidate.y = 0;
                if (candidate.sqrMagnitude > 0.0001f) direction = candidate.normalized;
                return validHitPoint.Value;
            }
            // Retain a direction, not a stale world point, when moving with the cursor outside.
            return tankPosition + direction;
        }
    }
}
