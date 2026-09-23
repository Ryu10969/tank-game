using UnityEngine;

namespace TankGame.Gameplay
{
    [CreateAssetMenu(menuName = "Tank Game/Gameplay Settings")]
    public sealed class GameplaySettings : ScriptableObject
    {
        [Min(1)] public int shotCapacity = 3;
        [Min(0)] public int maximumReflections = 1;
        [Min(0.1f)] public float moveSpeed = 4;
        [Min(1)] public float bodyTurnSpeed = 540;
        [Min(1)] public float turretTurnSpeed = 720;
        [Min(0.1f)] public float tankRadius = 0.5f;
        [Min(0)] public float planeHeight = 0.55f;
        [Min(0.1f)] public float projectileSpeed = 10;
        [Min(0.01f)] public float projectileRadius = 0.1f;
        [Min(0.1f)] public float projectileLifetime = 8;
        [Min(0.0001f)] public float surfaceSeparation = 0.002f;
        [Min(0)] public float muzzleDistance = 0.9f;
        [Min(1)] public int defaultDurability = 1;
        [Min(2)] public int heavyDurability = 2;
        [Min(0.1f)] public float heavyMoveSpeed = 1.2f;
        [Min(0.1f)] public float heavyProjectileSpeed = 7;
        [Min(1)] public int burstShotCount = 3;
        [Min(0.01f)] public float burstSpacingSeconds = 0.18f;
        [Min(0.01f)] public float burstReloadSeconds = 2;
        [Min(1)] public int mineCapacity = 2;
        [Min(0.01f)] public float mineFuseSeconds = 1;
        [Min(0.01f)] public float mineBlastRadius = 1.05f;
        [Min(0.01f)] public float stageTitleSeconds = 0.5f;
        [Min(0.01f)] public float stageGoSeconds = 0.5f;
        [Min(0.01f)] public float stageClearSeconds = 0.75f;
    }
}
