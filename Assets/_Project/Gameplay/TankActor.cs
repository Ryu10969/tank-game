using TankGame.Core;
using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class TankActor : MonoBehaviour
    {
        public TankLife Life { get; private set; }
        public ShotSlots Slots { get; private set; }
        public bool IsPlayer { get; private set; }
        public Transform Body { get; private set; }
        public Transform Turret { get; private set; }
        public ITankController Controller { get; set; }
        GameSession session;
        GameplaySettings settings;
        float speed;

        public void Initialize(GameSession owner, bool player, Transform body, Transform turret, float moveSpeed)
        {
            session = owner; settings = owner.Settings; IsPlayer = player;
            Body = body; Turret = turret; speed = moveSpeed;
            Life = new TankLife(); Slots = new ShotSlots(settings.shotCapacity);
        }
        public void Tick(float deltaTime)
        {
            if (!Life.IsAlive || Controller == null) return;
            Vector3 target = session.Player.transform.position;
            Vector3 toTarget = target - transform.position;
            bool clear = !Physics.Raycast(transform.position, toTarget.normalized, toTarget.magnitude, 1 << CollisionQueries.WallLayer);
            var observation = new TankObservation(transform.position, target, Turret.forward, deltaTime, clear);
            var command = Controller.ReadCommand(in observation);
            // Interpret aim against the same position snapshot used by the controller.
            Vector3 aim = command.AimPoint - transform.position; aim.y = 0;
            Vector3 move = Vector3.ClampMagnitude(new Vector3(command.Move.x, 0, command.Move.z), 1);
            if (move.sqrMagnitude > 0)
            {
                Body.rotation = Quaternion.RotateTowards(Body.rotation, Quaternion.LookRotation(move), settings.bodyTurnSpeed * deltaTime);
                float distance = speed * move.magnitude * deltaTime;
                if (CollisionQueries.FirstHit(transform.position, settings.tankRadius, move.normalized, distance + settings.surfaceSeparation, this, out var hit))
                    distance = Mathf.Max(0, hit.distance - settings.surfaceSeparation);
                transform.position += move.normalized * distance;
            }
            if (aim.sqrMagnitude > 0.0001f)
                Turret.rotation = Quaternion.RotateTowards(Turret.rotation, Quaternion.LookRotation(aim), settings.turretTurnSpeed * deltaTime);
            if (command.Fire) TryFire();
        }
        public bool TryFire()
        {
            if (!Life.IsAlive || session.Rules.State != MatchState.Playing || !Slots.TryAcquire()) return false;
            session.SpawnProjectile(this, Turret.forward);
            return true;
        }
        public void Hit()
        {
            if (session.Rules.State != MatchState.Playing || !Life.Hit()) return;
            GetComponent<Collider>().enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            session.Rules.TankDestroyed(IsPlayer);
        }
    }
}
