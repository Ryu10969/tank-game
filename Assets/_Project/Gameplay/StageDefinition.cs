using System;
using UnityEngine;
namespace TankGame.Gameplay
{
    [Serializable]
    public struct StageWall
    {
        public Vector2 center;
        public Vector2 size;
        public StageWall(Vector2 center, Vector2 size) { this.center = center; this.size = size; }
    }

    public enum EnemyBehavior { Mobile, Sentry }

    [Serializable]
    public struct StageEnemy
    {
        public Vector2 spawn;
        public Vector2[] patrolPoints;
        public BotSettings botSettings;
        public EnemyBehavior behavior;
        public StageEnemy(Vector2 spawn, Vector2[] patrolPoints, BotSettings botSettings, EnemyBehavior behavior)
        { this.spawn = spawn; this.patrolPoints = patrolPoints; this.botSettings = botSettings; this.behavior = behavior; }
    }

    [CreateAssetMenu(menuName = "Tank Game/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public int stageId = 1;
        public Vector2 size = new Vector2(20, 14);
        public Vector2 playerSpawn = new Vector2(-6, -3);
        public StageEnemy[] enemies;
        public StageWall[] walls = { new StageWall(Vector2.zero, new Vector2(2, 4)) };
        public StageWall[] destructibleWalls = Array.Empty<StageWall>();
        public Vector2[] mines = Array.Empty<Vector2>();
        public string themeId = "wood-prototype";
        public float boundaryThickness = 1;
        public static Vector3 World(Vector2 point, float height) => new Vector3(point.x, height, point.y);
    }
}
