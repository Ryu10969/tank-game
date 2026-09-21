using UnityEngine;
namespace TankGame.Gameplay
{
    [CreateAssetMenu(menuName = "Tank Game/Bot Settings")]
    public sealed class BotSettings : ScriptableObject
    {
        public float reactionTimeSeconds = 0.7f;
        public float aimErrorDegrees;
        public float aimToleranceDegrees = 1;
        public float fireCooldownSeconds = 2;
        public float moveSpeed = 1.5f;
        public bool ricochetAwareness;
        public float moveDurationSeconds = 1.2f;
        public float arrivalDistance = 0.15f;
    }
}
