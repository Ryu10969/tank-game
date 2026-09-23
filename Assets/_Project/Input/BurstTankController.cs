using TankGame.Gameplay;
using UnityEngine;

namespace TankGame.Input
{
    public sealed class BurstTankController : ITankController, IStoppableTankController
    {
        readonly ITankController inner;
        readonly int shotCount;
        readonly float spacingSeconds;
        readonly float reloadSeconds;
        int remainingShots;
        float spacingElapsed;
        float reloadRemaining;
        bool stopped;

        public int RemainingShots => remainingShots;
        public float ReloadRemaining => reloadRemaining;

        public BurstTankController(ITankController inner, int shotCount, float spacingSeconds, float reloadSeconds)
        {
            this.inner = inner;
            this.shotCount = Mathf.Max(1, shotCount);
            this.spacingSeconds = Mathf.Max(0.01f, spacingSeconds);
            this.reloadSeconds = Mathf.Max(0.01f, reloadSeconds);
        }

        public TankCommand ReadCommand(in TankObservation observation)
        {
            if (stopped) return new TankCommand(Vector3.zero, observation.Position + observation.TurretForward, false);

            reloadRemaining = Mathf.Max(0, reloadRemaining - observation.DeltaTime);
            var command = inner.ReadCommand(in observation);
            if (remainingShots > 0)
            {
                if (!observation.ClearShot)
                {
                    remainingShots = 0;
                    spacingElapsed = 0;
                    reloadRemaining = reloadSeconds;
                    return new TankCommand(command.Move, command.AimPoint, false, command.PlaceMine);
                }
                spacingElapsed += observation.DeltaTime;
                bool fire = spacingElapsed + 0.00001f >= spacingSeconds && observation.AvailableShots > 0;
                if (fire)
                {
                    spacingElapsed = 0;
                    remainingShots--;
                    if (remainingShots == 0) reloadRemaining = reloadSeconds;
                }
                return new TankCommand(command.Move, command.AimPoint, fire, command.PlaceMine);
            }

            bool beginBurst = command.Fire && observation.ClearShot && reloadRemaining <= 0 &&
                observation.AvailableShots >= shotCount;
            if (beginBurst)
            {
                remainingShots = shotCount - 1;
                spacingElapsed = 0;
                if (remainingShots == 0) reloadRemaining = reloadSeconds;
            }
            return new TankCommand(command.Move, command.AimPoint, beginBurst, command.PlaceMine);
        }

        public void Stop()
        {
            stopped = true;
            remainingShots = 0;
            if (inner is IStoppableTankController stoppable) stoppable.Stop();
        }
    }
}
