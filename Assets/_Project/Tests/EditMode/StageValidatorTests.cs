using NUnit.Framework;
using TankGame.Editor;
using TankGame.Gameplay;
using UnityEngine;
namespace TankGame.Tests.EditMode
{
    public sealed class StageValidatorTests
    {
        StageDefinition stage;
        BotSettings bot;
        [SetUp] public void Setup() { stage = ScriptableObject.CreateInstance<StageDefinition>(); bot = ScriptableObject.CreateInstance<BotSettings>(); stage.botSettings = bot; }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(stage); Object.DestroyImmediate(bot); }
        [Test] public void PrototypeStageIsValid() { Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Is.Empty); }
        [Test] public void WallSpawnIsRejected() { stage.playerSpawn = Vector2.zero; Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Is.Not.Empty); }
        [Test] public void OutsideSpawnIsRejected() { stage.playerSpawn = new Vector2(20, 0); Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Is.Not.Empty); }
        [Test] public void DuplicateIdIsRejected() { Assert.That(StageValidator.Validate(new[] { stage, stage }, 0.5f), Does.Contain("Duplicate Stage ID.")); }
        [Test] public void StageIdsMustMatchListOrder()
        { stage.stageId = 2; Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Does.Contain("Stage IDs must be contiguous and match list order.")); }
        [Test] public void BuildValidationRejectsMainStageOrderWithoutSorting()
        {
            var second = ScriptableObject.CreateInstance<StageDefinition>(); second.stageId = 2; second.botSettings = bot;
            var settings = ScriptableObject.CreateInstance<GameplaySettings>();
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => SliceProjectBuilder.ValidateMainStages(new[] { second, stage }, settings));
            }
            finally
            {
                Object.DestroyImmediate(second); Object.DestroyImmediate(settings);
            }
        }
        [Test] public void RequiredReferenceIsRejected() { stage.botSettings = null; Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Does.Contain("Missing required Stage reference.")); }
        [Test] public void IsolatedEnemyIsRejected()
        {
            stage.walls = new[] { new StageWall(Vector2.zero, new Vector2(2, 14)) };
            Assert.That(StageValidator.Validate(new[] { stage }, 0.5f), Does.Contain("Spawn is isolated or unreachable."));
        }
    }
}
