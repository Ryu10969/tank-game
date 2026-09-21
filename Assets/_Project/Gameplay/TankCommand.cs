using UnityEngine;
namespace TankGame.Gameplay
{
    public readonly struct TankCommand
    {
        public readonly Vector3 Move;
        public readonly Vector3 AimPoint;
        public readonly bool Fire;
        public TankCommand(Vector3 move, Vector3 aimPoint, bool fire) { Move = move; AimPoint = aimPoint; Fire = fire; }
    }
    public readonly struct TankObservation
    {
        public readonly Vector3 Position, Target, TurretForward;
        public readonly float DeltaTime;
        public readonly bool ClearShot;
        public TankObservation(Vector3 position, Vector3 target, Vector3 turretForward, float deltaTime, bool clearShot)
        { Position = position; Target = target; TurretForward = turretForward; DeltaTime = deltaTime; ClearShot = clearShot; }
    }
    public interface ITankController { TankCommand ReadCommand(in TankObservation observation); }
}
