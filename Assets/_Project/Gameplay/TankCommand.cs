using UnityEngine;
namespace TankGame.Gameplay
{
    public readonly struct TankCommand
    {
        public readonly Vector3 Move;
        public readonly Vector3 AimPoint;
        public readonly bool Fire;
        public readonly bool PlaceMine;
        public TankCommand(Vector3 move, Vector3 aimPoint, bool fire, bool placeMine = false)
        { Move = move; AimPoint = aimPoint; Fire = fire; PlaceMine = placeMine; }
    }
    public readonly struct TankObservation
    {
        public readonly Vector3 Position, Target, TurretForward;
        public readonly float DeltaTime;
        public readonly bool ClearShot;
        public readonly int AvailableShots;
        public TankObservation(Vector3 position, Vector3 target, Vector3 turretForward, float deltaTime, bool clearShot,
            int availableShots = int.MaxValue)
        {
            Position = position; Target = target; TurretForward = turretForward; DeltaTime = deltaTime;
            ClearShot = clearShot; AvailableShots = availableShots;
        }
    }
    public interface ITankController { TankCommand ReadCommand(in TankObservation observation); }
    public interface IStoppableTankController { void Stop(); }
}
