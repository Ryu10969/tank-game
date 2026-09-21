using TankGame.Gameplay;
using UnityEngine;
namespace TankGame.Input
{
    public sealed class BotTankController : ITankController
    {
        public enum BotState { Observe, Move, Aim, Fire, Recover }
        public BotState State { get; private set; }
        readonly BotSettings settings;
        readonly Vector2[] patrol;
        int destination;
        float elapsed;
        float cooldown;
        public BotTankController(BotSettings settings, Vector2[] patrol) { this.settings = settings; this.patrol = patrol; }
        public TankCommand ReadCommand(in TankObservation observation)
        {
            elapsed += observation.DeltaTime;
            cooldown = Mathf.Max(0, cooldown - observation.DeltaTime);
            Vector3 aim = observation.Target;
            Vector3 targetDirection = aim - observation.Position;
            targetDirection = Quaternion.AngleAxis(settings.aimErrorDegrees, Vector3.up) * targetDirection;
            aim = observation.Position + targetDirection;
            Vector3 move = Vector3.zero;
            bool fire = false;
            switch (State)
            {
                case BotState.Observe:
                    if (elapsed >= settings.reactionTimeSeconds) Enter(BotState.Move);
                    break;
                case BotState.Move:
                    Vector3 offset = StageDefinition.World(patrol[destination], observation.Position.y) - observation.Position;
                    move = Vector3.ClampMagnitude(offset / Mathf.Max(settings.moveSpeed * observation.DeltaTime, 0.0001f), 1);
                    if (offset.magnitude <= settings.arrivalDistance || elapsed >= settings.moveDurationSeconds)
                    {
                        if (offset.magnitude <= settings.arrivalDistance) destination = (destination + 1) % patrol.Length;
                        Enter(BotState.Aim);
                    }
                    break;
                case BotState.Aim:
                    if (!observation.ClearShot) Enter(BotState.Recover);
                    else if (Vector3.Angle(observation.TurretForward, targetDirection) <= settings.aimToleranceDegrees) Enter(BotState.Fire);
                    break;
                case BotState.Fire:
                    fire = observation.ClearShot && cooldown <= 0;
                    if (fire) cooldown = settings.fireCooldownSeconds;
                    Enter(BotState.Recover);
                    break;
                case BotState.Recover:
                    if (elapsed >= settings.reactionTimeSeconds && cooldown <= 0) Enter(BotState.Observe);
                    break;
            }
            return new TankCommand(move, aim, fire);
        }
        void Enter(BotState next) { State = next; elapsed = 0; }
    }
}
