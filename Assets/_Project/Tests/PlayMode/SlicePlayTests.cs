using System.Collections;
using NUnit.Framework;
using TankGame.Core;
using TankGame.Gameplay;
using TankGame.Input;
using TankGame.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankGame.Tests.PlayMode
{
    public sealed class SlicePlayTests
    {
        SliceBootstrap bootstrap;
        GameSession session;
        TankActor Player => session.Player;
        TankActor Enemy => session.Tanks[1];
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            bootstrap = Object.FindFirstObjectByType<SliceBootstrap>(); session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
        }
        [Test] public void MainHasValidReferencesAndStageOne()
        {
            Assert.That(bootstrap.stage.stageId, Is.EqualTo(1)); Assert.That(session.Tanks.Count, Is.GreaterThanOrEqualTo(2));
            foreach (var component in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                foreach (var attached in component.GetComponents<Component>()) Assert.That(attached, Is.Not.Null, component.name);
            Assert.That(bootstrap.gameCamera.orthographic, Is.True);
        }
        [Test] public void ThreeLiveShotsBlockFourthAndDespawnReturnsSlot()
        {
            Assert.That(Player.TryFire(), Is.True); Assert.That(Player.TryFire(), Is.True); Assert.That(Player.TryFire(), Is.True);
            Assert.That(Player.TryFire(), Is.False);
            session.Projectiles[0].Despawn(); session.Projectiles[0].Despawn();
            Assert.That(Player.Slots.Available, Is.EqualTo(1)); Assert.That(Player.TryFire(), Is.True);
        }
        ProjectileActor FreeProjectile(Vector3 position, Vector3 direction)
        {
            Player.TryFire(); var p = session.Projectiles[session.Projectiles.Count - 1];
            p.transform.position = position;
            // Initialize direction using a turret rotation before spawning in individual tests.
            Assert.That(Vector3.Dot(p.Direction, direction), Is.GreaterThan(0.99f));
            return p;
        }
        [Test] public void RealWallsReflectTwiceThenDespawnWithinOneFrame()
        {
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right);
            var p = FreeProjectile(new Vector3(0, session.Settings.planeHeight, -5), Vector3.right);
            p.Advance(10); Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.IsAlive, Is.True); Assert.That(p.Direction.x, Is.LessThan(0));
            p.Advance(20); Assert.That(p.Rules.Reflections, Is.EqualTo(2)); Assert.That(p.IsAlive, Is.True); Assert.That(p.Direction.x, Is.GreaterThan(0));
            p.Advance(20); Assert.That(p.IsAlive, Is.False); Assert.That(Player.Slots.Active, Is.Zero);
        }
        [Test] public void LargeFrameProcessesRemainingTravelAcrossMultipleWalls()
        {
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right);
            var p = FreeProjectile(new Vector3(0, session.Settings.planeHeight, -5), Vector3.right);
            p.Advance(60); Assert.That(p.Rules.Reflections, Is.EqualTo(2)); Assert.That(p.IsAlive, Is.False);
        }
        [Test] public void ProjectileHitDestroysEnemyAndWins()
        {
            Enemy.transform.position = Player.transform.position + Vector3.forward * 3; Physics.SyncTransforms();
            Player.TryFire(); session.Tick(0.3f);
            Assert.That(Enemy.Life.IsAlive, Is.False); Assert.That(Enemy.GetComponent<Collider>().enabled, Is.False);
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Victory));
            Enemy.Hit(); Assert.That(session.Rules.EnemiesRemaining, Is.Zero);
        }
        [Test] public void EnemyProjectileDestroysPlayerAndLoses()
        {
            Enemy.transform.position = Player.transform.position + Vector3.forward * 3;
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.back); Physics.SyncTransforms();
            Enemy.TryFire(); session.Tick(0.3f);
            Assert.That(Player.Life.IsAlive, Is.False); Assert.That(session.Rules.State, Is.EqualTo(MatchState.Defeat));
            Assert.That(Player.TryFire(), Is.False);
        }
        [UnityTest] public IEnumerator RestartDiscardsOldTanksAndProjectiles()
        {
            Player.TryFire(); var old = session.gameObject; Player.Hit();
            bootstrap.Restart(); bootstrap.Session.AutoTick = false;
            yield return null;
            Assert.That(old == null, Is.True); Assert.That(bootstrap.Session.Rules.State, Is.EqualTo(MatchState.Playing));
            Assert.That(bootstrap.Session.Player.Slots.Available, Is.EqualTo(3)); Assert.That(bootstrap.Session.Projectiles, Is.Empty);
            Assert.That(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [Test] public void LifetimeExpiryReturnsSlot()
        {
            Player.TryFire(); var p = session.Projectiles[0];
            p.transform.position = new Vector3(100, session.Settings.planeHeight, 100);
            p.Tick(session.Settings.projectileLifetime - 0.1f);
            Assert.That(p.IsAlive, Is.True);
            p.Tick(0.2f);
            Assert.That(p.IsAlive, Is.False); Assert.That(Player.Slots.Active, Is.Zero);
        }
        [Test] public void InitialShotDoesNotHitShooterButRicochetCan()
        {
            Player.transform.position = new Vector3(-6, session.Settings.planeHeight, -3);
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.back); Physics.SyncTransforms();
            Player.TryFire(); Assert.That(Player.Life.IsAlive, Is.True);
            session.Tick(1);
            Assert.That(Player.Life.IsAlive, Is.False); Assert.That(session.Rules.State, Is.EqualTo(MatchState.Defeat));
        }
        [Test] public void MuzzleCannotShootThroughWall()
        {
            Player.transform.position = new Vector3(-1.51f, session.Settings.planeHeight, 0);
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right); Physics.SyncTransforms();
            Player.TryFire(); var p = session.Projectiles[0];
            Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.transform.position.x, Is.LessThan(-1));
        }
        sealed class FixedCommand : ITankController
        {
            readonly Vector3 move;
            public FixedCommand(Vector3 move) { this.move = move; }
            public TankCommand ReadCommand(in TankObservation observation) => new TankCommand(move, observation.Position + Vector3.forward, false);
        }
        [Test] public void DiagonalMoveIsNormalizedAndIndependentOfBodyHeading()
        {
            var start = Player.transform.position; Player.Controller = new FixedCommand(new Vector3(1, 0, 1)); Player.Tick(0.1f);
            Assert.That(Vector3.Distance(start, Player.transform.position), Is.EqualTo(session.Settings.moveSpeed * 0.1f).Within(0.001f));
            Assert.That(Vector3.Angle(Player.Turret.forward, Vector3.forward), Is.LessThan(0.001f));
        }
        [Test] public void TankMovementCannotTunnelThroughWall()
        {
            Player.transform.position = new Vector3(-3, session.Settings.planeHeight, 0); Physics.SyncTransforms();
            Player.Controller = new FixedCommand(Vector3.right); Player.Tick(1);
            Assert.That(Player.transform.position.x, Is.LessThanOrEqualTo(-1.5f));
        }
        [Test] public void TerminalStateFreezesGameplay()
        {
            Player.TryFire(); var p = session.Projectiles[0]; var before = p.transform.position;
            Enemy.Hit(); Player.Controller = new FixedCommand(Vector3.right); var playerBefore = Player.transform.position;
            session.Tick(1);
            Assert.That(p.transform.position, Is.EqualTo(before)); Assert.That(Player.transform.position, Is.EqualTo(playerBefore));
        }
        [Test] public void MovementIsFrameRateIndependent()
        {
            var start = Player.transform.position; Player.Controller = new FixedCommand(Vector3.back);
            for (int i = 0; i < 10; i++) Player.Tick(0.01f);
            var fine = Player.transform.position; Player.transform.position = start; Physics.SyncTransforms(); Player.Tick(0.1f);
            Assert.That(Vector3.Distance(fine, Player.transform.position), Is.LessThan(0.001f));
        }
        [Test] public void BotPatrolsAimsAndFiresThroughControllerContract()
        {
            var botSettings = ScriptableObject.CreateInstance<BotSettings>();
            var controller = new BotTankController(botSettings, new[] { new Vector2(0, 3) });
            bool moved = false, fired = false;
            var observation = new TankObservation(Vector3.zero, Vector3.forward * 6, Vector3.forward, 0.1f, true);
            for (int i = 0; i < 100; i++)
            {
                var command = controller.ReadCommand(in observation);
                moved |= command.Move.sqrMagnitude > 0;
                fired |= command.Fire;
            }
            Object.Destroy(botSettings);
            Assert.That(moved, Is.True); Assert.That(fired, Is.True);
        }
        [Test] public void InvalidAimKeepsDirectionWhileTankMoves()
        {
            var memory = new AimMemory(Vector3.forward);
            memory.Resolve(Vector3.zero, Vector3.right * 5);
            var moved = new Vector3(3, 0, 7);
            Assert.That((memory.Resolve(moved, null) - moved).normalized, Is.EqualTo(Vector3.right));
        }
        [Test] public void RestartClickIsIgnoredUntilReleaseAndHoldDoesNotRepeat()
        {
            var gate = new FirePressGate();
            Assert.That(gate.Read(true, true), Is.False);
            Assert.That(gate.Read(false, true), Is.False);
            Assert.That(gate.Read(true, false), Is.False);
            Assert.That(gate.Read(true, true), Is.True);
            Assert.That(gate.Read(false, true), Is.False);
            Assert.That(gate.Read(false, false), Is.False);
            Assert.That(gate.Read(true, true), Is.True);
        }
    }
}
