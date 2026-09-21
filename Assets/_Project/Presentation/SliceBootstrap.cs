using TankGame.Core;
using TankGame.Gameplay;
using TankGame.Input;
using UnityEngine;
namespace TankGame.Presentation
{
    public sealed class SliceBootstrap : MonoBehaviour
    {
        public StageDefinition stage;
        public GameplaySettings settings;
        public PrototypePresentation presentation;
        public Camera gameCamera;
        public GameSession Session { get; private set; }
        HumanTankController human;
        void Start() { Restart(); }
        public void Restart()
        {
            human?.Dispose(); human = null;
            if (Session != null) { Session.gameObject.SetActive(false); Destroy(Session.gameObject); }
            var errors = StageValidator.Validate(new[] { stage }, settings.tankRadius);
            if (errors.Count > 0) { Debug.LogError(string.Join("\n", errors)); return; }
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = presentation.cameraSize;
            gameCamera.transform.SetPositionAndRotation(presentation.cameraPosition, Quaternion.Euler(presentation.cameraAngles));
            // Keep the complete arena visible for narrow desktop/browser windows too.
            float requiredWidth = stage.size.x / 2 + stage.boundaryThickness;
            gameCamera.orthographicSize = Mathf.Max(presentation.cameraSize, requiredWidth / gameCamera.aspect);
            var root = new GameObject("Stage 1 Runtime"); root.transform.SetParent(transform, false);
            Session = root.AddComponent<GameSession>(); Session.Initialize(settings, stage.enemySpawns.Length, presentation.projectile);
            PrimitiveFactory.Arena(root.transform, stage, presentation);
            var player = PrimitiveFactory.Tank(Session, stage.playerSpawn, true, presentation, settings.moveSpeed);
            human = new HumanTankController(gameCamera, settings.planeHeight, Vector3.forward);
            player.Controller = human;
            foreach (var spawn in stage.enemySpawns)
            {
                var bot = PrimitiveFactory.Tank(Session, spawn, false, presentation, stage.botSettings.moveSpeed);
                bot.Controller = new BotTankController(stage.botSettings, stage.patrolPoints);
            }
            Physics.SyncTransforms();
        }
        void OnDestroy() { human?.Dispose(); }
        void OnGUI()
        {
            if (Session == null) return;
            float scale = Mathf.Max(1, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.Label(new Rect(20, 18, 200, 32), $"AMMO  {Session.Player.Slots.Available} / {settings.shotCapacity}", new GUIStyle(GUI.skin.label) { fontSize = 24 });
            if (Session.Rules.State == MatchState.Playing) return;
            float x = Screen.width / scale / 2 - 120;
            float y = Screen.height / scale / 2 - 60;
            GUI.Box(new Rect(x, y, 240, 140), GUIContent.none);
            GUI.Label(new Rect(x, y + 14, 240, 40), Session.Rules.State == MatchState.Victory ? "VICTORY" : "DEFEAT", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 });
            if (GUI.Button(new Rect(x + 30, y + 76, 180, 42), "RESTART · STAGE 1")) Restart();
        }
    }
}
