using System;
using TankGame.Core;
using TankGame.Gameplay;
using TankGame.Input;
using UnityEngine;
namespace TankGame.Presentation
{
    public enum StagePresentationPhase { StageTitle, Go, Playing, StageClear, FinalVictory, Defeat }

    public sealed class SliceBootstrap : MonoBehaviour
    {
        public StageDefinition[] stages;
        public GameplaySettings settings;
        public PrototypePresentation presentation;
        public Camera gameCamera;
        public GameSession Session { get; private set; }
        public StageDefinition CurrentStage => progression == null ? null : stages[progression.CurrentIndex];
        public int CurrentStageId => CurrentStage == null ? 0 : CurrentStage.stageId;
        public StagePresentationPhase Phase { get; private set; }
        public string OverlayText
        {
            get
            {
                switch (Phase)
                {
                    case StagePresentationPhase.StageTitle: return $"STAGE {CurrentStageId}";
                    case StagePresentationPhase.Go: return "GO!";
                    case StagePresentationPhase.StageClear: return "STAGE CLEAR";
                    case StagePresentationPhase.FinalVictory: return "VICTORY";
                    case StagePresentationPhase.Defeat: return "DEFEAT";
                    default: return string.Empty;
                }
            }
        }
        public string MineHudText => Session == null ? string.Empty : $"MINES {Session.MinesRemaining}";
        HumanTankController human;
        StageProgression progression;
        float phaseRemaining;
        void Start() { Restart(); }
        void Update() { TickPresentation(Time.deltaTime); }
        public void Restart()
        {
            if (!EnsureProgression()) return;
            progression.Restart();
            LoadCurrentStage();
        }
        public bool AdvanceStageIfCleared()
        {
            int before = progression == null ? -1 : progression.CurrentIndex;
            TickPresentation(settings.stageClearSeconds);
            return progression != null && progression.CurrentIndex != before;
        }
        public void TickPresentation(float deltaTime)
        {
            if (Session == null) return;
            if (Phase == StagePresentationPhase.Playing)
            {
                if (Session.Rules.State == MatchState.Defeat)
                {
                    Session.SetGameplayEnabled(false); Session.CleanupMines();
                    Phase = StagePresentationPhase.Defeat; return;
                }
                if (Session.Rules.State == MatchState.Victory)
                {
                    Session.SetGameplayEnabled(false); Session.CleanupMines(); Phase = StagePresentationPhase.StageClear;
                    phaseRemaining = settings.stageClearSeconds;
                }
                else return;
            }

            float remaining = Mathf.Max(0, deltaTime);
            while (remaining >= 0 && (Phase == StagePresentationPhase.StageTitle ||
                Phase == StagePresentationPhase.Go || Phase == StagePresentationPhase.StageClear))
            {
                if (remaining < phaseRemaining) { phaseRemaining -= remaining; return; }
                remaining -= phaseRemaining;
                if (Phase == StagePresentationPhase.StageTitle)
                {
                    Phase = StagePresentationPhase.Go; phaseRemaining = settings.stageGoSeconds;
                }
                else if (Phase == StagePresentationPhase.Go)
                {
                    Phase = StagePresentationPhase.Playing; phaseRemaining = 0;
                    Session.SetGameplayEnabled(true); return;
                }
                else
                {
                    if (progression.TryAdvance()) LoadCurrentStage();
                    else Phase = StagePresentationPhase.FinalVictory;
                    return;
                }
            }
        }
        bool EnsureProgression()
        {
            var errors = StageValidator.Validate(stages, settings);
            if (presentation == null || presentation.mine == null || presentation.heavy == null || presentation.burst == null)
                errors.Add("Missing required presentation reference.");
            if (gameCamera == null) errors.Add("Missing game camera reference.");
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
            Session = root.AddComponent<GameSession>();
            Session.Initialize(settings, stage.enemies.Length, presentation.projectile, presentation.mine,
                (center, radius) => PrimitiveFactory.MineExplosion(root.transform, center, radius, presentation));
            PrimitiveFactory.Arena(root.transform, stage, presentation);
            foreach (var wall in stage.destructibleWalls) PrimitiveFactory.DestructibleWall(Session, wall, presentation);
            var player = PrimitiveFactory.Tank(Session, stage.playerSpawn, true, presentation, settings.moveSpeed);
            var indicator = player.gameObject.AddComponent<AmmoIndicator>(); indicator.Initialize(player, gameCamera);
            human = new HumanTankController(gameCamera, settings.planeHeight, Vector3.forward);
            player.Controller = human;
            foreach (var enemy in stage.enemies)
            {
                var bot = PrimitiveFactory.Tank(Session, enemy.spawn, false, presentation, enemy.botSettings.moveSpeed, enemy.archetype);
                bot.Controller = CreateEnemyController(enemy, settings);
            }
            Physics.SyncTransforms();
            Phase = StagePresentationPhase.StageTitle; phaseRemaining = settings.stageTitleSeconds;
            Session.SetGameplayEnabled(false);
        }
        public static ITankController CreateEnemyController(StageEnemy enemy, GameplaySettings settings)
        {
            ITankController controller;
            switch (enemy.behavior)
            {
                case EnemyBehavior.Mobile: controller = new BotTankController(enemy.botSettings, enemy.patrolPoints); break;
                case EnemyBehavior.Sentry: controller = new SentryTankController(enemy.botSettings); break;
                default: throw new ArgumentOutOfRangeException(nameof(enemy.behavior), enemy.behavior, "Undefined Enemy Behavior.");
            }
            switch (enemy.archetype)
            {
                case EnemyArchetype.Standard:
                case EnemyArchetype.Heavy: return controller;
                case EnemyArchetype.Burst:
                    return new BurstTankController(controller, settings.burstShotCount,
                        settings.burstSpacingSeconds, settings.burstReloadSeconds);
                default: throw new ArgumentOutOfRangeException(nameof(enemy.archetype), enemy.archetype, "Undefined Enemy Archetype.");
            }
        }
        void OnDestroy() { human?.Dispose(); }
        void OnGUI()
        {
            if (Session == null) return;
            float scale = Mathf.Max(1, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float scaledWidth = Screen.width / scale;
            GUI.Label(new Rect(scaledWidth / 2 - 90, 14, 180, 32), MineHudText,
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 });
            if (Phase == StagePresentationPhase.Playing) return;
            float x = scaledWidth / 2 - 120;
            float y = Screen.height / scale / 2 - 60;
            if (Phase == StagePresentationPhase.StageTitle || Phase == StagePresentationPhase.Go ||
                Phase == StagePresentationPhase.StageClear)
            {
                GUI.Label(new Rect(x, y + 35, 240, 50), OverlayText,
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30 });
                return;
            }
            GUI.Box(new Rect(x, y, 240, 140), GUIContent.none);
            GUI.Label(new Rect(x, y + 14, 240, 40), OverlayText, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 });
            if (GUI.Button(new Rect(x + 30, y + 76, 180, 42), "RESTART · STAGE 1")) Restart();
        }
    }
}
