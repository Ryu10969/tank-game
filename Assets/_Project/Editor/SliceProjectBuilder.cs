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
            var stage = Asset<StageDefinition>("Stage1"); stage.botSettings = bot;
            var art = Asset<PrototypePresentation>("PrototypePresentation");
            art.floor = Material("Floor", new Color(0.57f, 0.39f, 0.22f));
            art.wall = Material("Wall", new Color(0.76f, 0.56f, 0.32f));
            art.player = Material("Player", new Color(0.22f, 0.65f, 0.64f));
            art.enemy = Material("Enemy", new Color(0.83f, 0.29f, 0.16f));
            art.trim = Material("Trim", new Color(0.22f, 0.16f, 0.12f));
            art.projectile = Material("Projectile", new Color(1, 0.9f, 0.42f));
            EditorUtility.SetDirty(stage); EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            Validate();
            var scene = EditorSceneManager.OpenScene(Main);
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Core Vertical Slice").AddComponent<SliceBootstrap>();
            bootstrap.stage = stage; bootstrap.settings = settings; bootstrap.presentation = art; bootstrap.gameCamera = Camera.main;
            if (bootstrap.gameCamera == null) throw new InvalidOperationException("Main camera is required.");
            bootstrap.gameCamera.orthographic = true; bootstrap.gameCamera.orthographicSize = art.cameraSize;
            bootstrap.gameCamera.transform.SetPositionAndRotation(art.cameraPosition, Quaternion.Euler(art.cameraAngles));
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Main, true) };
            Debug.Log("Core slice prepared: Main only; Stage validation PASS.");
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
            var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>($"{Data}/GameplaySettings.asset");
            var ids = AssetDatabase.FindAssets("t:StageDefinition");
            var stages = new StageDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++) stages[i] = AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(ids[i]));
            if (settings == null || stages.Length == 0) throw new InvalidOperationException("Missing slice data.");
            var errors = StageValidator.Validate(stages, settings.tankRadius);
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
