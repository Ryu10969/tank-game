using System;
using System.IO;
using TankGame.Gameplay;
using TankGame.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TankGame.Editor
{
    public static class SliceProjectBuilder
    {
        const string Data = "Assets/_Project/Data";
        const string Main = "Assets/Scenes/Main.unity";
        [MenuItem("Tank Game/Prepare Core Slice")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Data);
            var settings = Asset<GameplaySettings>("GameplaySettings");
            var bot = Asset<BotSettings>("BotSettings");
            settings.maximumReflections = 1;
            settings.defaultDurability = 1;
            settings.heavyDurability = 2;
            settings.heavyMoveSpeed = 1.2f;
            settings.heavyProjectileSpeed = 7;
            settings.burstShotCount = 3;
            settings.burstSpacingSeconds = 0.18f;
            settings.burstReloadSeconds = 2;
            settings.mineCapacity = 2;
            settings.mineFuseSeconds = 1;
            settings.mineBlastRadius = 1.05f;
            settings.stageTitleSeconds = 0.5f;
            settings.stageGoSeconds = 0.5f;
            settings.stageClearSeconds = 0.75f;
            bot.reactionTimeSeconds = 0.35f;
            bot.fireCooldownSeconds = 1.5f;
            bot.moveSpeed = 1.8f;
            bot.moveDurationSeconds = 2.5f;
            bot.minimumRepositionSeconds = 0.6f;
            var stage1 = Asset<StageDefinition>("Stage1");
            ConfigureStage1(stage1, bot);
            var stage2 = Asset<StageDefinition>("Stage2");
            ConfigureStage2(stage2, bot);
            var stage3 = Asset<StageDefinition>("Stage3");
            ConfigureStage3(stage3, bot);
            var art = Asset<PrototypePresentation>("PrototypePresentation");
            art.floor = Material("Floor", new Color(0.57f, 0.39f, 0.22f));
            art.wall = Material("Wall", new Color(0.76f, 0.56f, 0.32f));
            art.destructibleWall = Material("DestructibleWall", new Color(0.86f, 0.68f, 0.38f));
            art.mine = Material("Mine", new Color(0.85f, 0.72f, 0.12f));
            art.player = Material("Player", new Color(0.22f, 0.65f, 0.64f));
            art.enemy = Material("Enemy", new Color(0.83f, 0.29f, 0.16f));
            art.heavy = Material("Heavy", new Color(0.24f, 0.58f, 0.28f));
            art.burst = Material("Burst", new Color(0.18f, 0.38f, 0.82f));
            art.trim = Material("Trim", new Color(0.22f, 0.16f, 0.12f));
            art.projectile = Material("Projectile", new Color(1, 0.9f, 0.42f));
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(bot);
            EditorUtility.SetDirty(stage1); EditorUtility.SetDirty(stage2); EditorUtility.SetDirty(stage3); EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(Main);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Core Vertical Slice").AddComponent<SliceBootstrap>();
            bootstrap.stages = new[] { stage1, stage2, stage3 };
            bootstrap.settings = settings; bootstrap.presentation = art; bootstrap.gameCamera = Camera.main;
            if (bootstrap.gameCamera == null) throw new InvalidOperationException("Main camera is required.");
            bootstrap.gameCamera.orthographic = true; bootstrap.gameCamera.orthographicSize = art.cameraSize;
            bootstrap.gameCamera.transform.SetPositionAndRotation(art.cameraPosition, Quaternion.Euler(art.cameraAngles));
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Main, true) };
            Validate();
            Debug.Log("Core slice prepared: Main only; Stage 1-3 validation PASS.");
        }
        static void ConfigureStage1(StageDefinition stage, BotSettings bot)
        {
            stage.stageId = 1;
            stage.size = new Vector2(20, 14);
            stage.playerSpawn = new Vector2(-6, -3);
            stage.enemies = new[] { new StageEnemy(new Vector2(6, 3), new[] { new Vector2(6, -3), new Vector2(6, 3) }, bot, EnemyBehavior.Mobile) };
            stage.walls = new[] { new StageWall(Vector2.zero, new Vector2(2, 4)) };
            stage.destructibleWalls = Array.Empty<StageWall>();
            stage.themeId = "wood-prototype";
            stage.boundaryThickness = 1;
        }
        static void ConfigureStage2(StageDefinition stage, BotSettings bot)
        {
            stage.stageId = 2;
            stage.size = new Vector2(20, 14);
            stage.playerSpawn = new Vector2(6, -4);
            stage.enemies = new[]
            {
                new StageEnemy(new Vector2(-6, 4), new[] { new Vector2(-6, -4), new Vector2(-4, 0), new Vector2(-6, 4) }, bot, EnemyBehavior.Mobile),
                new StageEnemy(new Vector2(6, 4), Array.Empty<Vector2>(), bot, EnemyBehavior.Sentry)
            };
            stage.walls = new[] { new StageWall(Vector2.zero, new Vector2(6, 2)) };
            stage.destructibleWalls = new[] { new StageWall(new Vector2(4, 0), new Vector2(1, 2)) };
            stage.themeId = "wood-prototype";
            stage.boundaryThickness = 1;
        }
        static void ConfigureStage3(StageDefinition stage, BotSettings bot)
        {
            stage.stageId = 3;
            stage.size = new Vector2(20, 14);
            stage.playerSpawn = new Vector2(-7, 0);
            stage.enemies = new[]
            {
                new StageEnemy(new Vector2(6, 3), new[] { new Vector2(6, 0), new Vector2(5, 4), new Vector2(7, 3) },
                    bot, EnemyBehavior.Mobile, EnemyArchetype.Heavy),
                new StageEnemy(new Vector2(6, -3), new[] { new Vector2(6, 0), new Vector2(5, -4), new Vector2(7, -3) },
                    bot, EnemyBehavior.Mobile, EnemyArchetype.Burst)
            };
            stage.walls = new[] { new StageWall(Vector2.zero, new Vector2(2, 4)) };
            stage.destructibleWalls = new[] { new StageWall(new Vector2(3, 0), new Vector2(1, 2)) };
            stage.themeId = "wood-prototype";
            stage.boundaryThickness = 1;
        }
        static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = $"{Data}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        static Material Material(string name, Color color)
        {
            string path = $"{Data}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color; material.SetFloat("_Smoothness", 0.05f); AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
        [MenuItem("Tank Game/Validate Stages")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(Main);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("Main is missing SliceBootstrap.");
            ValidateMainStages(bootstrap.stages, bootstrap.settings, bootstrap.presentation);
        }
        public static void ValidateMainStages(StageDefinition[] stages, GameplaySettings settings,
            PrototypePresentation presentation = null)
        {
            if (settings == null) throw new InvalidOperationException("Main is missing GameplaySettings.");
            var errors = StageValidator.Validate(stages, settings);
            if (presentation != null && (presentation.mine == null || presentation.heavy == null || presentation.burst == null))
                errors.Add("Missing required presentation reference.");
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }
        [MenuItem("Tank Game/Build Web")]
        public static void BuildWeb()
        {
            Validate();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL)) throw new InvalidOperationException("Web Build Support missing.");
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Main }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException($"Web build failed: {report.summary.result}");
            Debug.Log($"Web build PASS: {report.summary.totalSize} bytes.");
        }
    }
}
