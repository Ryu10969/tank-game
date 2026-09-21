using NUnit.Framework;
using TankGame.Core;
namespace TankGame.Tests.EditMode
{
    public sealed class CoreRulesTests
    {
        [Test] public void FourthShotIsRejectedUntilOneSlotReturns()
        {
            var slots = new ShotSlots(3);
            Assert.That(slots.TryAcquire(), Is.True); Assert.That(slots.TryAcquire(), Is.True); Assert.That(slots.TryAcquire(), Is.True);
            Assert.That(slots.TryAcquire(), Is.False); Assert.That(slots.Available, Is.Zero);
            slots.Release(); Assert.That(slots.Available, Is.EqualTo(1)); Assert.That(slots.TryAcquire(), Is.True);
        }
        [Test] public void ExactlyOneReflectionIsAllowed()
        {
            var rules = new ProjectileRules(1);
            Assert.That(rules.HitWall(), Is.True);
            Assert.That(rules.Reflections, Is.EqualTo(1)); Assert.That(rules.Alive, Is.True);
            Assert.That(rules.HitWall(), Is.False); Assert.That(rules.Alive, Is.False);
        }
        [Test] public void ZeroReflectionsDespawnOnFirstWallHit()
        {
            var rules = new ProjectileRules(0);
            Assert.That(rules.HitWall(), Is.False);
            Assert.That(rules.Reflections, Is.Zero); Assert.That(rules.Alive, Is.False);
        }
        [TestCase(-1, 0, 1, 0, 1, 0)]
        [TestCase(0, -1, 0, 1, 0, 1)]
        [TestCase(-0.7071068f, -0.7071068f, 1, 0, 0.7071068f, -0.7071068f)]
        public void ReflectionUsesSurfaceNormal(float dx, float dz, float nx, float nz, float expectedX, float expectedZ)
        {
            ProjectileRules.Reflect(dx, dz, nx, nz, out float x, out float z);
            Assert.That(x, Is.EqualTo(expectedX).Within(0.00001f)); Assert.That(z, Is.EqualTo(expectedZ).Within(0.00001f));
        }
        [TestCase(8, 3, 5)] [TestCase(1, 2, 0)]
        public void RemainingDistanceNeverBecomesNegative(float distance, float traveled, float expected)
        { Assert.That(ProjectileRules.RemainingDistance(distance, traveled), Is.EqualTo(expected)); }
        [Test] public void OneHitDestroysTankAndRepeatedHitIsIgnored()
        { var life = new TankLife(); Assert.That(life.Hit(), Is.True); Assert.That(life.IsAlive, Is.False); Assert.That(life.Hit(), Is.False); }
        [Test] public void OnlyLastEnemyCausesVictory()
        {
            var match = new MatchRules(2); match.TankDestroyed(false); Assert.That(match.State, Is.EqualTo(MatchState.Playing));
            match.TankDestroyed(false); Assert.That(match.State, Is.EqualTo(MatchState.Victory));
            match.TankDestroyed(true); Assert.That(match.State, Is.EqualTo(MatchState.Victory));
        }
        [Test] public void PlayerDeathCausesTerminalDefeat()
        { var match = new MatchRules(1); match.TankDestroyed(true); match.TankDestroyed(false); Assert.That(match.State, Is.EqualTo(MatchState.Defeat)); }
        [Test] public void StageProgressionAdvancesOnceAndStopsAtLastStage()
        {
            var progression = new StageProgression(2);
            Assert.That(progression.CurrentIndex, Is.Zero);
            Assert.That(progression.TryAdvance(), Is.True);
            Assert.That(progression.CurrentIndex, Is.EqualTo(1));
            Assert.That(progression.TryAdvance(), Is.False);
            progression.Restart(); Assert.That(progression.CurrentIndex, Is.Zero);
        }
    }
}
