using TankGame.Gameplay;
using UnityEngine;
namespace TankGame.Input
{
    public sealed class SentryTankController : ITankController
    {
        readonly BotSettings settings;
        float elapsed;
        float cooldown;
        public SentryTankController(BotSettings settings) { this.settings = settings; }
        public TankCommand ReadCommand(in TankObservation observation)
        {
            elapsed += observation.DeltaTime;
            cooldown = Mathf.Max(0, cooldown - observation.DeltaTime);
            bool aimed = Vector3.Angle(observation.TurretForward, observation.Target - observation.Position) <= settings.aimToleranceDegrees;
            bool fire = observation.ClearShot && aimed && elapsed >= settings.reactionTimeSeconds && cooldown <= 0;
            if (fire) { cooldown = settings.fireCooldownSeconds; elapsed = 0; }
            return new TankCommand(Vector3.zero, observation.Target, fire);
        }
    }
}
