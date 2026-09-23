using System.Collections;
using System.Collections.Generic;
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
        TankActor SecondEnemy => session.Tanks[2];
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            bootstrap = Object.FindFirstObjectByType<SliceBootstrap>();
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
        }
        [Test] public void MainHasValidReferencesAndStageOne()
        {
            Assert.That(bootstrap.stages.Length, Is.EqualTo(3));
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(1)); Assert.That(session.Tanks.Count, Is.GreaterThanOrEqualTo(2));
            foreach (var component in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                foreach (var attached in component.GetComponents<Component>()) Assert.That(attached, Is.Not.Null, component.name);
            Assert.That(bootstrap.gameCamera.orthographic, Is.True);
        }
        [Test] public void ThreeLiveShotsBlockFourthAndDespawnReturnsSlot()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.That(Player.TryFire(), Is.True);
                session.Projectiles[i].transform.position = new Vector3(100 + i * 2, session.Settings.planeHeight, 100);
                Physics.SyncTransforms();
            }
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
        ProjectileActor SpawnFree(TankActor owner, Vector3 position, Vector3 direction)
        {
            owner.Turret.rotation = Quaternion.LookRotation(direction); Physics.SyncTransforms();
            Assert.That(owner.TryFire(), Is.True);
            var projectile = session.Projectiles[session.Projectiles.Count - 1];
            projectile.transform.position = position; Physics.SyncTransforms();
            return projectile;
        }
        void MoveTanksAway()
        {
            for (int i = 0; i < session.Tanks.Count; i++)
                session.Tanks[i].transform.position = new Vector3(40 + i * 3, session.Settings.planeHeight, 40);
            Physics.SyncTransforms();
        }
        GameObject CreateTestWall(float leftFace, float z, bool destructible)
        {
            var wall = new GameObject(destructible ? "Test Destructible Wall" : "Test Normal Wall");
            wall.layer = CollisionQueries.WallLayer; wall.transform.SetParent(session.transform);
            wall.transform.position = new Vector3(leftFace + 0.5f, 0.75f, z);
            wall.transform.localScale = new Vector3(1, 1.5f, 1);
            wall.AddComponent<BoxCollider>();
            if (destructible) session.Register(wall.AddComponent<DestructibleWallActor>());
            Physics.SyncTransforms();
            return wall;
        }
        void PositionPlayerForMuzzle(float z)
        {
            Player.transform.position = new Vector3(-0.9f, session.Settings.planeHeight, z);
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right);
            Physics.SyncTransforms();
        }
        static CollisionQueries.SelectionCandidate ChainCandidate(int id, bool reversePriority)
        {
            float epsilon = CollisionQueries.TimeEpsilon;
            if (!reversePriority)
            {
                if (id == 0) return new CollisionQueries.SelectionCandidate(0, CollisionQueries.NormalWallPriority, 100);
                if (id == 1) return new CollisionQueries.SelectionCandidate(0.75f * epsilon, CollisionQueries.TankPriority, 200);
                return new CollisionQueries.SelectionCandidate(1.5f * epsilon, CollisionQueries.ProjectilePriority, 300);
            }
            if (id == 0) return new CollisionQueries.SelectionCandidate(0, CollisionQueries.ProjectilePriority, 100);
            if (id == 1) return new CollisionQueries.SelectionCandidate(0.75f * epsilon, CollisionQueries.TankPriority, 200);
            return new CollisionQueries.SelectionCandidate(1.5f * epsilon, CollisionQueries.NormalWallPriority, 300);
        }
        [TestCase(0, 1, 2)]
        [TestCase(0, 2, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)]
        [TestCase(2, 0, 1)]
        [TestCase(2, 1, 0)]
        public void ProductionSelectionEpsilonChainUsesImmutableMinimumForEveryPermutation(int first, int second, int third)
        {
            var candidates = new[] { ChainCandidate(first, false), ChainCandidate(second, false), ChainCandidate(third, false) };
            int selected = CollisionQueries.SelectCandidateIndex(candidates);
            Assert.That(candidates[selected].StableKey, Is.EqualTo(200));
        }
        [TestCase(0, 1, 2)]
        [TestCase(0, 2, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)]
        [TestCase(2, 0, 1)]
        [TestCase(2, 1, 0)]
        public void ReverseEpsilonChainUsesImmutableMinimumForEveryPermutation(int first, int second, int third)
        {
            var candidates = new[] { ChainCandidate(first, true), ChainCandidate(second, true), ChainCandidate(third, true) };
            int selected = CollisionQueries.SelectCandidateIndex(candidates);
            Assert.That(candidates[selected].StableKey, Is.EqualTo(100));
        }
        [TestCase(0.75f, true)]
        [TestCase(0.99f, true)]
        [TestCase(1f, true)]
        [TestCase(1.01f, false)]
        [TestCase(1.5f, false)]
        public void EpsilonBoundaryAnchorsPriorityToAbsoluteMinimum(float multiplier, bool laterHighPriorityWins)
        {
            var candidates = new[]
            {
                new CollisionQueries.SelectionCandidate(0, CollisionQueries.NormalWallPriority, 100),
                new CollisionQueries.SelectionCandidate(multiplier * CollisionQueries.TimeEpsilon, CollisionQueries.ProjectilePriority, 200)
            };
            int selected = CollisionQueries.SelectCandidateIndex(candidates);
            Assert.That(candidates[selected].StableKey, Is.EqualTo(laterHighPriorityWins ? 200ul : 100ul));
        }
        void LoadStageTwo()
        {
            Enemy.Hit(); Assert.That(bootstrap.AdvanceStageIfCleared(), Is.True);
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            Physics.SyncTransforms();
        }
        void LoadStageThree()
        {
            LoadStageTwo();
            Enemy.Hit(); SecondEnemy.Hit(); Assert.That(bootstrap.AdvanceStageIfCleared(), Is.True);
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            Physics.SyncTransforms();
        }
        [Test] public void PlayerProjectileReflectsOnceThenSecondWallDespawnsIt()
        {
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right);
            var p = FreeProjectile(new Vector3(0, session.Settings.planeHeight, -5), Vector3.right);
            p.Advance(10); Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.IsAlive, Is.True); Assert.That(p.Direction.x, Is.LessThan(0));
            p.Advance(20); Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.IsAlive, Is.False); Assert.That(Player.Slots.Active, Is.Zero);
        }
        [Test] public void LargeFrameProcessesRemainingTravelAcrossMultipleWalls()
        {
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.right);
            var p = FreeProjectile(new Vector3(0, session.Settings.planeHeight, -5), Vector3.right);
            p.Advance(60); Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.IsAlive, Is.False);
        }
        [Test] public void EnemyProjectileUsesTheSameOneReflectionLimit()
        {
            Enemy.transform.position = new Vector3(0, session.Settings.planeHeight, -5);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.right); Physics.SyncTransforms();
            Assert.That(Enemy.TryFire(), Is.True); var p = session.Projectiles[0];
            p.Advance(10); Assert.That(p.Rules.Reflections, Is.EqualTo(1)); Assert.That(p.IsAlive, Is.True);
            p.Advance(20); Assert.That(p.IsAlive, Is.False); Assert.That(Enemy.Slots.Active, Is.Zero);
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
        [UnityTest] public IEnumerator StageTwoDefeatRestartReturnsCleanStageOne()
        {
            LoadStageTwo();
            var oldSession = session; var oldPlayer = Player; var oldEnemy = Enemy;
            Assert.That(Player.TryFire(), Is.True); var oldProjectile = session.Projectiles[0];
            Assert.That(Player.Slots.Available, Is.EqualTo(2)); Assert.That(session.Projectiles.Count, Is.EqualTo(1));
            Player.Hit(); Assert.That(session.Rules.State, Is.EqualTo(MatchState.Defeat));
            bootstrap.Restart(); bootstrap.Session.AutoTick = false;
            yield return null;
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(1));
            Assert.That(bootstrap.Session.Player.Slots.Available, Is.EqualTo(3));
            Assert.That(bootstrap.Session.Projectiles, Is.Empty);
            Assert.That(bootstrap.Session.Rules.State, Is.EqualTo(MatchState.Playing));
            Assert.That(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(RuntimeRootCount(), Is.EqualTo(1));
            Assert.That(oldSession == null, Is.True); Assert.That(oldPlayer == null, Is.True);
            Assert.That(oldEnemy == null, Is.True); Assert.That(oldProjectile == null, Is.True);
        }
        [UnityTest] public IEnumerator StageOneClearLoadsDistinctStageTwoOnce()
        {
            var stageOneSession = session;
            Enemy.Hit();
            Assert.That(bootstrap.AdvanceStageIfCleared(), Is.True);
            Assert.That(bootstrap.AdvanceStageIfCleared(), Is.False);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            yield return null;
            Assert.That(stageOneSession == null, Is.True);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(2));
            Assert.That(Player.transform.position.x, Is.EqualTo(6).Within(0.001f));
            Assert.That(Enemy.transform.position.x, Is.EqualTo(-6).Within(0.001f));
            Assert.That(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FinalStageVictoryDoesNotLoadMissingStage()
        {
            LoadStageThree();
            Enemy.Hit(); Enemy.Hit(); SecondEnemy.Hit(); yield return null;
            bootstrap.TickPresentation(bootstrap.settings.stageClearSeconds);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(3));
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Victory));
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.FinalVictory));
            Assert.That(session.TryPlaceMine(Player), Is.False);
            Assert.That(bootstrap.AdvanceStageIfCleared(), Is.False);
        }
        [Test] public void PresentationProgressesStageOneToTwoToThreeAndStopsAtFinalVictory()
        {
            Enemy.Hit();
            bootstrap.TickPresentation(bootstrap.settings.stageClearSeconds);
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(2));
            Enemy.Hit(); Enemy.Hit(); SecondEnemy.Hit();
            bootstrap.TickPresentation(bootstrap.settings.stageClearSeconds);
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(3));
            Enemy.Hit(); Enemy.Hit(); SecondEnemy.Hit();
            bootstrap.TickPresentation(bootstrap.settings.stageClearSeconds);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(3));
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.FinalVictory));
            Assert.That(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [Test] public void StageTwoRequiresEveryEnemyForVictoryAndSlotsAreIndependent()
        {
            LoadStageTwo();
            Assert.That(session.Tanks.Count, Is.EqualTo(3));
            Assert.That(Enemy.Slots.Available, Is.EqualTo(3)); Assert.That(SecondEnemy.Slots.Available, Is.EqualTo(3));
            Enemy.Hit();
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Playing));
            Assert.That(session.Rules.EnemiesRemaining, Is.EqualTo(1));
            SecondEnemy.Hit();
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Victory));
        }
        [TestCase(0, 1)]
        [TestCase(0, 0)]
        public void ProjectileClashDespawnsBothAndReturnsOwnerSlots(int firstOwnerIndex, int secondOwnerIndex)
        {
            var firstOwner = session.Tanks[firstOwnerIndex]; var secondOwner = session.Tanks[secondOwnerIndex];
            var first = SpawnFree(firstOwner, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(secondOwner, new Vector3(1, session.Settings.planeHeight, -5), Vector3.left);
            int firstBefore = firstOwner.Slots.Active; int secondBefore = secondOwner.Slots.Active;
            first.Advance(3);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            Assert.That(firstOwner.Life.IsAlive, Is.True); Assert.That(secondOwner.Life.IsAlive, Is.True);
            if (firstOwner == secondOwner) Assert.That(firstOwner.Slots.Active, Is.EqualTo(firstBefore - 2));
            else
            {
                Assert.That(firstOwner.Slots.Active, Is.EqualTo(firstBefore - 1));
                Assert.That(secondOwner.Slots.Active, Is.EqualTo(secondBefore - 1));
            }
            first.Despawn(); second.Despawn();
            Assert.That(firstOwner.Slots.Active, Is.Zero); Assert.That(secondOwner.Slots.Active, Is.Zero);
        }
        [Test] public void EnemyProjectilesClashAcrossDifferentEnemies()
        {
            LoadStageTwo();
            var first = SpawnFree(Enemy, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(SecondEnemy, new Vector3(1, session.Settings.planeHeight, -5), Vector3.left);
            first.Advance(3);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            Assert.That(Enemy.Slots.Available, Is.EqualTo(3)); Assert.That(SecondEnemy.Slots.Available, Is.EqualTo(3));
        }
        [Test] public void MovingProjectilesClashHeadOnAndReturnSlotsOnce()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(2, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.3f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            Assert.That(Player.Slots.Active, Is.Zero); Assert.That(Enemy.Slots.Active, Is.Zero);
            first.Despawn(); second.Despawn();
            Assert.That(Player.Slots.Active, Is.Zero); Assert.That(Enemy.Slots.Active, Is.Zero);
        }
        [Test] public void SameDirectionSameSpeedProjectilesDoNotClash()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-3, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(-1.5f, session.Settings.planeHeight, -5), Vector3.right);
            session.Tick(0.4f);
            Assert.That(first.IsAlive, Is.True); Assert.That(second.IsAlive, Is.True);
            Assert.That(second.transform.position.x - first.transform.position.x, Is.EqualTo(1.5f).Within(0.001f));
        }
        [Test] public void PerpendicularProjectilesClashOnlyWhenTheyReachCrossingTogether()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, -6), Vector3.forward);
            session.Tick(0.2f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
        }
        [Test] public void GeometricallyCrossingPathsAtDifferentTimesDoNotClash()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, -3), Vector3.back);
            session.Tick(0.3f);
            Assert.That(first.IsAlive, Is.True); Assert.That(second.IsAlive, Is.True);
        }
        [Test] public void HighDeltaStillDetectsMovingProjectileClash()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-4, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(4, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.5f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
        }
        [Test] public void ProjectileClashBeforeTankPreventsDamage()
        {
            LoadStageTwo(); MoveTanksAway();
            Enemy.transform.position = new Vector3(3, session.Settings.planeHeight, -5); Physics.SyncTransforms();
            var first = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Player, new Vector3(1, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.5f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            Assert.That(Enemy.Life.IsAlive, Is.True);
        }
        [Test] public void TankHitBeforeProjectileClashWinsByTimeOfImpact()
        {
            LoadStageTwo(); MoveTanksAway();
            Enemy.transform.position = new Vector3(0, session.Settings.planeHeight, -5); Physics.SyncTransforms();
            var first = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Player, new Vector3(2, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.3f);
            Assert.That(Enemy.Life.IsAlive, Is.False);
            Assert.That(first.IsAlive ^ second.IsAlive, Is.True);
        }
        [Test] public void ProjectileClashBeforeWallDoesNotReflect()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(7, session.Settings.planeHeight, -5), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(8, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.3f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            Assert.That(first.Rules.Reflections, Is.Zero);
        }
        [Test] public void WallBeforeClashReflectsThenClashesDuringRemainingTick()
        {
            MoveTanksAway();
            var reflected = SpawnFree(Player, new Vector3(8, session.Settings.planeHeight, -5), Vector3.right);
            var other = SpawnFree(Enemy, new Vector3(7, session.Settings.planeHeight, -5), Vector3.right);
            session.Tick(0.35f);
            Assert.That(reflected.Rules.Reflections, Is.EqualTo(1));
            Assert.That(reflected.IsAlive, Is.False); Assert.That(other.IsAlive, Is.False);
        }
        [Test] public void ProjectileClashResultDoesNotDependOnRegistrationOrder()
        {
            MoveTanksAway();
            var leftFirst = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, -5), Vector3.right);
            var rightSecond = SpawnFree(Enemy, new Vector3(2, session.Settings.planeHeight, -5), Vector3.left);
            session.Tick(0.3f);
            Assert.That(leftFirst.IsAlive || rightSecond.IsAlive, Is.False);

            var rightFirst = SpawnFree(Enemy, new Vector3(2, session.Settings.planeHeight, -4), Vector3.left);
            var leftSecond = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, -4), Vector3.right);
            session.Tick(0.3f);
            Assert.That(rightFirst.IsAlive || leftSecond.IsAlive, Is.False);
        }
        [Test] public void SameTimeProjectileClashBeatsTankHitInSessionSimulation()
        {
            LoadStageTwo(); MoveTanksAway();
            const float z = 100;
            Enemy.transform.position = new Vector3(-0.3f, session.Settings.planeHeight, z);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.left); Physics.SyncTransforms();
            var left = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, z), Vector3.right);
            var right = SpawnFree(Enemy, new Vector3(0.4f, session.Settings.planeHeight, z), Vector3.left);
            session.Tick(0.2f);
            Assert.That(left.IsAlive, Is.False); Assert.That(right.IsAlive, Is.False);
            Assert.That(Enemy.Life.IsAlive, Is.True);
        }
        [Test] public void SameTimeProjectileAndTankTieIsIndependentOfProjectileRegistrationOrder()
        {
            LoadStageTwo(); MoveTanksAway();
            Enemy.transform.position = new Vector3(-0.3f, session.Settings.planeHeight, 100);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.left); Physics.SyncTransforms();
            var leftFirst = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, 100), Vector3.right);
            var rightSecond = SpawnFree(Enemy, new Vector3(0.4f, session.Settings.planeHeight, 100), Vector3.left);
            session.Tick(0.2f);
            Assert.That(leftFirst.IsAlive || rightSecond.IsAlive, Is.False);
            Assert.That(Enemy.Life.IsAlive, Is.True);

            Enemy.transform.position = new Vector3(-0.3f, session.Settings.planeHeight, 102);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.left); Physics.SyncTransforms();
            var rightFirst = SpawnFree(Enemy, new Vector3(0.4f, session.Settings.planeHeight, 102), Vector3.left);
            var leftSecond = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, 102), Vector3.right);
            session.Tick(0.2f);
            Assert.That(rightFirst.IsAlive || leftSecond.IsAlive, Is.False);
            Assert.That(Enemy.Life.IsAlive, Is.True);
        }
        [Test] public void StaticEpsilonChainKeepsAbsoluteMinimumAcrossColliderCreationOrder()
        {
            LoadStageTwo(); MoveTanksAway();
            float distanceEpsilon = session.Settings.projectileSpeed * CollisionQueries.TimeEpsilon;
            for (int i = 0; i < 2; i++)
            {
                float z = 100 + i * 2;
                PositionPlayerForMuzzle(z);
                Enemy.transform.position = new Vector3(0.3f + 1.5f * distanceEpsilon, session.Settings.planeHeight, z);
                DestructibleWallActor destructible;
                if (i == 0)
                {
                    CreateTestWall(-0.2f, z, false);
                    destructible = CreateTestWall(-0.2f + 0.75f * distanceEpsilon, z, true).GetComponent<DestructibleWallActor>();
                }
                else
                {
                    destructible = CreateTestWall(-0.2f + 0.75f * distanceEpsilon, z, true).GetComponent<DestructibleWallActor>();
                    CreateTestWall(-0.2f, z, false);
                }
                Physics.SyncTransforms(); Assert.That(Player.TryFire(), Is.True);
                Assert.That(destructible.IsAlive, Is.False);
                Assert.That(Enemy.Life.IsAlive, Is.True);
            }
        }
        GameSession.Interaction SelectGlobalStaticScenario(bool reverseProjectileRegistration,
            bool reverseColliderCreation, out DestructibleWallActor destructible)
        {
            MoveTanksAway();
            const float firstTime = 0.1f;
            const float firstLane = 110;
            const float secondLane = 112;
            float epsilonDistance = session.Settings.projectileSpeed * CollisionQueries.TimeEpsilon;
            ProjectileActor first;
            ProjectileActor second;
            if (reverseProjectileRegistration)
            {
                second = SpawnFree(Enemy, Vector3.up * session.Settings.planeHeight + Vector3.forward * secondLane, Vector3.right);
                first = SpawnFree(Player, Vector3.up * session.Settings.planeHeight + Vector3.forward * firstLane, Vector3.right);
            }
            else
            {
                first = SpawnFree(Player, Vector3.up * session.Settings.planeHeight + Vector3.forward * firstLane, Vector3.right);
                second = SpawnFree(Enemy, Vector3.up * session.Settings.planeHeight + Vector3.forward * secondLane, Vector3.right);
            }

            float firstFace = first.Radius + first.Speed * firstTime;
            if (reverseColliderCreation)
            {
                destructible = CreateTestWall(firstFace + 1.5f * epsilonDistance, secondLane, true).GetComponent<DestructibleWallActor>();
                CreateTestWall(firstFace + 0.75f * epsilonDistance, secondLane, false);
                CreateTestWall(firstFace, firstLane, false);
            }
            else
            {
                CreateTestWall(firstFace, firstLane, false);
                CreateTestWall(firstFace + 0.75f * epsilonDistance, secondLane, false);
                destructible = CreateTestWall(firstFace + 1.5f * epsilonDistance, secondLane, true).GetComponent<DestructibleWallActor>();
            }
            Physics.SyncTransforms();
            var steps = new List<GameSession.ProjectileStep>();
            foreach (var projectile in session.Projectiles)
                steps.Add(new GameSession.ProjectileStep
                {
                    Projectile = projectile,
                    RemainingTime = firstTime + 2 * CollisionQueries.TimeEpsilon
                });
            return GameSession.FindFirstInteraction(steps);
        }
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void GlobalStaticArbitrationExcludesLocallyPreferredHitOutsideWindow(
            bool reverseProjectileRegistration, bool reverseColliderCreation)
        {
            var selected = SelectGlobalStaticScenario(reverseProjectileRegistration,
                reverseColliderCreation, out var destructible);
            Assert.That(selected.Kind, Is.EqualTo(GameSession.InteractionKind.Static));
            Assert.That(selected.Time, Is.LessThanOrEqualTo(0.1f + CollisionQueries.TimeEpsilon));
            Assert.That(selected.Hit.collider.GetComponent<DestructibleWallActor>(), Is.Null);
            Assert.That(destructible.IsAlive, Is.True);
        }
        [TestCase(0.99f, true)]
        [TestCase(1f, true)]
        [TestCase(1.01f, false)]
        public void GlobalStaticBoundaryUsesActualInteractionTime(float epsilonMultiplier, bool destructibleWins)
        {
            MoveTanksAway();
            const float firstTime = 0.1f;
            const float firstLane = 114;
            const float secondLane = 116;
            var first = SpawnFree(Player, Vector3.up * session.Settings.planeHeight + Vector3.forward * firstLane, Vector3.right);
            SpawnFree(Enemy, Vector3.up * session.Settings.planeHeight + Vector3.forward * secondLane, Vector3.right);
            float firstFace = first.Radius + first.Speed * firstTime;
            CreateTestWall(firstFace, firstLane, false);
            var destructible = CreateTestWall(firstFace + epsilonMultiplier * first.Speed * CollisionQueries.TimeEpsilon,
                secondLane, true).GetComponent<DestructibleWallActor>();
            Physics.SyncTransforms();
            var steps = new List<GameSession.ProjectileStep>();
            foreach (var projectile in session.Projectiles)
                steps.Add(new GameSession.ProjectileStep
                {
                    Projectile = projectile,
                    RemainingTime = firstTime + 2 * CollisionQueries.TimeEpsilon
                });
            var selected = GameSession.FindFirstInteraction(steps);
            Assert.That(selected.Hit.collider.GetComponent<DestructibleWallActor>() == destructible,
                Is.EqualTo(destructibleWins));
        }
        [TestCase(0.99f, true)]
        [TestCase(1.01f, false)]
        public void GlobalClashBoundaryCannotOverrideEarlierWallOutsideWindow(
            float epsilonMultiplier, bool clashWins)
        {
            MoveTanksAway();
            const float firstTime = 0.1f;
            const float wallLane = 118;
            const float clashLane = 120;
            var wallProjectile = SpawnFree(Player,
                Vector3.up * session.Settings.planeHeight + Vector3.forward * wallLane, Vector3.right);
            var clashLeft = SpawnFree(Player,
                Vector3.up * session.Settings.planeHeight + Vector3.forward * clashLane, Vector3.right);
            float clashTime = firstTime + epsilonMultiplier * CollisionQueries.TimeEpsilon;
            float clashSeparation = clashLeft.Radius * 2 + clashLeft.Speed * 2 * clashTime;
            SpawnFree(Enemy, new Vector3(clashSeparation, session.Settings.planeHeight, clashLane), Vector3.left);
            CreateTestWall(wallProjectile.Radius + wallProjectile.Speed * firstTime, wallLane, false);
            Physics.SyncTransforms();
            var steps = new List<GameSession.ProjectileStep>();
            foreach (var projectile in session.Projectiles)
                steps.Add(new GameSession.ProjectileStep
                {
                    Projectile = projectile,
                    RemainingTime = firstTime + 2 * CollisionQueries.TimeEpsilon
                });
            var selected = GameSession.FindFirstInteraction(steps);
            Assert.That(selected.Kind == GameSession.InteractionKind.Clash, Is.EqualTo(clashWins));
        }
        [Test] public void MuzzleProjectileClashBeatsDestructibleWallAtSameTime()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            var existing = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, z), Vector3.forward);
            var wall = CreateTestWall(-0.1f, z, true).GetComponent<DestructibleWallActor>();
            Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.False); Assert.That(fired.IsAlive, Is.False);
            Assert.That(wall.IsAlive, Is.True);
        }
        [Test] public void MuzzleProjectileClashBeatsTankAtSameTime()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            var existing = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, z), Vector3.forward);
            SecondEnemy.transform.position = new Vector3(0.4f, session.Settings.planeHeight, z);
            Physics.SyncTransforms(); Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.False); Assert.That(fired.IsAlive, Is.False);
            Assert.That(SecondEnemy.Life.IsAlive, Is.True);
        }
        [Test] public void MuzzleDestructibleWallBeatsTankAtSameTime()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            Enemy.transform.position = new Vector3(0.4f, session.Settings.planeHeight, z);
            var wall = CreateTestWall(-0.1f, z, true).GetComponent<DestructibleWallActor>();
            Physics.SyncTransforms(); Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(wall.IsAlive, Is.False); Assert.That(Enemy.Life.IsAlive, Is.True);
            Assert.That(fired.IsAlive, Is.False);
        }
        [Test] public void MuzzleTankBeatsNormalWallAtSameTime()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            Enemy.transform.position = new Vector3(0.4f, session.Settings.planeHeight, z);
            CreateTestWall(-0.1f, z, false); Physics.SyncTransforms();
            Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(Enemy.Life.IsAlive, Is.False); Assert.That(fired.IsAlive, Is.False);
            Assert.That(fired.Rules.Reflections, Is.Zero);
        }
        [Test] public void MuzzlePriorityDoesNotDependOnColliderCreationOrder()
        {
            LoadStageTwo(); MoveTanksAway();
            PositionPlayerForMuzzle(100);
            var firstProjectile = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, 100), Vector3.forward);
            var firstWall = CreateTestWall(-0.1f, 100, true).GetComponent<DestructibleWallActor>();
            Assert.That(Player.TryFire(), Is.True);
            Assert.That(firstProjectile.IsAlive, Is.False); Assert.That(firstWall.IsAlive, Is.True);

            PositionPlayerForMuzzle(102);
            var secondWall = CreateTestWall(-0.1f, 102, true).GetComponent<DestructibleWallActor>();
            var secondProjectile = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, 102), Vector3.forward);
            Assert.That(Player.TryFire(), Is.True);
            Assert.That(secondProjectile.IsAlive, Is.False); Assert.That(secondWall.IsAlive, Is.True);
        }
        [Test] public void MuzzleCastUsesCurrentProjectilePoseInsteadOfStalePhysicsPose()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            Enemy.transform.position = new Vector3(-0.9f, session.Settings.planeHeight, z);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.right); Physics.SyncTransforms();
            var existing = SpawnFree(Player, new Vector3(0, session.Settings.planeHeight, z), Vector3.right);
            session.Tick(0.2f);
            Assert.That(existing.transform.position.x, Is.EqualTo(2).Within(0.001f));
            Assert.That(Enemy.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.True); Assert.That(fired.IsAlive, Is.True);
        }
        [Test] public void MuzzleCastClashesAtCurrentProjectilePoseAndReleasesAmmoOnce()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            var existing = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, z), Vector3.forward);
            Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.False); Assert.That(fired.IsAlive, Is.False);
            Assert.That(Player.Slots.Active, Is.Zero); Assert.That(Enemy.Slots.Active, Is.Zero);
            existing.Despawn(); fired.Despawn();
            Assert.That(Player.Slots.Active, Is.Zero); Assert.That(Enemy.Slots.Active, Is.Zero);
        }
        [Test] public void MuzzleCastMissesUnrelatedCurrentProjectilePose()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            var existing = SpawnFree(Enemy, new Vector3(2, session.Settings.planeHeight, z), Vector3.forward);
            Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.True); Assert.That(fired.IsAlive, Is.True);
        }
        [Test] public void MuzzleProjectileClashBeatsNormalWallAtSameTime()
        {
            LoadStageTwo(); MoveTanksAway(); const float z = 100;
            PositionPlayerForMuzzle(z);
            var existing = SpawnFree(Enemy, new Vector3(0, session.Settings.planeHeight, z), Vector3.forward);
            CreateTestWall(-0.1f, z, false);
            Assert.That(Player.TryFire(), Is.True);
            var fired = session.Projectiles[session.Projectiles.Count - 1];
            Assert.That(existing.IsAlive, Is.False); Assert.That(fired.IsAlive, Is.False);
            Assert.That(fired.Rules.Reflections, Is.Zero);
        }
        [Test] public void ExactTangentMovingProjectilesClash()
        {
            MoveTanksAway(); float diameter = session.Settings.projectileRadius * 2;
            var first = SpawnFree(Player, new Vector3(-1, session.Settings.planeHeight, 100), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(1, session.Settings.planeHeight, 100 + diameter), Vector3.left);
            session.Tick(0.2f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
        }
        [Test] public void LargeScaleNumericallySensitiveTangentClashes()
        {
            MoveTanksAway(); float diameter = session.Settings.projectileRadius * 2;
            var first = SpawnFree(Player, new Vector3(-50, session.Settings.planeHeight, 100), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(50, session.Settings.planeHeight, 100 + diameter), Vector3.left);
            session.Tick(5.1f);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
        }
        [Test] public void LargeScaleNearTangentMissOutsideToleranceDoesNotClash()
        {
            MoveTanksAway(); float separation = session.Settings.projectileRadius * 2 + 0.001f;
            var first = SpawnFree(Player, new Vector3(-50, session.Settings.planeHeight, 100), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(50, session.Settings.planeHeight, 100 + separation), Vector3.left);
            session.Tick(5.1f);
            Assert.That(first.IsAlive, Is.True); Assert.That(second.IsAlive, Is.True);
        }
        [Test] public void ClearlySeparatedMovingProjectilesDoNotClash()
        {
            MoveTanksAway();
            var first = SpawnFree(Player, new Vector3(-1, session.Settings.planeHeight, 100), Vector3.right);
            var second = SpawnFree(Enemy, new Vector3(1, session.Settings.planeHeight, 101), Vector3.left);
            session.Tick(0.2f);
            Assert.That(first.IsAlive, Is.True); Assert.That(second.IsAlive, Is.True);
        }
        [Test] public void ReflectedProjectileCanClashAndTerminalProjectileCannotDamageOrReflect()
        {
            var first = SpawnFree(Player, new Vector3(0, session.Settings.planeHeight, -5), Vector3.right);
            first.Advance(10); Assert.That(first.Rules.Reflections, Is.EqualTo(1)); Assert.That(first.IsAlive, Is.True);
            first.transform.position = new Vector3(1, session.Settings.planeHeight, -5);
            var second = SpawnFree(Enemy, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            Physics.SyncTransforms();
            first.Advance(3);
            Assert.That(first.IsAlive, Is.False); Assert.That(second.IsAlive, Is.False);
            int reflections = first.Rules.Reflections;
            Enemy.transform.position = first.transform.position; Physics.SyncTransforms();
            first.Advance(20); first.Clash(second); first.Despawn();
            Assert.That(Enemy.Life.IsAlive, Is.True); Assert.That(first.Rules.Reflections, Is.EqualTo(reflections));
            Assert.That(Player.Slots.Active, Is.Zero); Assert.That(Enemy.Slots.Active, Is.Zero);
        }
        [Test] public void AmmoIndicatorReadsSlotsAndClashRestoresAllSlots()
        {
            var indicator = Player.GetComponent<AmmoIndicator>();
            Assert.That(indicator, Is.Not.Null); Assert.That(indicator.SlotGlyphs, Is.EqualTo("●●●"));
            var first = SpawnFree(Player, new Vector3(-1, session.Settings.planeHeight, -5), Vector3.right);
            Assert.That(indicator.SlotGlyphs, Is.EqualTo("○●●"));
            var second = SpawnFree(Player, new Vector3(1, session.Settings.planeHeight, -5), Vector3.left);
            SpawnFree(Player, new Vector3(100, session.Settings.planeHeight, 100), Vector3.forward);
            Assert.That(indicator.SlotGlyphs, Is.EqualTo("○○○"));
            first.Advance(3);
            Assert.That(second.IsAlive, Is.False); Assert.That(indicator.SlotGlyphs, Is.EqualTo("○●●"));
            session.Projectiles[2].Despawn(); Assert.That(indicator.SlotGlyphs, Is.EqualTo("●●●"));
            Assert.That(Object.FindObjectsByType<AmmoIndicator>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Enemy.GetComponent<AmmoIndicator>(), Is.Null);
        }
        [UnityTest] public IEnumerator StageTransitionAndRestartKeepOneFullPlayerIndicator()
        {
            var oldIndicator = Player.GetComponent<AmmoIndicator>();
            Enemy.Hit(); bootstrap.AdvanceStageIfCleared(); yield return null;
            session = bootstrap.Session; session.AutoTick = false;
            Assert.That(oldIndicator == null, Is.True);
            Assert.That(Object.FindObjectsByType<AmmoIndicator>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Player.GetComponent<AmmoIndicator>().SlotGlyphs, Is.EqualTo("●●●"));
            bootstrap.Restart(); yield return null;
            session = bootstrap.Session; session.AutoTick = false;
            Assert.That(Object.FindObjectsByType<AmmoIndicator>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Player.GetComponent<AmmoIndicator>().SlotGlyphs, Is.EqualTo("●●●"));
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
        void AssertDirectOwnerImmunity(TankActor owner)
        {
            MoveTanksAway();
            owner.transform.position = new Vector3(0, session.Settings.planeHeight, 5);
            int durability = owner.Life.RemainingDurability;
            var projectile = SpawnFree(owner, new Vector3(-2, session.Settings.planeHeight, 5), Vector3.right);
            Assert.That(projectile.Owner, Is.SameAs(owner));
            session.Tick(0.4f);
            Assert.That(owner.Life.IsAlive, Is.True);
            Assert.That(owner.Life.RemainingDurability, Is.EqualTo(durability));
            Assert.That(projectile.IsAlive, Is.True);
            Assert.That(projectile.transform.position.x, Is.GreaterThan(0.5f));
        }
        [Test] public void PlayerProjectileDoesNotHitOwnerDirectly()
        { AssertDirectOwnerImmunity(Player); }
        [Test] public void MobileProjectileDoesNotHitOwner()
        { AssertDirectOwnerImmunity(Enemy); }
        [Test] public void SentryProjectileDoesNotHitOwner()
        { LoadStageTwo(); AssertDirectOwnerImmunity(SecondEnemy); }
        [Test] public void HeavyProjectileDoesNotHitOwner()
        { LoadStageThree(); AssertDirectOwnerImmunity(Enemy); }
        [Test] public void BurstProjectileDoesNotHitOwner()
        { LoadStageThree(); AssertDirectOwnerImmunity(SecondEnemy); }
        [Test] public void ReflectedPlayerProjectileDoesNotHitOwner()
        {
            Player.transform.position = new Vector3(-6, session.Settings.planeHeight, -3);
            Player.Turret.rotation = Quaternion.LookRotation(Vector3.back); Physics.SyncTransforms();
            Player.TryFire(); var projectile = session.Projectiles[0];
            session.Tick(1);
            Assert.That(projectile.Rules.Reflections, Is.EqualTo(1));
            Assert.That(projectile.IsAlive, Is.True);
            Assert.That(Player.Life.IsAlive, Is.True); Assert.That(session.Rules.State, Is.EqualTo(MatchState.Playing));
        }
        [Test] public void OwnerChildColliderIsIgnored()
        {
            MoveTanksAway();
            Player.transform.position = new Vector3(0, session.Settings.planeHeight, 5);
            var child = new GameObject("Owner Extra Collider"); child.layer = CollisionQueries.TankLayer;
            child.transform.SetParent(Player.transform, false); child.transform.localPosition = Vector3.right * 0.9f;
            child.AddComponent<SphereCollider>().radius = 0.35f; Physics.SyncTransforms();
            var projectile = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, 5), Vector3.right);
            session.Tick(0.4f);
            Assert.That(Player.Life.IsAlive, Is.True); Assert.That(projectile.IsAlive, Is.True);
            Assert.That(projectile.transform.position.x, Is.GreaterThan(1.25f));
        }
        [Test] public void GlobalToiIgnoresOwnerThenReflectsFromWall()
        {
            MoveTanksAway();
            Player.transform.position = new Vector3(0, session.Settings.planeHeight, 5);
            CreateTestWall(2, 5, false);
            var projectile = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, 5), Vector3.right);
            session.Tick(0.5f);
            Assert.That(Player.Life.IsAlive, Is.True); Assert.That(projectile.IsAlive, Is.True);
            Assert.That(projectile.Rules.Reflections, Is.EqualTo(1)); Assert.That(projectile.Direction.x, Is.LessThan(0));
        }
        [Test] public void GlobalToiIgnoresOwnerThenHitsOtherTank()
        {
            MoveTanksAway();
            Player.transform.position = new Vector3(0, session.Settings.planeHeight, 5);
            Enemy.transform.position = new Vector3(2, session.Settings.planeHeight, 5); Physics.SyncTransforms();
            var projectile = SpawnFree(Player, new Vector3(-2, session.Settings.planeHeight, 5), Vector3.right);
            session.Tick(0.5f);
            Assert.That(Player.Life.IsAlive, Is.True); Assert.That(Enemy.Life.IsAlive, Is.False);
            Assert.That(projectile.IsAlive, Is.False);
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
        sealed class AlwaysFireCommand : ITankController
        {
            public TankCommand ReadCommand(in TankObservation observation) =>
                new TankCommand(Vector3.zero, observation.Position + Vector3.forward, true);
        }
        sealed class TargetFireCommand : ITankController
        {
            public TankCommand ReadCommand(in TankObservation observation) =>
                new TankCommand(Vector3.zero, observation.Target, true);
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
        [Test] public void BlockedBotKeepsRepositioningInsteadOfRecoveringInPlace()
        {
            var botSettings = ScriptableObject.CreateInstance<BotSettings>();
            botSettings.reactionTimeSeconds = 0.1f; botSettings.moveDurationSeconds = 0.5f;
            var controller = new BotTankController(botSettings, new[] { new Vector2(0, 3), new Vector2(3, 0) });
            int movingCommands = 0;
            var observation = new TankObservation(Vector3.zero, Vector3.forward * 6, Vector3.forward, 0.1f, false);
            for (int i = 0; i < 30; i++)
                if (controller.ReadCommand(in observation).Move.sqrMagnitude > 0) movingCommands++;
            Object.Destroy(botSettings);
            Assert.That(movingCommands, Is.GreaterThanOrEqualTo(25));
            Assert.That(controller.State, Is.EqualTo(BotTankController.BotState.Move));
        }
        [Test] public void RuntimeBotRepositionsWhenStageWallBlocksLineOfSight()
        {
            var enemy = bootstrap.CurrentStage.enemies[0];
            Enemy.Controller = new BotTankController(enemy.botSettings, enemy.patrolPoints);
            var start = Enemy.transform.position;
            for (int i = 0; i < 12; i++) session.Tick(0.1f);
            Assert.That(Vector3.Distance(start, Enemy.transform.position), Is.GreaterThan(0.1f));
            Assert.That(session.Projectiles, Is.Empty);
        }
        [Test] public void RuntimeBotRepositionsAfterFiring()
        {
            PrepareRuntimeBotForClearShot();
            Assert.That(TickUntilEnemyFires(), Is.True);
            var start = Enemy.transform.position;
            session.Projectiles[0].Despawn();
            for (int i = 0; i < 10; i++) session.Tick(0.1f);
            Assert.That(Vector3.Distance(start, Enemy.transform.position), Is.GreaterThan(0.1f));
        }
        [Test] public void RuntimeBotKeepsMovingDuringFireCooldown()
        {
            PrepareRuntimeBotForClearShot();
            Assert.That(TickUntilEnemyFires(), Is.True);
            var start = Enemy.transform.position;
            session.Projectiles[0].Despawn();
            for (int i = 0; i < 8; i++) session.Tick(0.1f);
            Assert.That(Vector3.Distance(start, Enemy.transform.position), Is.GreaterThan(0.1f));
            Assert.That(session.Projectiles, Is.Empty);
        }
        [Test] public void SentryStaysStillAimsAndFiresWithLineOfSight()
        {
            LoadStageTwo();
            var sentryDefinition = bootstrap.CurrentStage.enemies[1];
            SecondEnemy.Controller = new SentryTankController(sentryDefinition.botSettings);
            var start = SecondEnemy.transform.position;
            for (int i = 0; i < 20 && session.Projectiles.Count == 0; i++) session.Tick(0.1f);
            Assert.That(SecondEnemy.transform.position, Is.EqualTo(start));
            Assert.That(session.Projectiles.Count, Is.EqualTo(1));
            Assert.That(Vector3.Angle(SecondEnemy.Turret.forward, Player.transform.position - SecondEnemy.transform.position), Is.LessThan(2));
        }
        [Test] public void SentryDoesNotFireWithoutLineOfSight()
        {
            LoadStageTwo();
            Player.transform.position = new Vector3(-6, session.Settings.planeHeight, -4);
            SecondEnemy.transform.position = new Vector3(6, session.Settings.planeHeight, 4);
            var sentryDefinition = bootstrap.CurrentStage.enemies[1];
            SecondEnemy.Controller = new SentryTankController(sentryDefinition.botSettings); Physics.SyncTransforms();
            var start = SecondEnemy.transform.position;
            for (int i = 0; i < 30; i++) session.Tick(0.1f);
            Assert.That(SecondEnemy.transform.position, Is.EqualTo(start)); Assert.That(session.Projectiles, Is.Empty);
        }
        [Test] public void SentryHonorsCooldownAndThreeProjectileCapacity()
        {
            LoadStageTwo();
            var sentryDefinition = bootstrap.CurrentStage.enemies[1];
            SecondEnemy.Controller = new SentryTankController(sentryDefinition.botSettings);
            int seen = 0;
            for (int i = 0; i < 100; i++)
            {
                SecondEnemy.Tick(0.1f); Physics.SyncTransforms();
                if (session.Projectiles.Count > seen)
                {
                    seen = session.Projectiles.Count;
                    session.Projectiles[seen - 1].transform.position = new Vector3(100 + seen * 2, session.Settings.planeHeight, 100);
                    Physics.SyncTransforms();
                }
            }
            Assert.That(session.Projectiles.Count, Is.EqualTo(3)); Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(3));
        }
        [Test] public void SentryStopsAfterTerminalMatchState()
        {
            LoadStageTwo();
            var sentryDefinition = bootstrap.CurrentStage.enemies[1];
            SecondEnemy.Controller = new SentryTankController(sentryDefinition.botSettings);
            var position = SecondEnemy.transform.position; var turret = SecondEnemy.Turret.rotation;
            Player.Hit(); session.Tick(2);
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Defeat));
            Assert.That(SecondEnemy.transform.position, Is.EqualTo(position)); Assert.That(SecondEnemy.Turret.rotation, Is.EqualTo(turret));
            Assert.That(session.Projectiles, Is.Empty);
        }
        [Test] public void StageThreeIsValidAndContainsOneHeavyAndOneBurstBehindInitialCover()
        {
            Assert.That(StageValidator.Validate(bootstrap.stages, bootstrap.settings), Is.Empty);
            LoadStageThree();
            Assert.That(bootstrap.CurrentStage.enemies.Length, Is.EqualTo(2));
            Assert.That(bootstrap.CurrentStage.enemies[0].archetype, Is.EqualTo(EnemyArchetype.Heavy));
            Assert.That(bootstrap.CurrentStage.enemies[1].archetype, Is.EqualTo(EnemyArchetype.Burst));
            foreach (var tank in new[] { Enemy, SecondEnemy })
            {
                Vector3 direction = tank.transform.position - Player.transform.position;
                Assert.That(Physics.Raycast(Player.transform.position, direction.normalized, direction.magnitude,
                    1 << CollisionQueries.WallLayer), Is.True);
            }
        }
        [Test] public void HeavySurvivesFirstHitWithoutEnemyCountChangeAndDiesOnSecond()
        {
            LoadStageThree(); int before = session.Rules.EnemiesRemaining;
            Enemy.Hit();
            Assert.That(Enemy.Life.IsAlive, Is.True); Assert.That(Enemy.Life.RemainingDurability, Is.EqualTo(1));
            Assert.That(session.Rules.EnemiesRemaining, Is.EqualTo(before));
            Enemy.Hit();
            Assert.That(Enemy.Life.IsAlive, Is.False); Assert.That(session.Rules.EnemiesRemaining, Is.EqualTo(before - 1));
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Playing));
        }
        [Test] public void HeavyMovementProjectileSpeedAndColorDifferFromMobile()
        {
            LoadStageThree();
            Assert.That(Enemy.MoveSpeed, Is.LessThan(bootstrap.stages[0].enemies[0].botSettings.moveSpeed));
            Assert.That(Enemy.ProjectileSpeed, Is.LessThan(session.Settings.projectileSpeed));
            Assert.That(Enemy.GetComponentsInChildren<Renderer>()[0].sharedMaterial, Is.SameAs(bootstrap.presentation.heavy));
            MoveTanksAway(); Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.forward); Physics.SyncTransforms();
            Assert.That(Enemy.TryFire(), Is.True);
            Assert.That(session.Projectiles[0].Speed, Is.EqualTo(session.Settings.heavyProjectileSpeed));
        }
        [Test] public void BurstFiresThreeAtConfiguredSpacingReloadAndNeverExceedsSlots()
        {
            LoadStageThree(); MoveTanksAway();
            var controller = new BurstTankController(new AlwaysFireCommand(), session.Settings.burstShotCount,
                session.Settings.burstSpacingSeconds, session.Settings.burstReloadSeconds);
            SecondEnemy.Controller = controller; SecondEnemy.Turret.rotation = Quaternion.LookRotation(Vector3.forward);
            session.Tick(0.01f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
            session.Tick(session.Settings.burstSpacingSeconds - 0.01f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
            session.Tick(0.01f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(2));
            for (int i = 0; i < 9; i++) session.Tick(0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(3));
            session.Tick(0.01f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(3));
            foreach (var projectile in session.Projectiles) projectile.Despawn();
            Assert.That(SecondEnemy.Slots.Active, Is.Zero);
            session.Tick(session.Settings.burstReloadSeconds - 0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.Zero);
            session.Tick(0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
        }
        [Test] public void BurstCancelsPendingShotsWhenLosIsLostAndStartsFreshAfterReload()
        {
            LoadStageThree(); MoveTanksAway();
            Player.transform.position = new Vector3(-4, session.Settings.planeHeight, 5);
            SecondEnemy.transform.position = new Vector3(4, session.Settings.planeHeight, 5);
            SecondEnemy.Turret.rotation = Quaternion.LookRotation(Vector3.left);
            var controller = new BurstTankController(new TargetFireCommand(), session.Settings.burstShotCount,
                session.Settings.burstSpacingSeconds, session.Settings.burstReloadSeconds);
            SecondEnemy.Controller = controller; Physics.SyncTransforms();

            session.Tick(0.01f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
            Assert.That(controller.RemainingShots, Is.EqualTo(2));
            var firstProjectile = session.Projectiles[0];
            firstProjectile.transform.position = new Vector3(100, session.Settings.planeHeight, 100);
            var blocker = CreateTestWall(-0.5f, 5, false);

            session.Tick(session.Settings.burstSpacingSeconds);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
            Assert.That(controller.RemainingShots, Is.Zero);
            Assert.That(controller.ReloadRemaining, Is.EqualTo(session.Settings.burstReloadSeconds).Within(0.0001f));

            firstProjectile.Despawn(); blocker.SetActive(false); Physics.SyncTransforms();
            session.Tick(session.Settings.burstReloadSeconds - 0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.Zero);
            session.Tick(0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(1));
            Assert.That(controller.RemainingShots, Is.EqualTo(2));
            for (int i = 0; i < 18; i++) session.Tick(0.02f);
            Assert.That(SecondEnemy.Slots.Active, Is.EqualTo(3));
            Assert.That(controller.RemainingShots, Is.Zero);
        }
        [Test] public void BurstStopsOnOwnDeathAndTerminalState()
        {
            LoadStageThree(); MoveTanksAway();
            var controller = new BurstTankController(new AlwaysFireCommand(), 3,
                session.Settings.burstSpacingSeconds, session.Settings.burstReloadSeconds);
            SecondEnemy.Controller = controller; SecondEnemy.Tick(0.01f);
            Assert.That(controller.RemainingShots, Is.EqualTo(2));
            SecondEnemy.Hit();
            Assert.That(controller.RemainingShots, Is.Zero);

            bootstrap.Restart(); bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            Player.transform.position = new Vector3(-4, session.Settings.planeHeight, 5);
            Enemy.transform.position = new Vector3(4, session.Settings.planeHeight, 5);
            Enemy.Turret.rotation = Quaternion.LookRotation(Vector3.left); Physics.SyncTransforms();
            var terminalController = new BurstTankController(new AlwaysFireCommand(), 3,
                session.Settings.burstSpacingSeconds, session.Settings.burstReloadSeconds);
            Enemy.Controller = terminalController;
            Enemy.Tick(0.01f); Assert.That(terminalController.RemainingShots, Is.EqualTo(2));
            Player.Hit();
            Assert.That(terminalController.RemainingShots, Is.Zero);
            var observation = new TankObservation(Enemy.transform.position, Player.transform.position,
                Enemy.Turret.forward, 1, true, 3);
            Assert.That(terminalController.ReadCommand(in observation).Fire, Is.False);
        }
        [UnityTest] public IEnumerator BurstAndItsProjectilesAreCleanedOnRestartTransition()
        {
            LoadStageThree(); MoveTanksAway();
            SecondEnemy.Controller = new BurstTankController(new AlwaysFireCommand(), 3,
                session.Settings.burstSpacingSeconds, session.Settings.burstReloadSeconds);
            SecondEnemy.Tick(0.01f); var oldSession = session; var oldBurst = SecondEnemy; var oldProjectile = session.Projectiles[0];
            bootstrap.Restart(); yield return null;
            Assert.That(oldSession == null, Is.True); Assert.That(oldBurst == null, Is.True); Assert.That(oldProjectile == null, Is.True);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(1));
        }
        [Test] public void StageIntroShowsTitleThenGoAndLocksAllGameplay()
        {
            bootstrap.Restart(); session = bootstrap.Session; session.AutoTick = false;
            Player.Controller = new FixedCommand(Vector3.right); var start = Player.transform.position;
            Assert.That(bootstrap.OverlayText, Is.EqualTo("STAGE 1"));
            Assert.That(Player.TryFire(), Is.False); Assert.That(session.TryPlaceMine(Player), Is.False);
            session.Tick(1); Assert.That(Player.transform.position, Is.EqualTo(start));
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds);
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.Go)); Assert.That(bootstrap.OverlayText, Is.EqualTo("GO!"));
            session.Tick(1); Assert.That(Player.transform.position, Is.EqualTo(start));
            bootstrap.TickPresentation(bootstrap.settings.stageGoSeconds);
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.Playing));
            session.Tick(0.1f); Assert.That(Player.transform.position, Is.Not.EqualTo(start));
        }
        [Test] public void StageClearLocksGameplayThenTransitionsAfterDelay()
        {
            Assert.That(session.TryPlaceMine(Player), Is.True); var pendingMine = session.Mines[0];
            Player.TryFire(); var projectile = session.Projectiles[0];
            projectile.transform.position = new Vector3(100, session.Settings.planeHeight, 100); Physics.SyncTransforms();
            var projectileStart = projectile.transform.position;
            Enemy.Hit(); bootstrap.TickPresentation(0);
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.StageClear));
            Assert.That(bootstrap.OverlayText, Is.EqualTo("STAGE CLEAR"));
            Assert.That(session.Mines, Is.Empty); Assert.That(pendingMine.IsArmed, Is.False);
            Player.Controller = new FixedCommand(Vector3.right); var playerStart = Player.transform.position;
            session.Tick(1);
            Assert.That(Player.transform.position, Is.EqualTo(playerStart)); Assert.That(projectile.transform.position, Is.EqualTo(projectileStart));
            bootstrap.TickPresentation(bootstrap.settings.stageClearSeconds - 0.01f);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(1));
            bootstrap.TickPresentation(0.01f);
            Assert.That(bootstrap.CurrentStageId, Is.EqualTo(2)); Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.StageTitle));
        }
        [Test] public void DestructibleWallBlocksThenProjectileDestroysWithoutReflectionAndClearsPathAndLos()
        {
            LoadStageTwo();
            var wall = session.DestructibleWalls[0];
            Player.transform.position = new Vector3(5.2f, session.Settings.planeHeight, 0); Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Vector3(5.2f, session.Settings.planeHeight, 0), Vector3.left, 1.5f, 1 << CollisionQueries.WallLayer), Is.True);
            Player.Controller = new FixedCommand(Vector3.left); Player.Tick(0.5f);
            Assert.That(Player.transform.position.x, Is.GreaterThan(4.5f));
            Player.Controller = null; Player.Turret.rotation = Quaternion.LookRotation(Vector3.left); Physics.SyncTransforms();
            Assert.That(Player.TryFire(), Is.True); var projectile = session.Projectiles[0];
            projectile.Advance(2);
            Assert.That(wall.IsAlive, Is.False); Assert.That(projectile.IsAlive, Is.False);
            Assert.That(projectile.Rules.Reflections, Is.Zero); Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Vector3(5.2f, session.Settings.planeHeight, 0), Vector3.left, 1.5f, 1 << CollisionQueries.WallLayer), Is.False);
            Player.Controller = new FixedCommand(Vector3.left); Player.Tick(0.3f);
            Assert.That(Player.transform.position.x, Is.LessThan(4.5f));
            Player.transform.position = new Vector3(5.2f, session.Settings.planeHeight, 0); Physics.SyncTransforms();
            var pass = SpawnFree(Player, new Vector3(5.2f, session.Settings.planeHeight, 0), Vector3.left); pass.Advance(1.5f);
            Assert.That(pass.IsAlive, Is.True); Assert.That(pass.transform.position.x, Is.LessThan(4.5f));
            wall.Hit(); Assert.That(wall.IsAlive, Is.False);
        }
        [Test] public void MineStartsAtTwoAllowsTwoAndRejectsThird()
        {
            Assert.That(session.MinesRemaining, Is.EqualTo(2)); Assert.That(session.Mines, Is.Empty);
            Assert.That(session.TryPlaceMine(Player), Is.True);
            Player.transform.position += Vector3.right * 2;
            Assert.That(session.TryPlaceMine(Player), Is.True);
            Assert.That(session.TryPlaceMine(Player), Is.False);
            Assert.That(session.MinesRemaining, Is.Zero); Assert.That(session.Mines.Count, Is.EqualTo(2));
            Assert.That(bootstrap.MineHudText, Is.EqualTo("MINES 0"));
        }
        [Test] public void MineWaitsOneSecondAndTankOrProjectileContactDoesNotTrigger()
        {
            Assert.That(session.TryPlaceMine(Player), Is.True); var mine = session.Mines[0];
            var projectile = SpawnFree(Player, mine.transform.position + Vector3.left * 2, Vector3.right);
            projectile.Advance(4);
            Assert.That(mine.IsArmed, Is.True); Assert.That(projectile.IsAlive, Is.True);
            Enemy.transform.position = mine.transform.position; Physics.SyncTransforms();
            session.Tick(0.99f);
            Assert.That(mine.IsArmed, Is.True); Assert.That(Player.Life.IsAlive, Is.True); Assert.That(Enemy.Life.IsAlive, Is.True);
            session.Tick(0.01f);
            Assert.That(mine.IsArmed, Is.False); Assert.That(Player.Life.IsAlive, Is.False); Assert.That(Enemy.Life.IsAlive, Is.False);
        }
        [Test] public void MineDamagesStandardEnemyThroughNormalEnemyAccounting()
        {
            Assert.That(session.TryPlaceMine(Player), Is.True); var position = session.Mines[0].transform.position;
            Player.transform.position = new Vector3(-8, session.Settings.planeHeight, 5);
            Enemy.transform.position = new Vector3(position.x, session.Settings.planeHeight, position.z); Physics.SyncTransforms();
            session.Tick(1);
            Assert.That(Enemy.Life.IsAlive, Is.False); Assert.That(session.Rules.EnemiesRemaining, Is.Zero);
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Victory));
        }
        [Test] public void OneMineDamagesPlayerAndHeavyExactlyOnce()
        {
            LoadStageThree();
            Assert.That(session.TryPlaceMine(Player), Is.True); var position = session.Mines[0].transform.position;
            Enemy.transform.position = new Vector3(position.x, session.Settings.planeHeight, position.z);
            SecondEnemy.transform.position = new Vector3(8, session.Settings.planeHeight, 5); Physics.SyncTransforms();
            session.Tick(1);
            Assert.That(Player.Life.IsAlive, Is.False);
            Assert.That(Enemy.Life.IsAlive, Is.True); Assert.That(Enemy.Life.RemainingDurability, Is.EqualTo(1));
            Assert.That(session.Rules.State, Is.EqualTo(MatchState.Defeat));
        }
        [Test] public void MineExplosionCreatesOneVfxAndVfxCompletesItsOwnLifecycle()
        {
            MoveTanksAway();
            Assert.That(session.TryPlaceMine(Player), Is.True); var mine = session.Mines[0];
            mine.Explode(); mine.Explode();
            var effects = Object.FindObjectsByType<MineExplosionVfx>(FindObjectsSortMode.None);
            Assert.That(effects, Has.Length.EqualTo(1));
            var effect = effects[0]; effect.enabled = false;
            Assert.That(effect.transform.parent, Is.EqualTo(session.transform));
            Assert.That(effect.TargetDiameter, Is.EqualTo(session.Settings.mineBlastRadius * 2).Within(0.0001f));
            Assert.That(effect.Lifetime, Is.EqualTo(0.30f).Within(0.0001f));
            Assert.That(mine.gameObject.activeSelf, Is.False); Assert.That(effect.IsAlive, Is.True);
            effect.Tick(0.29f);
            Assert.That(effect.IsAlive, Is.True); Assert.That(effect.transform.localScale.x, Is.LessThan(effect.TargetDiameter));
            effect.Tick(0.02f);
            Assert.That(effect.IsAlive, Is.False); Assert.That(effect.gameObject.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator StageTransitionAndRestartCleanLiveMineExplosionVfx()
        {
            MoveTanksAway();
            Assert.That(session.TryPlaceMine(Player), Is.True); var mine = session.Mines[0];
            Player.transform.position += Vector3.right * 3; mine.Explode();
            var transitionEffect = Object.FindFirstObjectByType<MineExplosionVfx>(); transitionEffect.enabled = false;
            Enemy.Hit(); Assert.That(bootstrap.AdvanceStageIfCleared(), Is.True); yield return null;
            Assert.That(transitionEffect == null, Is.True);

            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false;
            foreach (var tank in session.Tanks) tank.Controller = null;
            MoveTanksAway(); Assert.That(session.TryPlaceMine(Player), Is.True); mine = session.Mines[0];
            Player.transform.position += Vector3.right * 3; mine.Explode();
            var restartEffect = Object.FindFirstObjectByType<MineExplosionVfx>(); restartEffect.enabled = false;
            bootstrap.Restart(); yield return null;
            Assert.That(restartEffect == null, Is.True);
            Assert.That(Object.FindObjectsByType<MineExplosionVfx>(FindObjectsSortMode.None), Is.Empty);
        }
        [Test] public void MineIsBlockedDuringIntroClearAndTerminalPresentation()
        {
            bootstrap.Restart(); session = bootstrap.Session; session.AutoTick = false;
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.StageTitle));
            Assert.That(session.TryPlaceMine(Player), Is.False);
            bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            Enemy.Hit(); bootstrap.TickPresentation(0);
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.StageClear));
            Assert.That(session.TryPlaceMine(Player), Is.False);
            bootstrap.Restart(); bootstrap.TickPresentation(bootstrap.settings.stageTitleSeconds + bootstrap.settings.stageGoSeconds);
            session = bootstrap.Session; session.AutoTick = false; Player.Hit(); bootstrap.TickPresentation(0);
            Assert.That(bootstrap.Phase, Is.EqualTo(StagePresentationPhase.Defeat));
            Assert.That(session.TryPlaceMine(Player), Is.False);
        }
        [Test] public void UndefinedEnemyBehaviorDoesNotFallBackToMobileController()
        {
            var invalid = bootstrap.CurrentStage.enemies[0]; invalid.behavior = (EnemyBehavior)999;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SliceBootstrap.CreateEnemyController(invalid, bootstrap.settings));
        }
        [Test] public void UndefinedEnemyArchetypeDoesNotFallBackToStandard()
        {
            var invalid = bootstrap.CurrentStage.enemies[0]; invalid.archetype = (EnemyArchetype)999;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SliceBootstrap.CreateEnemyController(invalid, bootstrap.settings));
        }
        [UnityTest] public IEnumerator StageTransitionAndRestartCleanGimmicksWithoutDuplicates()
        {
            LoadStageTwo(); Assert.That(session.TryPlaceMine(Player), Is.True);
            var wall = session.DestructibleWalls[0]; var mine = session.Mines[0];
            bootstrap.Restart(); yield return null;
            session = bootstrap.Session; session.AutoTick = false;
            Assert.That(wall == null, Is.True); Assert.That(mine == null, Is.True);
            Assert.That(session.DestructibleWalls, Is.Empty); Assert.That(session.Mines, Is.Empty);
            Assert.That(session.MinesRemaining, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<DestructibleWallActor>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<MineActor>(FindObjectsSortMode.None), Is.Empty);
        }
        [UnityTest] public IEnumerator StageTransitionCleansMineAndResetsTwoUses()
        {
            Assert.That(session.TryPlaceMine(Player), Is.True); var oldMine = session.Mines[0];
            Enemy.Hit(); Assert.That(bootstrap.AdvanceStageIfCleared(), Is.True); yield return null;
            session = bootstrap.Session; session.AutoTick = false;
            Assert.That(oldMine == null, Is.True); Assert.That(session.Mines, Is.Empty);
            Assert.That(session.MinesRemaining, Is.EqualTo(2)); Assert.That(bootstrap.CurrentStageId, Is.EqualTo(2));
        }
        void PrepareRuntimeBotForClearShot()
        {
            Player.transform.position = new Vector3(-6, session.Settings.planeHeight, -4);
            Enemy.transform.position = new Vector3(6, session.Settings.planeHeight, -4);
            var enemy = bootstrap.CurrentStage.enemies[0];
            Enemy.Controller = new BotTankController(enemy.botSettings, enemy.patrolPoints);
            Physics.SyncTransforms();
        }
        bool TickUntilEnemyFires()
        {
            for (int i = 0; i < 50; i++)
            {
                session.Tick(0.1f);
                if (session.Projectiles.Count > 0) return true;
            }
            return false;
        }
        int RuntimeRootCount()
        {
            int count = 0;
            foreach (Transform child in bootstrap.transform)
                if (child.name.EndsWith(" Runtime", System.StringComparison.Ordinal)) count++;
            return count;
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
