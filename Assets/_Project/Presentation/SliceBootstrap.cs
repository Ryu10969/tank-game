using System;
using TankGame.Core;
using TankGame.Gameplay;
using TankGame.Input;
using UnityEngine;
namespace TankGame.Presentation
{
    public sealed class SliceBootstrap : MonoBehaviour
    {
        public StageDefinition[] stages;
        public GameplaySettings settings;
        public PrototypePresentation presentation;
        public Camera gameCamera;
        public GameSession Session { get; private set; }
        public StageDefinition CurrentStage => progression == null ? null : stages[progression.CurrentIndex];
        public int CurrentStageId => CurrentStage == null ? 0 : CurrentStage.stageId;
        HumanTankController human;
        StageProgression progression;
        void Start() { Restart(); }
        void Update() { AdvanceStageIfCleared(); }
        public void Restart()
        {
            if (!EnsureProgression()) return;
            progression.Restart();
            LoadCurrentStage();
        }
        public bool AdvanceStageIfCleared()
        {
            if (Session == null || Session.Rules.State != MatchState.Victory || !progression.HasNext) return false;
            progression.TryAdvance();
            LoadCurrentStage();
            return true;
        }
        bool EnsureProgression()
        {
            var errors = StageValidator.Validate(stages, settings.tankRadius);
            if (errors.Count > 0) { Debug.LogError(string.Join("\n", errors)); return false; }
            if (progression == null || progression.StageCount != stages.Length)
                progression = new StageProgression(stages.Length);
            return true;
        }
        void LoadCurrentStage()
        {
            human?.Dispose(); human = null;
            if (Session != null) { Session.gameObject.SetActive(false); Destroy(Session.gameObject); }
            var stage = CurrentStage;
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = presentation.cameraSize;
            gameCamera.transform.SetPositionAndRotation(presentation.cameraPosition, Quaternion.Euler(presentation.cameraAngles));
            // Keep the complete arena visible for narrow desktop/browser windows too.
            float requiredWidth = stage.size.x / 2 + stage.boundaryThickness;
            gameCamera.orthographicSize = Mathf.Max(presentation.cameraSize, requiredWidth / gameCamera.aspect);
            var root = new GameObject($"Stage {stage.stageId} Runtime"); root.transform.SetParent(transform, false);
            Session = root.AddComponent<GameSession>(); Session.Initialize(settings, stage.enemies.Length, presentation.projectile);
            PrimitiveFactory.Arena(root.transform, stage, presentation);
            foreach (var wall in stage.destructibleWalls) PrimitiveFactory.DestructibleWall(Session, wall, presentation);
            foreach (var mine in stage.mines) PrimitiveFactory.Mine(Session, mine, presentation);
            var player = PrimitiveFactory.Tank(Session, stage.playerSpawn, true, presentation, settings.moveSpeed);
            var indicator = player.gameObject.AddComponent<AmmoIndicator>(); indicator.Initialize(player, gameCamera);
            human = new HumanTankController(gameCamera, settings.planeHeight, Vector3.forward);
            player.Controller = human;
            foreach (var enemy in stage.enemies)
            {
                var bot = PrimitiveFactory.Tank(Session, enemy.spawn, false, presentation, enemy.botSettings.moveSpeed);
                bot.Controller = CreateEnemyController(enemy);
            }
            Physics.SyncTransforms();
        }
        public static ITankController CreateEnemyController(StageEnemy enemy)
        {
            switch (enemy.behavior)
            {
                case EnemyBehavior.Mobile: return new BotTankController(enemy.botSettings, enemy.patrolPoints);
                case EnemyBehavior.Sentry: return new SentryTankController(enemy.botSettings);
                default: throw new ArgumentOutOfRangeException(nameof(enemy.behavior), enemy.behavior, "Undefined Enemy Behavior.");
            }
        }
        void OnDestroy() { human?.Dispose(); }
        void OnGUI()
        {
            if (Session == null) return;
            float scale = Mathf.Max(1, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            if (Session.Rules.State == MatchState.Playing) return;
            float x = Screen.width / scale / 2 - 120;
            float y = Screen.height / scale / 2 - 60;
            GUI.Box(new Rect(x, y, 240, 140), GUIContent.none);
            GUI.Label(new Rect(x, y + 14, 240, 40), Session.Rules.State == MatchState.Victory ? "VICTORY" : "DEFEAT", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 });
            if (GUI.Button(new Rect(x + 30, y + 76, 180, 42), "RESTART · STAGE 1")) Restart();
        }
    }
}
